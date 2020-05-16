IxMilia.Converters
==================

A portable .NET library for converting between various CAD file types.

# Examples

## Convert DXF to SVG

``` csharp
var dxf = DxfFile.Load(@"C:\path\to\file.dxf");

// specify the DXF bounds to plot; left, right, bottom, top
var dxfRect = new ConverterDxfRect(0.0, 800.0, 0.0, 600.0);

// specify the SVG element size
var svgRect = new ConverterSvgRect(400.0, 300.0);

// create options
var convertOptions = new DxfToSvgConverterOptions(dxfRect, svgRect);

// and finally convert
var svg = new DxfToSvgConverter().Convert(dxf, convertOptions);

// optionally write the svg to disk
svg.SaveTo(@"C:\path\to\my\final.svg");
```

Populate the submodules with

``` bash
git submodule update --init --recursive
```

Update submodules to latest `master` with

``` bash
git submodule update --recursive --remote
```
