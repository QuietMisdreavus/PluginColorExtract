'PluginColorExtract - Rainmeter plugin to extract interface colors from an image
'Copyright (C) 2015 Bryan "icesoldier" Mitchell

'This program is free software; you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation; either version 2 of the License, or
'(at your option) any later version.

'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.

'You should have received a copy of the GNU General Public License along
'with this program; if not, write to the Free Software Foundation, Inc.,
'51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

Imports System.Drawing
Imports System.Threading
Imports System.Runtime.InteropServices
Imports Rainmeter

Friend Enum ColorType
    Background
    Accent1
    Accent2
End Enum

Friend Structure ColorSet
    Public Background As Color
    Public Accent1 As Color
    Public Accent2 As Color
End Structure

Friend Class Measure

    Friend Shared ParentsList As New List(Of Parent)

    Friend varColorType As ColorType
    Friend varColorAlpha As Integer
    Friend varMeasureName As String

    Friend varParentMeasure As Parent = Nothing
    Friend varParentName As String
    Friend varSkin As IntPtr

    Friend Sub New(api As Rainmeter.API)
        varMeasureName = api.GetMeasureName()
        varSkin = api.GetSkin()
    End Sub

    Friend Sub Dispose()
        If varParentMeasure IsNot Nothing Then
            varParentMeasure.Dispose()
            ParentsList.Remove(varParentMeasure)
            varParentMeasure = Nothing
        End If
    End Sub

    Friend Sub Reload(api As Rainmeter.API, ByRef maxValue As Double)
        Dim TypeOption As String = api.ReadString("ColorType", "Background")
        Select Case TypeOption.ToLowerInvariant()
            Case "accent1"
                varColorType = ColorType.Accent1
            Case "accent2"
                varColorType = ColorType.Accent2
            Case Else
                varColorType = ColorType.Background
        End Select

        varColorAlpha = api.ReadInt("ColorAlpha", 255)

        If varColorAlpha > 255 Then
            varColorAlpha = 255
        ElseIf varColorAlpha < 0 Then
            varColorAlpha = 0
        End If

        'Get the raw string put there to see that the option is set in the first place
        'e.g. if the ImagePath refers to a NowPlaying cover image, but the current track has no
        'cover (or the player is unloaded), we still keep the reference to the parent class intact
        Dim PathOption As String = api.ReadPath("ImagePath", "")
        Dim PathReference As String = api.ReadString("ImagePath", "", False)
        varParentName = api.ReadString("ParentMeasure", "", False)

        If Not String.IsNullOrEmpty(PathReference) And Not String.IsNullOrEmpty(varParentName) Then
            Rainmeter.API.Log(Rainmeter.API.LogType.Debug, "ColorExtract.dll: Both ImagePath and ParentMeasure are set, ignoring parent")
        End If

        If Not String.IsNullOrEmpty(PathReference) Then
            If varParentMeasure Is Nothing Then
                varParentMeasure = New Parent(varMeasureName, varSkin)
                ParentsList.Add(varParentMeasure)
            End If
            varParentMeasure.UpdatePath(PathOption)
        ElseIf Not String.IsNullOrEmpty(varParentName) Then
            varParentMeasure = Nothing
            For Each ParentCheck As Parent In ParentsList
                If String.Format("[{0}]", ParentCheck.varMeasureName) = varParentName And ParentCheck.varSkin = varSkin Then
                    varParentMeasure = ParentCheck
                    Exit For
                End If
            Next
        Else
            Rainmeter.API.Log(Rainmeter.API.LogType.Debug, "ColorExtract.dll: Neither ImagePath nor ParentMeasure are set (" & varMeasureName & ")")
            varParentMeasure = Nothing
        End If
    End Sub

    Friend Function Update() As Double
        If varParentMeasure IsNot Nothing AndAlso varParentMeasure.varMeasureName = Me.varMeasureName Then
            varParentMeasure.Update()
        End If

        Return 0.0
    End Function

    Friend Function GetString() As String
        If varParentMeasure Is Nothing Then
            Return ""
        Else
            Return varParentMeasure.GetString(varColorType, varColorAlpha)
        End If
    End Function

End Class

