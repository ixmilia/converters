using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public struct DxfToDwgConverterOptions
    {
        public DwgVersionId TargetVersion { get; set; }

        public DxfToDwgConverterOptions(DwgVersionId targetVersion)
        {
            TargetVersion = targetVersion;
        }
    }

    public class DxfToDwgConverter : IConverter<DxfFile, DwgDrawing, DxfToDwgConverterOptions>
    {
        public Task<DwgDrawing> Convert(DxfFile source, DxfToDwgConverterOptions options)
        {
            var target = new DwgDrawing();
            target.FileHeader.Version = options.TargetVersion;
            target.LineTypes.Clear();
            target.Layers.Clear();

            // TODO: all the other things
            ConvertActiveViewPortSettings(source, target);
            ConvertDimensionStyles(source, target);
            ConvertLineTypes(source, target);
            ConvertLayers(source, target);
            ConvertBlocks(source, target);
            ConvertEntities(source, target);
            ConvertHeaderVariables(source, target);

            // ensure reference quality, since we may have re-created the objects
            target.CurrentEntityLineType = target.LineTypes[target.CurrentEntityLineType.Name];
            target.CurrentLayer = target.Layers[target.CurrentLayer.Name];

            return Task.FromResult(target);
        }

        private static void ConvertHeaderVariables(DxfFile source, DwgDrawing target)
        {
            target.FileHeader.MaintenenceVersion = source.Header.MaintenanceVersion; // $ACADMAINTVER
            if (short.TryParse(source.Header.DrawingCodePage, out var codePage)) target.FileHeader.CodePage = codePage; // $DWGCODEPAGE
            target.Variables.InsertionBase = source.Header.InsertionBase.ToDwgPoint(); // $INSBASE
            target.Variables.MinimumDrawingExtents = source.Header.MinimumDrawingExtents.ToDwgPoint(); // $EXTMIN
            target.Variables.MaximumDrawingExtents = source.Header.MaximumDrawingExtents.ToDwgPoint(); // $EXTMAX
            target.Variables.MinimumDrawingLimits = source.Header.MinimumDrawingLimits.ToDwgPoint(); // $LIMMIN
            target.Variables.MaximumDrawingLimits = source.Header.MaximumDrawingLimits.ToDwgPoint(); // $LIMMAX
            target.Variables.DrawOrthoganalLines = source.Header.DrawOrthoganalLines; // $ORTHOMODE
            target.Variables.UseRegenMode = source.Header.UseRegenMode; // $REGENMODE
            target.Variables.FillModeOn = source.Header.FillModeOn; // $FILLMODE
            target.Variables.UseQuickTextMode = source.Header.UseQuickTextMode; // $QTEXTMODE
            target.Variables.MirrorText = source.Header.MirrorText; // $MIRRTEXT
            target.Variables.DragMode = (DwgDragMode)source.Header.DragMode; // $DRAGMODE
            target.Variables.LineTypeScale = source.Header.LineTypeScale; // $LTSCALE
            target.Variables.ObjectSnapFlags = source.Header.ObjectSnapFlags; // $OSMODE
            target.Variables.AttributeVisibility = (DwgAttributeVisibility)source.Header.AttributeVisibility; // $ATTMODE
            target.Variables.DefaultTextHeight = source.Header.DefaultTextHeight; // $TEXTSIZE
            target.Variables.TraceWidth = source.Header.TraceWidth; // $TRACEWID
            // $TEXTSTYLE will be handled elsewhere
            // $CLAYER handled in `ConvertLayers()`
            // $CELTYPE handled in `ConvertLineTypes()`
            target.Variables.CurrentEntityColor = source.Header.CurrentEntityColor.ToDwgColor(); // $CECOLOR
            target.Variables.CurrentEntityLineTypeScale = source.Header.CurrentEntityLineTypeScale; // $CELTSCALE
            target.Variables.RetainDeletedObjects = source.Header.RetainDeletedObjects; // $DELOBJ
            target.Variables.DisplaySilhouetteCurvesInWireframeMode = source.Header.DisplaySilhouetteCurvesInWireframeMode; // $DISPSILH
            target.Variables.DimensioningScaleFactor = source.Header.DimensioningScaleFactor; // $DIMSCALE
            target.Variables.DimensioningArrowSize = source.Header.DimensioningArrowSize; // $DIMASZ
            target.Variables.DimensionExtensionLineOffset = source.Header.DimensionExtensionLineOffset; // $DIMEXO
            target.Variables.DimensionLineIncrement = source.Header.DimensionLineIncrement; // $DIMDLI
            target.Variables.DimensionDistanceRoundingValue = source.Header.DimensionDistanceRoundingValue; // $DIMRND
            target.Variables.DimensionLineExtension = source.Header.DimensionLineExtension; // $DIMDLE
            target.Variables.DimensionExtensionLineExtension = source.Header.DimensionExtensionLineExtension; // $DIMEXE
            target.Variables.DimensionPlusTolerance = source.Header.DimensionPlusTolerance; // $DIMTP
            target.Variables.DimensionMinusTolerance = source.Header.DimensionMinusTolerance; // $DIMTM
            target.Variables.DimensioningTextHeight = source.Header.DimensioningTextHeight; // $DIMTXT
            target.Variables.CenterMarkSize = source.Header.CenterMarkSize; // $DIMCEN
            target.Variables.DimensioningTickSize = source.Header.DimensioningTickSize; // $DIMTSZ
            target.Variables.GenerateDimensionTolerances = source.Header.GenerateDimensionTolerances; // $DIMTOL
            target.Variables.GenerateDimensionLimits = source.Header.GenerateDimensionLimits; // $DIMLIM
            target.Variables.DimensionTextInsideHorizontal = source.Header.DimensionTextInsideHorizontal; // $DIMTIH
            target.Variables.DimensionTextOutsideHorizontal = source.Header.DimensionTextOutsideHorizontal; // $DIMTOH
            target.Variables.SuppressFirstDimensionExtensionLine = source.Header.SuppressFirstDimensionExtensionLine; // $DIMSE1
            target.Variables.SuppressSecondDimensionExtensionLine = source.Header.SuppressSecondDimensionExtensionLine; // $DIMSE2
            target.Variables.TextAboveDimensionLine = source.Header.TextAboveDimensionLine; // $DIMTAD
            target.Variables.DimensionUnitZeroSuppression = (DwgUnitZeroSuppression)source.Header.DimensionUnitZeroSuppression; // $DIMZIN
            target.Variables.ArrowBlockName = source.Header.ArrowBlockName; // $DIMBLK
            target.Variables.CreateAssociativeDimensioning = source.Header.CreateAssociativeDimensioning; // $DIMASO
            target.Variables.RecomputeDimensionsWhileDragging = source.Header.RecomputeDimensionsWhileDragging; // $DIMSHO
            target.Variables.DimensioningSuffix = source.Header.DimensioningSuffix; // $DIMPOST
            target.Variables.AlternateDimensioningSuffix = source.Header.AlternateDimensioningSuffix; // $DIMAPOST
            target.Variables.UseAlternateDimensioning = source.Header.UseAlternateDimensioning; // $DIMALT
            target.Variables.AlternateDimensioningDecimalPlaces = source.Header.AlternateDimensioningDecimalPlaces; // $DIMALTD
            target.Variables.AlternateDimensioningScaleFactor = source.Header.AlternateDimensioningScaleFactor; // $DIMALTF
            target.Variables.DimensionLinearMeasurementsScaleFactor = source.Header.DimensionLinearMeasurementsScaleFactor; // $DIMLFAC
            target.Variables.ForceDimensionLineExtensionsOutsideIfTextIs = source.Header.ForceDimensionLineExtensionsOutsideIfTextIs; // $DIMTOFL
            target.Variables.DimensionVerticalTextPosition = source.Header.DimensionVerticalTextPosition; // $DIMTVP
            target.Variables.ForceDimensionTextInsideExtensions = source.Header.ForceDimensionTextInsideExtensions; // $DIMTIX
            target.Variables.SuppressOutsideExtensionDimensionLines = source.Header.SuppressOutsideExtensionDimensionLines; // $DIMSOXD
            target.Variables.UseSeparateArrowBlocksForDimensions = source.Header.UseSeparateArrowBlocksForDimensions; // $DIMSAH
            target.Variables.FirstArrowBlockName = source.Header.FirstArrowBlockName; // $DIMBLK1
            target.Variables.SecondArrowBlockName = source.Header.SecondArrowBlockName; // $DIMBLK2
            // $DIMSTYLE will be handled elsewhere
            target.Variables.DimensionLineColor = source.Header.DimensionLineColor.ToDwgColor(); // $DIMCLRD
            target.Variables.DimensionExtensionLineColor = source.Header.DimensionExtensionLineColor.ToDwgColor(); // $DIMCLRE
            target.Variables.DimensionTextColor = source.Header.DimensionTextColor.ToDwgColor(); // $DIMCLRT
            target.Variables.DimensionToleranceDisplayScaleFactor = source.Header.DimensionToleranceDisplayScaleFactor; // $DIMTFAC
            target.Variables.DimensionLineGap = source.Header.DimensionLineGap; // $DIMGAP
            target.Variables.DimensionTextJustification = (DwgDimensionTextJustification)source.Header.DimensionTextJustification; // $DIMJUST
            target.Variables.DimensionToleranceVerticalJustification = (DwgJustification)source.Header.DimensionToleranceVerticalJustification; // $DIMTOLJ
            target.Variables.DimensionToleranceZeroSuppression = (DwgUnitZeroSuppression)source.Header.DimensionToleranceZeroSuppression; // $DIMTZIN
            target.Variables.AlternateDimensioningZeroSupression = (DwgUnitZeroSuppression)source.Header.AlternateDimensioningZeroSupression; // $DIMALTZ
            target.Variables.AlternateDimensioningToleranceZeroSupression = (DwgUnitZeroSuppression)source.Header.AlternateDimensioningToleranceZeroSupression; // $DIMALTTZ
            target.Variables.DimensionTextAndArrowPlacement = (DwgDimensionFit)source.Header.DimensionTextAndArrowPlacement; // $DIMFIT
            target.Variables.DimensionCursorControlsTextPosition = source.Header.DimensionCursorControlsTextPosition; // $DIMUPT
            target.Variables.DimensionUnitFormat = (DwgUnitFormat)source.Header.DimensionUnitFormat; // $DIMUNIT
            target.Variables.DimensionUnitToleranceDecimalPlaces = source.Header.DimensionUnitToleranceDecimalPlaces; // $DIMDEC
            target.Variables.DimensionToleranceDecimalPlaces = source.Header.DimensionToleranceDecimalPlaces; // $DIMTDEC
            target.Variables.AlternateDimensioningUnits = (DwgUnitFormat)source.Header.AlternateDimensioningUnits; // $DIMALTU
            target.Variables.AlternateDimensioningToleranceDecimalPlaces = source.Header.AlternateDimensioningToleranceDecimalPlaces; // $DIMALTTD
            // $DIMTXSTY will be handled elsewhere
            target.Variables.DimensioningAngleFormat = (DwgAngleFormat)source.Header.DimensioningAngleFormat; // $DIMAUNIT
            target.Variables.UnitFormat = (DwgUnitFormat)source.Header.UnitFormat; // $LUNITS
            target.Variables.UnitPrecision = source.Header.UnitPrecision; // $LUPREC
            target.Variables.SketchRecordIncrement = source.Header.SketchRecordIncrement; // $SKETCHINC
            target.Variables.FilletRadius = source.Header.FilletRadius; // $FILLETRAD
            target.Variables.AngleUnitFormat = (DwgAngleFormat)source.Header.AngleUnitFormat; // $AUNITS
            target.Variables.AngleUnitPrecision = source.Header.AngleUnitPrecision; // $AUPREC
            target.Variables.FileName = source.Header.FileName; // $MENU
            target.Variables.Elevation = source.Header.Elevation; // $ELEVATION
            target.Variables.PaperSpaceElevation = source.Header.PaperspaceElevation; // $PELEVATION
            target.Variables.Thickness = source.Header.Thickness; // $THICKNESS
            target.Variables.UseLimitsChecking = source.Header.UseLimitsChecking; // $LIMCHECK
            target.Variables.BlipMode = source.Header.BlipMode; // $BLIPMODE
            target.Variables.FirstChamferDistance = source.Header.FirstChamferDistance; // $CHAMFERA
            target.Variables.SecondChamferDistance = source.Header.SecondChamferDistance; // $CHAMFERB
            target.Variables.ChamferLength = source.Header.ChamferLength; // $CHAMFERC
            target.Variables.ChamferAngle = source.Header.ChamferAngle; // $CHAMFERD
            target.Variables.PolylineSketchMode = (DwgPolySketchMode)source.Header.PolylineSketchMode; // $SKPOLY
            target.Variables.CreationDate = source.Header.CreationDate; // $TDCREATE
            target.Variables.UpdateDate = source.Header.UpdateDate; // $TDUPDATE
            target.Variables.TimeInDrawing = source.Header.TimeInDrawing; // $TDINDWG
            target.Variables.UserElapsedTimer = source.Header.UserElapsedTimer; // $TDUSRTIMER
            target.Variables.UserTimerOn = source.Header.UserTimerOn; // $USRTIMER
            target.Variables.AngleZeroDirection = source.Header.AngleZeroDirection; // $ANGBASE
            target.Variables.AngleDirection = (DwgAngleDirection)source.Header.AngleDirection; // $ANGDIR
            target.Variables.PointDisplayMode = (short)source.Header.PointDisplayMode; // $PDMODE
            target.Variables.PointDisplaySize = source.Header.PointDisplaySize; // $PDSIZE
            target.Variables.DefaultPolylineWidth = source.Header.DefaultPolylineWidth; // $PLINEWID
            target.Variables.CoordinateDisplay = (DwgCoordinateDisplay)source.Header.CoordinateDisplay; // $COORDS
            target.Variables.DisplaySplinePolygonControl = source.Header.DisplaySplinePolygonControl; // $SPLFRAME
            target.Variables.PEditSplineCurveType = (DwgPolylineCurvedAndSmoothSurfaceType)source.Header.PEditSplineCurveType; // $SPLINETYPE
            target.Variables.LineSegmentsPerSplinePatch = source.Header.LineSegmentsPerSplinePatch; // $SPLINESEGS
            target.Variables.ShowAttributeEntryDialogs = source.Header.ShowAttributeEntryDialogs; // $ATTDIA
            target.Variables.PromptForAttributeOnInsert = source.Header.PromptForAttributeOnInsert; // $ATTREQ
            target.Variables.NextAvailableHandle = new DwgHandleReference(DwgHandleReferenceCode.Declaration, (uint)source.Header.NextAvailableHandle.Value); // $HANDSEED
            target.Variables.MeshTabulationsInFirstDirection = source.Header.MeshTabulationsInFirstDirection; // $SURFTAB1
            target.Variables.MeshTabulationsInSecondDirection = source.Header.MeshTabulationsInSecondDirection; // $SURFTAB2
            target.Variables.PEditSmoothSurfaceType = (DwgPolylineCurvedAndSmoothSurfaceType)source.Header.PEditSmoothSurfaceType; // $SURFTYPE
            target.Variables.PEditSmoothMDensity = source.Header.PEditSmoothMDensity; // $SURFU
            target.Variables.PEditSmoothNDensity = source.Header.PEditSmoothNDensity; // $SURFV
            // $UCSNAME will be handled elsewhere
            target.Variables.UCSOrigin = source.Header.UCSOrigin.ToDwgPoint(); // $UCSORG
            target.Variables.UCSXAxis = source.Header.UCSXAxis.ToDwgVector(); // $UCSXDIR
            target.Variables.UCSYAxis = source.Header.UCSYAxis.ToDwgVector(); // $UCSYDIR
            // $PUCSNAME will be handled elsewhere
            target.Variables.PaperSpaceUCSOrigin = source.Header.PaperspaceUCSOrigin.ToDwgPoint(); // $PUCSORG
            target.Variables.PaperSpaceUCSXAxis = source.Header.PaperspaceXAxis.ToDwgVector(); // $PUCSXDIR
            target.Variables.PaperSpaceUCSYAxis = source.Header.PaperspaceYAxis.ToDwgVector(); // $PUCSYDIR
            target.Variables.UserInt1 = source.Header.UserInt1; // $USERI1
            target.Variables.UserInt2 = source.Header.UserInt2; // $USERI2
            target.Variables.UserInt3 = source.Header.UserInt3; // $USERI3
            target.Variables.UserInt4 = source.Header.UserInt4; // $USERI4
            target.Variables.UserInt5 = source.Header.UserInt5; // $USERI5
            target.Variables.UserReal1 = source.Header.UserReal1; // $USERR1
            target.Variables.UserReal2 = source.Header.UserReal2; // $USERR2
            target.Variables.UserReal3 = source.Header.UserReal3; // $USERR3
            target.Variables.UserReal4 = source.Header.UserReal4; // $USERR4
            target.Variables.UserReal5 = source.Header.UserReal5; // $USERR5
            target.Variables.SetUCSToWCSInDViewOrVPoint = source.Header.SetUCSToWCSInDViewOrVPoint; // $WORLDVIEW
            target.Variables.EdgeShading = (DwgShadeEdgeMode)source.Header.EdgeShading; // $SHADEDGE
            target.Variables.PercentAmbientToDiffuse = source.Header.PercentAmbientToDiffuse; // $SHADEDIF
            target.Variables.PreviousReleaseTileCompatability = source.Header.PreviousReleaseTileCompatibility; // $TILEMODE
            target.Variables.MaximumActiveViewports = source.Header.MaximumActiveViewports; // $MAXACTVP
            target.Variables.PaperSpaceInsertionBase = source.Header.PaperspaceInsertionBase.ToDwgPoint(); // $PINSBASE
            target.Variables.LimitCheckingInPaperSpace = source.Header.LimitCheckingInPaperspace; // $PLIMCHECK
            target.Variables.PaperSpaceMinimumDrawingExtents = source.Header.PaperspaceMinimumDrawingExtents.ToDwgPoint(); // $PEXTMIN
            target.Variables.PaperSpaceMaximumDrawingExtents = source.Header.PaperspaceMaximumDrawingExtents.ToDwgPoint(); // $PEXTMAX
            target.Variables.PaperSpaceMinimumDrawingLimits = source.Header.PaperspaceMinimumDrawingLimits.ToDwgPoint(); // $PLIMMIN
            target.Variables.PaperSpaceMaximumDrawingLimits = source.Header.PaperspaceMaximumDrawingLimits.ToDwgPoint(); // $PLIMMAX
            target.Variables.DisplayFractionsInInput = source.Header.DisplayFractionsInInput; // $UNITMODE
            target.Variables.RetainXRefDependentVisibilitySettings = source.Header.RetainXRefDependentVisibilitySettings; // $VISRETAIN
            target.Variables.IsPolylineContinuousAroundVerticies = source.Header.IsPolylineContinuousAroundVertices; // $PLINEGEN
            target.Variables.SpacialIndexMaxDepth = source.Header.SpacialIndexMaxDepth; // $TREEDEPTH
            target.Variables.PickStyle = (DwgPickStyle)source.Header.PickStyle; // $PICKSTYLE
            // $CMLSTYLE will be handled elsewhere
            target.Variables.CurrentMultilineJustification = (DwgJustification)source.Header.CurrentMultilineJustification; // $CMLJUST
            target.Variables.CurrentMultilineScale = source.Header.CurrentMultilineScale; // $CMLSCALE
            target.Variables.SaveProxyGraphics = source.Header.SaveProxyGraphics; // $PROXYGRAPHICS
            // $CMATERIAL will be handled elsewhere
        }

        private static void ConvertActiveViewPortSettings(DxfFile source, DwgDrawing target)
        {
            if (source.ActiveViewPort is object)
            {
                target.ViewPorts["*ACTIVE"].LowerLeft = source.ActiveViewPort.LowerLeft.ToDwgPoint();
                target.ViewPorts["*ACTIVE"].Height = source.ActiveViewPort.ViewHeight;
            }
        }

        private static void ConvertBlocks(DxfFile source, DwgDrawing target)
        {
            foreach (var block in source.Blocks)
            {
                var blockName = block.Name.ToUpperInvariant();
                if (blockName == "*MODEL_SPACE" ||
                    blockName == "*PAPER_SPACE")
                {
                    // these are special-cased elsewhere
                    continue;
                }

                var layer = target.EnsureLayer(block.Layer, DwgColor.FromIndex(1), source.Header.CurrentEntityLineType);
                var dwgBlockHeader = DwgBlockHeader.CreateBlockRecordWithName(block.Name, layer);
                dwgBlockHeader.BasePoint = block.BasePoint.ToDwgPoint();
                target.BlockHeaders.Add(dwgBlockHeader);

                foreach (var entity in block.Entities)
                {
                    ConvertAndAddEntityToBlockHeader(entity, target, dwgBlockHeader.Name);
                }
            }
        }

        private static void ConvertDimensionStyles(DxfFile source, DwgDrawing target)
        {
            foreach (var dimStyle in source.DimensionStyles)
            {
                var dwgDimStyle = new DwgDimStyle(dimStyle.Name)
                {
                    // regular properties
                    DimensioningSuffix = dimStyle.DimensioningSuffix,
                    AlternateDimensioningSuffix = dimStyle.AlternateDimensioningSuffix,
                    ArrowBlockName = dimStyle.ArrowBlockName,
                    FirstArrowBlockName = dimStyle.FirstArrowBlockName,
                    SecondArrowBlockName = dimStyle.SecondArrowBlockName,
                    DimensioningScaleFactor = dimStyle.DimensioningScaleFactor,
                    DimensioningArrowSize = dimStyle.DimensioningArrowSize,
                    DimensionExtensionLineOffset = dimStyle.DimensionExtensionLineOffset,
                    DimensionLineIncrement = dimStyle.DimensionLineIncrement,
                    DimensionExtensionLineExtension = dimStyle.DimensionExtensionLineExtension,
                    DimensionDistanceRoundingValue = dimStyle.DimensionDistanceRoundingValue,
                    DimensionLineExtension = dimStyle.DimensionLineExtension,
                    DimensionPlusTolerance = dimStyle.DimensionPlusTolerance,
                    DimensionMinusTolerance = dimStyle.DimensionMinusTolerance,
                    GenerateDimensionTolerances = dimStyle.GenerateDimensionTolerances,
                    GenerateDimensionLimits = dimStyle.GenerateDimensionLimits,
                    DimensionTextInsideHorizontal = dimStyle.DimensionTextInsideHorizontal,
                    DimensionTextOutsideHorizontal = dimStyle.DimensionTextOutsideHorizontal,
                    SuppressFirstDimensionExtensionLine = dimStyle.SuppressFirstDimensionExtensionLine,
                    SuppressSecondDimensionExtensionLine = dimStyle.SuppressSecondDimensionExtensionLine,
                    TextAboveDimensionLine = dimStyle.TextAboveDimensionLine,
                    DimensionUnitZeroSuppression = (DwgUnitZeroSuppression)dimStyle.DimensionUnitZeroSuppression,
                    DimensioningTextHeight = dimStyle.DimensioningTextHeight,
                    CenterMarkSize = dimStyle.CenterMarkSize,
                    DimensioningTickSize = dimStyle.DimensioningTickSize,
                    AlternateDimensioningScaleFactor = dimStyle.AlternateDimensioningScaleFactor,
                    DimensionLinearMeasurementsScaleFactor = dimStyle.DimensionLinearMeasurementScaleFactor,
                    DimensionVerticalTextPosition = dimStyle.DimensionVerticalTextPosition,
                    DimensionToleranceDisplayScaleFactor = dimStyle.DimensionToleranceDisplaceScaleFactor,
                    DimensionLineGap = dimStyle.DimensionLineGap,
                    UseAlternateDimensioning = dimStyle.UseAlternateDimensioning,
                    AlternateDimensioningDecimalPlaces = dimStyle.AlternateDimensioningDecimalPlaces,
                    ForceDimensionLineExtensionsOutsideIfTextIs = dimStyle.ForceDimensionLineExtensionsOutsideIfTextExists,
                    UseSeparateArrowBlocksForDimensions = dimStyle.UseSeparateArrowBlocksForDimensions,
                    ForceDimensionTextInsideExtensions = dimStyle.ForceDimensionTextInsideExtensions,
                    SuppressOutsideExtensionDimensionLines = dimStyle.SuppressOutsideExtensionDimensionLines,
                    DimensionLineColor = dimStyle.DimensionLineColor?.ToDwgColor() ?? DwgColor.ByBlock,
                    DimensionExtensionLineColor = dimStyle.DimensionExtensionLineColor?.ToDwgColor() ?? DwgColor.ByBlock,
                    DimensionTextColor = dimStyle.DimensionTextColor?.ToDwgColor() ?? DwgColor.ByBlock,
                    DimensionUnitFormat = (DwgUnitFormat)dimStyle.DimensionUnitFormat,
                    DimensionUnitToleranceDecimalPlaces = dimStyle.DimensionUnitToleranceDecimalPlaces,
                    DimensionToleranceDecimalPlaces = dimStyle.DimensionToleranceDecimalPlaces,
                    AlternateDimensioningUnits = (DwgUnitFormat)dimStyle.AlternateDimensioningUnits,
                    AlternateDimensioningToleranceDecimalPlaces = dimStyle.AlternateDimensioningToleranceDecimalPlaces,
                    DimensioningAngleFormat = (DwgAngleFormat)dimStyle.DimensioningAngleFormat,
                    DimensionTextJustification = (DwgDimensionTextJustification)dimStyle.DimensionTextJustification,
                    DimensionToleranceVerticalJustification = (DwgJustification)dimStyle.DimensionToleranceVerticalJustification,
                    DimensionToleranceZeroSuppression = (DwgUnitZeroSuppression)dimStyle.DimensionToleranceZeroSuppression,
                    AlternateDimensioningZeroSupression = (DwgUnitZeroSuppression)dimStyle.AlternateDimensioningZeroSuppression,
                    AlternateDimensioningToleranceZeroSupression = (DwgUnitZeroSuppression)dimStyle.AlternateDimensioningToleranceZeroSuppression,
                    DimensionTextAndArrowPlacement = (DwgDimensionFit)dimStyle.DimensionTextAndArrowPlacement,
                    DimensionCursorControlsTextPosition = dimStyle.DimensionCursorControlsTextPosition,

                    // other properties
                    Style = target.Styles[dimStyle.DimensionTextStyle ?? source.Header.DimensionTextStyle],
                };
                target.DimStyles.Remove(dimStyle.Name);
                target.DimStyles.Add(dwgDimStyle);
            }

            target.DimensionStyle = target.DimStyles[target.DimensionStyle.Name];
        }

        private static void ConvertLineTypes(DxfFile source, DwgDrawing target)
        {
            foreach (var lineType in source.LineTypes)
            {
                var dwgLineType = new DwgLineType(lineType.Name)
                {
                    Description = lineType.Description,
                    PatternLength = lineType.TotalPatternLength,
                };
                foreach (var dashElement in lineType.Elements)
                {
                    var dashInfo = new DwgLineType.DwgLineTypeDashInfo(dashElement.DashDotSpaceLength);
                    dwgLineType.DashInfos.Add(dashInfo);
                }

                target.LineTypes.Add(dwgLineType);
            }

            if (source.Header.CurrentEntityLineType is object &&
                !target.LineTypes.ContainsKey(source.Header.CurrentEntityLineType))
            {
                // current line type doesn't exist; create it
                var lineType = target.LineTypeOrCurrent(source.Header.CurrentEntityLineType);
                target.LineTypes.Add(lineType);
                target.CurrentEntityLineType = lineType;
            }
        }

        private static void ConvertLayers(DxfFile source, DwgDrawing target)
        {
            foreach (var layer in source.Layers)
            {
                var dwgLayer = new DwgLayer(layer.Name)
                {
                    Color = layer.Color.ToDwgColor(),
                    LineType = target.LineTypeOrCurrent(layer.LineTypeName),
                };
                target.Layers.Add(dwgLayer);
            }

            target.EnsureLineType("BYLAYER");
            target.EnsureLineType("BYBLOCK");
            target.EnsureLineType("CONTINUOUS");
            target.EnsureLayer("0", DwgColor.ByLayer, "CONTINUOUS");

            target.CurrentLayer = target.LayerOrCurrent(source.Header.CurrentLayer);
            target.ModelSpaceBlockRecord.Block.Layer = target.LayerOrCurrent(target.ModelSpaceBlockRecord.Block.Layer.Name);
            target.ModelSpaceBlockRecord.EndBlock.Layer = target.LayerOrCurrent(target.ModelSpaceBlockRecord.Block.Layer.Name);
            target.PaperSpaceBlockRecord.Block.Layer = target.LayerOrCurrent(target.PaperSpaceBlockRecord.Block.Layer.Name);
            target.PaperSpaceBlockRecord.EndBlock.Layer = target.LayerOrCurrent(target.PaperSpaceBlockRecord.Block.Layer.Name);
        }

        private static void ConvertEntities(DxfFile source, DwgDrawing target)
        {
            foreach (var entity in source.Entities)
            {
                ConvertAndAddEntityToBlockHeader(entity, target, target.ModelSpaceBlockRecord.Name);
            }
        }

        private static void ConvertAndAddEntityToBlockHeader(DxfEntity entity, DwgDrawing drawing, string blockHeaderName)
        {
            switch (entity)
            {
                case DxfAlignedDimension aligned:
                    AddToDrawing(aligned.ToDwgAlignedDimension(drawing), aligned.Layer, aligned.LineTypeName);
                    break;
                case DxfArc arc:
                    AddToDrawing(arc.ToDwgArc(), arc.Layer, entity.LineTypeName);
                    break;
                case DxfCircle circle:
                    AddToDrawing(circle.ToDwgCircle(), circle.Layer, entity.LineTypeName);
                    break;
                case DxfEllipse ellipse:
                    AddToDrawing(ellipse.ToDwgEllipse(), ellipse.Layer, ellipse.LineTypeName);
                    break;
                case DxfInsert insert:
                    AddToDrawing(insert.ToDwgInsert(drawing), insert.Layer, insert.LineTypeName);
                    break;
                case DxfLine line:
                    AddToDrawing(line.ToDwgLine(), line.Layer, entity.LineTypeName);
                    break;
                case DxfModelPoint modelPoint:
                    AddToDrawing(modelPoint.ToDwgLocation(), modelPoint.Layer, entity.LineTypeName);
                    break;
                case DxfLwPolyline lwpolyline:
                    AddToDrawing(lwpolyline.ToDwgLwPolyline(), lwpolyline.Layer, entity.LineTypeName);
                    break;
                case DxfPolyline polyline:
                    AddToDrawing(polyline.ToDwgPolyline(), polyline.Layer, entity.LineTypeName);
                    break;
                case DxfRotatedDimension rotated:
                    AddToDrawing(rotated.ToDwgRotatedDimension(drawing), rotated.Layer, rotated.LineTypeName);
                    break;
                case DxfSpline spline:
                    AddToDrawing(spline.ToDwgSpline(), spline.Layer, entity.LineTypeName);
                    break;
                case DxfText text:
                    AddToDrawing(text.ToDwgText(), text.Layer, text.LineTypeName);
                    break;
                default:
                    // TODO: everything else
                    break;
            }

            void AddToDrawing(DwgEntity dwgEntity, string layerName, string lineTypeName)
            {
                AddEntityToBlockHeader(drawing, blockHeaderName, dwgEntity, layerName, lineTypeName);
            }
        }

        private static void AddEntityToBlockHeader(DwgDrawing drawing, string blockHeaderName, DwgEntity entity, string layerName, string lineTypeName)
        {
            entity.Layer = drawing.EnsureLayer(layerName, DwgColor.FromIndex(1), lineTypeName);
            entity.LineType = drawing.EnsureLineType(lineTypeName);
            drawing.BlockHeaders[blockHeaderName].Entities.Add(entity);
        }
    }
}
