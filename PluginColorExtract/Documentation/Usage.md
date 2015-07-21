# ColorExtract Plugin

`Plugin=ColorExtract` analyzes a given image and extracts three colors to use as a color scheme
for a meter based on that image.

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