Friend Class Parent

    Friend varLocker As New Object()
    Friend varImagePath As String = ""
    Friend varNeedsUpdating As Boolean = False
    Friend varWorkerThread As Thread = Nothing
    Friend varBackground As Color = Color.Black
    Friend varAccent1 As Color = Color.White
    Friend varAccent2 As Color = Color.White

    Friend varMeasureName As String
    Friend varSkin As IntPtr

    Friend Sub New(pMeasureName As String, pSkin As IntPtr)
        varMeasureName = pMeasureName
        varSkin = pSkin
    End Sub

    Friend Sub UpdatePath(pImagePath As String)
        SyncLock varLocker
            If varImagePath <> pImagePath Then
                varImagePath = pImagePath
                varNeedsUpdating = True
            End If
        End SyncLock
    End Sub

    Friend Sub Dispose()
        SyncLock varLocker
            If varWorkerThread IsNot Nothing Then
                varWorkerThread.Abort()
                varWorkerThread = Nothing
            End If
        End SyncLock
    End Sub

    Friend Sub Update()
        SyncLock varLocker
            If varWorkerThread Is Nothing And varNeedsUpdating = True Then
                varWorkerThread = New Thread(AddressOf ExtractThreadProc)
                varWorkerThread.Start(Me)
            End If
        End SyncLock
    End Sub

    Friend Function GetString(pColorType As ColorType, pColorAlpha As Integer) As String
        Dim RetColor As Color

        SyncLock varLocker
            Select Case pColorType
                Case ColorType.Background
                    RetColor = varBackground
                Case ColorType.Accent1
                    RetColor = varAccent1
                Case ColorType.Accent2
                    RetColor = varAccent2
            End Select
        End SyncLock

        Return String.Format("{0},{1},{2},{3}", RetColor.R, RetColor.G, RetColor.B, pColorAlpha)
    End Function

    Private Shared Sub ExtractThreadProc(data As Object)
        Dim Source As Parent = CType(data, Parent)
        Dim Temp As Parent

        SyncLock Source.varLocker
            Temp = CType(Source.MemberwiseClone(), Parent)
            Source.varNeedsUpdating = False
        End SyncLock

        If Temp.varNeedsUpdating Then
            Dim ColorSet As ColorSet

            If System.IO.File.Exists(Temp.varImagePath) Then
                Using FullImage As New Bitmap(Temp.varImagePath)
                    Using ReducedImage As New Bitmap(FullImage, 50, 50)
                        ColorSet = SelectColors(ReducedImage)
                    End Using
                End Using
            Else
                ColorSet.Background = Color.Black
                ColorSet.Accent1 = Color.White
                ColorSet.Accent2 = Color.White
            End If

            SyncLock Source.varLocker
                Source.varBackground = ColorSet.Background
                Source.varAccent1 = ColorSet.Accent1
                Source.varAccent2 = ColorSet.Accent2
            End SyncLock
        End If

        SyncLock Source.varLocker
            Source.varWorkerThread = Nothing
        End SyncLock
    End Sub

