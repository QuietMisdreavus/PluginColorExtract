# ColorExtract Plugin

`Plugin=ColorExtract` analyzes a given image and extracts three colors to use as a color scheme
for a meter based on that image.

ColorExtract operates with a "parent / child" approach. A main "parent" ColorExtract measure is
used to load and process the image, and then "child" measures are used to show individual colors
from the "parent", using the `ParentMeasure` and `ColorType` options.

## Usage

ColorExtract measures take the form:

``` ini
[MeasureParent]
Measure=Plugin
Plugin=ColorExtract
ImagePath=#@#Images\cover.jpg
```

This measure will load and extract colors from an image in the skin's [@Resources folder][],
creating three `ColorType` options with the color values. This information can be used in
subsequent "child" ColorExtract measures:

[@Resources folder]: http://docs.rainmeter.net/manual/skins/resources-folder

``` ini
[MeasureBackgroundColor]
Measure=Plugin
Plugin=ColorExtract
ParentMeasure=[MeasureParent]
ColorType=Background

[MeasureHeaderColor]
Measure=Plugin
Plugin=ColorExtract
ParentMeasure=[MeasureParent]
ColorType=Accent1
```

The string values of these "child" measures can be used as [section variables][] to dynamically
change the display colors of one or more meters based on the input image:

[section variables]: http://docs.rainmeter.net/manual/variables/section-variables

``` ini
[MeterColoredString]
Meter=String
Text=This is some demo text.
DynamicVariables=1
SolidColor=[MeasureBackgroundColor]
FontColor=[MeasureHeaderColor]
```

The "parent" measure can even take its image path dynamically, for example from a [QuotePlugin][]
or a [NowPlaying][] measure. The plugin will only re-process an image once its path changes, to
preserve CPU resources.

[QuotePlugin]: http://docs.rainmeter.net/manual/plugins/quote
[NowPlaying]: http://docs.rainmeter.net/manual/plugins/nowplaying

``` ini
[MeasureImage]
Measure=Plugin
Plugin=QuotePlugin
PathName=#@#Images
UpdateDivider=60

[MeasureColorExtract]
Measure=Plugin
Plugin=ColorExtract
ImagePath=[MeasureImage]
DynamicVariables=1
```

With this setup, `MeasureColorExtract` will provide new colors every time `MeasureImage` changes its
image.

**Note**: ColorExtract "parent" measures will process an image in the background, to keep Rainmeter
from freezing. This means that new colors will appear at least one update cycle after an image
changes.

## Options

#### General measure options

All [general measure options][] are valid.

[general measure options]: http://docs.rainmeter.net/manual/measures/general-options

### Parent measure options

#### `ImagePath`

Path to an image. It's possible to set this to the name of another measure, e.g.
`ImagePath=[SomeMeasure]`, however you should also set `DynamicVariables=1` so that measure updates
are properly tracked.

### Child measure options

#### `ParentMeasure`

Name of a parent measure to load colors from.

*Example*: `ParentMeasure=[SomeMeasure]`

#### `ColorType`

Color value to return. Valid values are `Background`, `Accent1`, and `Accent2`.

`Background` will return a color value suitable for use as a background for the other two colors.

`Accent1` and `Accent2` will return color values suitable for a header and body text,
respectively. These may return variants of each other or of black and white if not enough suitable
contrasting colors are found in the image.

Colors returned will have an alpha value of `ColorAlpha`.

(Default: Background)

#### `ColorAlpha`

Alpha value to apply to the returned color. This value is capped to between 0-255, inclusive.

(Default: 255)

<!-- vim: set ft=markdown: -->
