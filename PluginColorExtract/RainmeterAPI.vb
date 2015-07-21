'  Copyright (C) 2011 Birunthan Mohanathas
'  VB.NET implementation (C) 2015 Bryan Mitchell

'  This program is free software; you can redistribute it and/or
'  modify it under the terms of the GNU General Public License
'  as published by the Free Software Foundation; either version 2
'  of the License, or (at your option) any later version.

'  This program is distributed in the hope that it will be useful,
'  but WITHOUT ANY WARRANTY; without even the implied warranty of
'  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'  GNU General Public License for more details.

'  You should have received a copy of the GNU General Public License
'  along with this program; if not, write to the Free Software
'  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

Imports System.Runtime.InteropServices

Namespace Global.Rainmeter

    ''' <summary>
    ''' Wrapper around the Rainmeter C API.
    ''' </summary>
    Public Class API

        Private m_Rm As IntPtr

        Public Sub New(rm As IntPtr)
            m_Rm = rm
        End Sub

        Private Declare Unicode Function RmReadString Lib "Rainmeter.dll" (
                rm As IntPtr, [option] As String, defValue As String, replaceMeasures As Boolean) As IntPtr

        Private Declare Unicode Function RmReadFormula Lib "Rainmeter.dll" (
                rm As IntPtr, [option] As String, defValue As Double) As Double

        Private Declare Unicode Function RmReplaceVariables Lib "Rainmeter.dll" (
                rm As IntPtr, str As String) As IntPtr

        Private Declare Unicode Function RmPathToAbsolute Lib "Rainmeter.dll" (
                rm As IntPtr, relativePath As String) As IntPtr

        Public Declare Unicode Sub Execute Lib "Rainmeter.dll" Alias "RmExecute" (
                skin As IntPtr, command As String)

        Private Declare Function RmGet Lib "Rainmeter.dll" (
                rm As IntPtr, type As RmGetType) As IntPtr

        <DllImport("Rainmeter.dll", CharSet:=CharSet.Unicode, CallingConvention:=CallingConvention.Cdecl)>
        Private Shared Function LSLog(type As LogType, unused As String, message As String) As Integer
        End Function

        Private Enum RmGetType
            MeasureName = 0
            Skin = 1
            SettingsFile = 2
            SkinName = 3
            SkinWindowHandle = 4
        End Enum

        Public Enum LogType
            [Error] = 1
            Warning = 2
            Notice = 3
            Debug = 4
        End Enum

        Public Function ReadString([option] As String, defValue As String, Optional replaceMeasures As Boolean = True) As String
            Return Marshal.PtrToStringUni(RmReadString(m_Rm, [option], defValue, replaceMeasures))
        End Function

        Public Function ReadPath([option] As String, defValue As String) As String
            Return Marshal.PtrToStringUni(RmPathToAbsolute(m_Rm, ReadString([option], defValue)))
        End Function

        Public Function ReadDouble([option] As String, defValue As Double) As Double
            Return RmReadFormula(m_Rm, [option], defValue)
        End Function

        Public Function ReadInt([option] As String, defValue As Integer) As Integer
            Return CInt(RmReadFormula(m_Rm, [option], defValue))
        End Function

        Public Function ReplaceVariables(str As String) As String
            Return Marshal.PtrToStringUni(RmReplaceVariables(m_Rm, str))
        End Function

        Public Function GetMeasureName() As String
            Return Marshal.PtrToStringUni(RmGet(m_Rm, RmGetType.MeasureName))
        End Function

        Public Function GetSkin() As IntPtr
            Return RmGet(m_Rm, RmGetType.Skin)
        End Function

        Public Function GetSettingsFile() As String
            Return Marshal.PtrToStringUni(RmGet(m_Rm, RmGetType.SettingsFile))
        End Function

        Public Function GetSkinName() As String
            Return Marshal.PtrToStringUni(RmGet(m_Rm, RmGetType.SkinName))
        End Function

        Public Function GetSkinWindow() As IntPtr
            Return RmGet(m_Rm, RmGetType.SkinWindowHandle)
        End Function

        Public Shared Sub Log(type As LogType, message As String)
            LSLog(type, Nothing, message)
        End Sub

    End Class

    ''' <summary>
    ''' Dummy attribute to mark method as exported for DllExporter.exe.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Method)>
    Public Class DllExport
        Inherits Attribute
    End Class

End Namespace