#Region "Color Extract Helpers"

    Private Shared Function SelectColors(pImage As Bitmap) As ColorSet
        Dim ret As ColorSet

        Const CandidateDiffBackground As Double = 0.3
        Const MinDiffBackground As Double = 0.4
        Const TrackDistance As Double = 0.25

        ret.Background = DominantColors(InsideBorder(pImage))(0)
        Dim CandidateColors As List(Of Color) = DominantColors(Pixels(pImage))

        Dim FirstFound As Boolean = False
        Dim SecondFound As Boolean = False
        Dim BackgroundBrightness As Double = ColorBrightness(ret.Background)

        For Each Candidate As Color In CandidateColors
            If Math.Abs(ColorBrightness(Candidate) - BackgroundBrightness) < CandidateDiffBackground Then
                Continue For
            End If

            If Math.Abs(ColorBrightness(Candidate) - BackgroundBrightness) < MinDiffBackground Then
                Candidate = YUV.BrightenFromBackground(Candidate, ret.Background, (MinDiffBackground - CandidateDiffBackground))
                If Math.Abs(ColorBrightness(Candidate) - BackgroundBrightness) < MinDiffBackground Then
                    'If the background is close enough to black (maybe white, but black was what I hit first),
                    'we can't push the brightness any closer to our threshold just by pushing the Y value. Toss it.
                    Continue For
                End If
            End If

            If Not FirstFound Then
                ret.Accent1 = Candidate
                FirstFound = True
                Continue For
            End If

            If YUV.ColorDistance(ret.Accent1, Candidate) > TrackDistance Then
                SecondFound = True
                ret.Accent2 = Candidate
                Exit For
            End If
        Next

        If Not SecondFound Or Not FirstFound Then
            For Each backup In {Color.Black, Color.White}
                If Math.Abs(ColorBrightness(backup) - BackgroundBrightness) >= CandidateDiffBackground Then
                    If Not FirstFound Then
                        ret.Accent1 = backup
                        FirstFound = True
                        Exit For
                    ElseIf Not SecondFound AndAlso YUV.ColorDistance(ret.Accent1, backup) > TrackDistance Then
                        SecondFound = True
                        ret.Accent2 = backup
                        Exit For
                    End If
                End If
            Next
        End If

        If Not SecondFound Then
            ret.Accent2 = YUV.FadeIntoBackground(ret.Accent1, ret.Background, 0.1)
        End If

        Return ret
    End Function

    Private Shared Iterator Function InsideBorder(pImage As Bitmap) As IEnumerable(Of Color)
        For x = 2 To pImage.Width - 3
            Yield pImage.GetPixel(x, 2)
            Yield pImage.GetPixel(x, pImage.Height - 3)
        Next

        For y = 2 To pImage.Height - 3
            Yield pImage.GetPixel(2, y)
            Yield pImage.GetPixel(pImage.Width - 3, y)
        Next
    End Function

    Private Shared Iterator Function Pixels(pImage As Bitmap) As IEnumerable(Of Color)
        For x = 0 To pImage.Width - 1
            For y = 0 To pImage.Height - 1
                Yield pImage.GetPixel(x, y)
            Next
        Next
    End Function

    Private Shared Function DominantColors(pColors As IEnumerable(Of Color)) As List(Of Color)
        Dim buckets As List(Of List(Of Color)) = ColorBuckets(pColors)
        buckets.Sort(Function(left, right) left.Count.CompareTo(right.Count) * -1)

        Dim ColorReductions As New List(Of Color)

        For Each b In buckets
            ColorReductions.Add(MeanColor(b))
        Next

        Return ColorReductions
    End Function

    Private Shared Function ColorBuckets(pColors As IEnumerable(Of Color)) As List(Of List(Of Color))
        Dim subsets As New List(Of List(Of Color))

        For Each c In pColors
            Dim bucket As List(Of Color) = Nothing

            For Each check In subsets
                If YUV.ColorDistance(c, check(0)) < 0.1 Then
                    bucket = check
                    Exit For
                End If
            Next

            If bucket Is Nothing Then
                bucket = New List(Of Color)
                subsets.Add(bucket)
            End If

            bucket.Add(c)
        Next

        Return subsets
    End Function

    Private Shared Function ColorBrightness(pColor As Color) As Double
        Dim accum As Double = 0.0

        accum += Math.Pow(pColor.R, 2) * 0.299
        accum += Math.Pow(pColor.G, 2) * 0.587
        accum += Math.Pow(pColor.B, 2) * 0.114

        Return Math.Sqrt(accum) / 255
    End Function

    Private Shared Function MeanColor(pColors As List(Of Color)) As Color
        Dim RAccum As Integer = 0
        Dim GAccum As Integer = 0
        Dim BAccum As Integer = 0

        For Each c In pColors
            RAccum += c.R
            GAccum += c.G
            BAccum += c.B
        Next

        Return Color.FromArgb(CInt(RAccum / pColors.Count), CInt(GAccum / pColors.Count), CInt(BAccum / pColors.Count))
    End Function

#End Region

End Class

Public Module Plugin

    Private StringBuffer As IntPtr = IntPtr.Zero

    <DllExport>
    Public Sub Initialize(ByRef data As IntPtr, rm As IntPtr)
        data = GCHandle.ToIntPtr(GCHandle.Alloc(New Measure(New Rainmeter.API(rm))))
    End Sub

    <DllExport>
    Public Sub Finalize(data As IntPtr)
        CType(GCHandle.FromIntPtr(data).Target, Measure).Dispose()
        GCHandle.FromIntPtr(data).Free()

        If StringBuffer <> IntPtr.Zero Then
            Marshal.FreeHGlobal(StringBuffer)
            StringBuffer = IntPtr.Zero
        End If
    End Sub

    <DllExport>
    Public Sub Reload(data As IntPtr, rm As IntPtr, ByRef maxValue As Double)
        Dim measure As Measure = CType(GCHandle.FromIntPtr(data).Target, Measure)
        measure.Reload(New Rainmeter.API(rm), maxValue)
    End Sub

    <DllExport>
    Public Function Update(data As IntPtr) As Double
        Dim measure As Measure = CType(GCHandle.FromIntPtr(data).Target, Measure)
        Return measure.Update()
    End Function

    <DllExport>
    Public Function GetString(data As IntPtr) As IntPtr
        Dim measure As Measure = CType(GCHandle.FromIntPtr(data).Target, Measure)
        If StringBuffer <> IntPtr.Zero Then
            Marshal.FreeHGlobal(StringBuffer)
            StringBuffer = IntPtr.Zero
        End If

        Dim stringValue As String = measure.GetString()
        If stringValue IsNot Nothing Then
            StringBuffer = Marshal.StringToHGlobalUni(stringValue)
        End If

        Return StringBuffer
    End Function

End Module

Public Structure YUV

    Public Y As Double
    Public U As Double
    Public V As Double

    Private Const RWeight As Double = 0.299
    Private Const GWeight As Double = 0.587
    Private Const BWeight As Double = 0.114
    Private Const UMax As Double = 0.436
    Private Const VMax As Double = 0.615

    Public Function ToRGB() As Color
        Dim R As Integer = Y + (1.14 * V)
        Dim G As Integer = Y - (0.395 * U) - (0.581 * V)
        Dim B As Integer = Y + (2.033 * U)

        If R > 255 Then
            R = 255
        ElseIf R < 0 Then
            R = 0
        End If

        If G > 255 Then
            G = 255
        ElseIf G < 0 Then
            G = 0
        End If

        If B > 255 Then
            B = 255
        ElseIf B < 0 Then
            B = 0
        End If

        Return Color.FromArgb(R, G, B)
    End Function

    Public Shared Function FromRGB(pRGB As Color) As YUV
        Dim ret As YUV

        ret.Y = (RWeight * pRGB.R) + (GWeight * pRGB.G) + (BWeight * pRGB.B)
        ret.U = UMax * ((pRGB.B - ret.Y) / (1 - BWeight))
        ret.V = VMax * ((pRGB.R - ret.Y) / (1 - RWeight))

        Return ret
    End Function

    Public Shared Function FadeIntoBackground(pRGB As Color, pBackground As Color, pAmount As Double) As Color
        Dim Back As YUV = YUV.FromRGB(pBackground)
        Dim ret As YUV = YUV.FromRGB(pRGB)

        If (Back.Y - ret.Y > 0) Then
            ret.Y += pAmount * 255
            ret.Y = Math.Min(ret.Y, 255)
        Else
            ret.Y -= pAmount * 255
            ret.Y = Math.Max(ret.Y, 0)
        End If

        Return ret.ToRGB()
    End Function

    Public Shared Function BrightenFromBackground(pRGB As Color, pBackground As Color, pAmount As Double) As Color
        Dim Back As YUV = YUV.FromRGB(pBackground)
        Dim ret As YUV = YUV.FromRGB(pRGB)

        If (Back.Y - ret.Y < 0) Then
            ret.Y += pAmount * 255
            ret.Y = Math.Min(ret.Y, 255)
        Else
            ret.Y -= pAmount * 255
            ret.Y = Math.Max(ret.Y, 0)
        End If

        Return ret.ToRGB()
    End Function

    Public Shared Function Distance(pLeft As YUV, pRight As YUV) As Double
        Dim accum As Double = 0.0
        Dim term As Double

        term = pLeft.Y - pRight.Y
        accum += term * term

        term = pLeft.U - pRight.U
        accum += term * term

        term = pLeft.V - pRight.V
        accum += term * term

        Return Math.Sqrt(accum) / 255
    End Function

    Public Shared Function ColorDistance(pLeft As Color, pRight As Color) As Double
        Return Distance(FromRGB(pLeft), FromRGB(pRight))
    End Function

End Structure
