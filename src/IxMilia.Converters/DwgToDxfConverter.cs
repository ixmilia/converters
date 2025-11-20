using System.Threading.Tasks;
using IxMilia.Dwg;
using IxMilia.Dwg.Objects;
using IxMilia.Dxf;
using IxMilia.Dxf.Blocks;
using IxMilia.Dxf.Entities;

namespace IxMilia.Converters
{
    public struct DwgToDxfConverterOptions
    {
        public DxfAcadVersion TargetVersion { get; set; }

        public DwgToDxfConverterOptions(DxfAcadVersion targetVersion)
        {
            TargetVersion = targetVersion;
        }
    }

    public class DwgToDxfConverter : IConverter<DwgDrawing, DxfFile, DwgToDxfConverterOptions>
    {
        public Task<DxfFile> Convert(DwgDrawing source, DwgToDxfConverterOptions options)
        {
            var result = new DxfFile();
            result.Layers.Clear();
            result.Header.Version = options.TargetVersion;
            result.Header.CurrentLayer = source.CurrentLayer.Name;

            if (result.ActiveViewPort is not null)
            {
                result.ActiveViewPort.LowerLeft = source.ViewPorts["*ACTIVE"].LowerLeft.ToDxfPoint();
                result.ActiveViewPort.ViewHeight = source.ViewPorts["*ACTIVE"].Height;
            }

            // TODO: convert the other things
            ConvertHeaderVariables(result, source);

            // blocks
            foreach (var blockHeader in source.BlockHeaders.Values)
            {
                if (blockHeader.IsModelSpaceBlock || blockHeader.IsPaperSpaceBlock)
                {
                    // these are special-cased elsewhere
                    continue;
                }

                var dxfBlock = new DxfBlock(blockHeader.Name)
                {
                    BasePoint = blockHeader.BasePoint.ToDxfPoint(),
                    Layer = blockHeader.Block.Layer.Name,
                };
                foreach (var entity in blockHeader.Entities)
                {
                    var dxfEntity = ConvertEntity(entity);
                    if (dxfEntity is not null)
                    {
                        dxfBlock.Entities.Add(dxfEntity);
                    }
                }

                result.Blocks.Add(dxfBlock);
            }

            // dim styles
            result.DimensionStyles.Clear();
            foreach (var dimStyle in source.DimStyles.Values)
            {
                var dxfDimStyle = new DxfDimStyle(dimStyle.Name)
                {
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
                    DimensionUnitZeroSuppression = (DxfUnitZeroSuppression)dimStyle.DimensionUnitZeroSuppression,
                    DimensioningTextHeight = dimStyle.DimensioningTextHeight,
                    CenterMarkSize = dimStyle.CenterMarkSize,
                    DimensioningTickSize = dimStyle.DimensioningTickSize,
                    AlternateDimensioningScaleFactor = dimStyle.AlternateDimensioningScaleFactor,
                    DimensionLinearMeasurementScaleFactor = dimStyle.DimensionLinearMeasurementsScaleFactor,
                    DimensionVerticalTextPosition = dimStyle.DimensionVerticalTextPosition,
                    DimensionToleranceDisplaceScaleFactor = dimStyle.DimensionToleranceDisplayScaleFactor,
                    DimensionLineGap = dimStyle.DimensionLineGap,
                    UseAlternateDimensioning = dimStyle.UseAlternateDimensioning,
                    AlternateDimensioningDecimalPlaces = dimStyle.AlternateDimensioningDecimalPlaces,
                    ForceDimensionLineExtensionsOutsideIfTextExists = dimStyle.ForceDimensionLineExtensionsOutsideIfTextIs,
                    UseSeparateArrowBlocksForDimensions = dimStyle.UseSeparateArrowBlocksForDimensions,
                    ForceDimensionTextInsideExtensions = dimStyle.ForceDimensionTextInsideExtensions,
                    SuppressOutsideExtensionDimensionLines = dimStyle.SuppressOutsideExtensionDimensionLines,
                    DimensionLineColor = dimStyle.DimensionLineColor.ToDxfColor(),
                    DimensionExtensionLineColor = dimStyle.DimensionExtensionLineColor.ToDxfColor(),
                    DimensionTextColor = dimStyle.DimensionTextColor.ToDxfColor(),
                    DimensionUnitFormat = (DxfUnitFormat)dimStyle.DimensionUnitFormat,
                    DimensionUnitToleranceDecimalPlaces = dimStyle.DimensionUnitToleranceDecimalPlaces,
                    DimensionToleranceDecimalPlaces = dimStyle.DimensionToleranceDecimalPlaces,
                    AlternateDimensioningUnits = (DxfUnitFormat)dimStyle.AlternateDimensioningUnits,
                    AlternateDimensioningToleranceDecimalPlaces = dimStyle.AlternateDimensioningToleranceDecimalPlaces,
                    DimensioningAngleFormat = (DxfAngleFormat)dimStyle.DimensioningAngleFormat,
                    DimensionTextJustification = (DxfDimensionTextJustification)dimStyle.DimensionTextJustification,
                    DimensionToleranceVerticalJustification = (DxfJustification)dimStyle.DimensionToleranceVerticalJustification,
                    DimensionToleranceZeroSuppression = (DxfToleranceZeroSuppression)dimStyle.DimensionToleranceZeroSuppression,
                    AlternateDimensioningZeroSuppression = (DxfAlternateUnitZeroSuppression)dimStyle.AlternateDimensioningZeroSupression,
                    AlternateDimensioningToleranceZeroSuppression = (DxfAlternateToleranceZeroSuppression)dimStyle.AlternateDimensioningToleranceZeroSupression,
                    DimensionTextAndArrowPlacement = (DxfDimensionFit)dimStyle.DimensionTextAndArrowPlacement,
                    DimensionCursorControlsTextPosition = dimStyle.DimensionCursorControlsTextPosition,
                };
                result.DimensionStyles.Add(dxfDimStyle);
            }

            // layers
            foreach (var layer in source.Layers.Values)
            {
                result.Layers.Add(new DxfLayer(layer.Name, layer.Color.ToDxfColor()));
            }

            // line types
            foreach (var lineType in source.LineTypes.Values)
            {
                var dxfLineType = new DxfLineType(lineType.Name)
                {
                    Description = lineType.Description,
                    TotalPatternLength = lineType.PatternLength
                };
                foreach (var dashInfo in lineType.DashInfos)
                {
                    var dashElement = new DxfLineTypeElement() { DashDotSpaceLength = dashInfo.DashLength };
                    dxfLineType.Elements.Add(dashElement);
                }

                result.LineTypes.Add(dxfLineType);
            }

            // entities
            foreach (var entity in source.ModelSpaceBlockRecord.Entities)
            {
                var dxfEntity = ConvertEntity(entity);
                if (dxfEntity is not null)
                {
                    result.Entities.Add(dxfEntity);
                }
            }

            return Task.FromResult(result);
        }

        private static void ConvertHeaderVariables(DxfFile target, DwgDrawing source)
        {
            target.Header.MaintenanceVersion = source.FileHeader.MaintenenceVersion; // $ACADMAINTVER
            target.Header.DrawingCodePage = source.FileHeader.CodePage.ToString(); // $DWGCODEPAGE
            target.Header.InsertionBase = source.Variables.InsertionBase.ToDxfPoint(); // $INSBASE
            target.Header.MinimumDrawingExtents = source.Variables.MinimumDrawingExtents.ToDxfPoint(); // $EXTMIN
            target.Header.MaximumDrawingExtents = source.Variables.MaximumDrawingExtents.ToDxfPoint(); // $EXTMAX
            target.Header.MinimumDrawingLimits = source.Variables.MinimumDrawingLimits.ToDxfPoint(); // $LIMMIN
            target.Header.MaximumDrawingLimits = source.Variables.MaximumDrawingLimits.ToDxfPoint(); // $LIMMAX
            target.Header.DrawOrthoganalLines = source.Variables.DrawOrthoganalLines; // $ORTHOMODE
            target.Header.UseRegenMode = source.Variables.UseRegenMode; // $REGENMODE
            target.Header.FillModeOn = source.Variables.FillModeOn; // $FILLMODE
            target.Header.UseQuickTextMode = source.Variables.UseQuickTextMode; // $QTEXTMODE
            target.Header.MirrorText = source.Variables.MirrorText; // $MIRRTEXT
            target.Header.DragMode = (DxfDragMode)source.Variables.DragMode; // $DRAGMODE
            target.Header.LineTypeScale = source.Variables.LineTypeScale; // $LTSCALE
            target.Header.ObjectSnapFlags = source.Variables.ObjectSnapFlags; // $OSMODE
            target.Header.AttributeVisibility = (DxfAttributeVisibility)source.Variables.AttributeVisibility; // $ATTMODE
            target.Header.DefaultTextHeight = source.Variables.DefaultTextHeight; // $TEXTSIZE
            target.Header.TraceWidth = source.Variables.TraceWidth; // $TRACEWID
            // $TEXTSTYLE will be handled elsewhere
            // $CLAYER will be handled elsewhere
            // $CELTYPE will be handled elsewhere
            target.Header.CurrentEntityColor = source.Variables.CurrentEntityColor.ToDxfColor(); // $CECOLOR
            target.Header.CurrentEntityLineTypeScale = source.Variables.CurrentEntityLineTypeScale; // $CELTSCALE
            target.Header.RetainDeletedObjects = source.Variables.RetainDeletedObjects; // $DELOBJ
            target.Header.DisplaySilhouetteCurvesInWireframeMode = source.Variables.DisplaySilhouetteCurvesInWireframeMode; // $DISPSILH
            target.Header.DimensioningScaleFactor = source.Variables.DimensioningScaleFactor; // $DIMSCALE
            target.Header.DimensioningArrowSize = source.Variables.DimensioningArrowSize; // $DIMASZ
            target.Header.DimensionExtensionLineOffset = source.Variables.DimensionExtensionLineOffset; // $DIMEXO
            target.Header.DimensionLineIncrement = source.Variables.DimensionLineIncrement; // $DIMDLI
            target.Header.DimensionDistanceRoundingValue = source.Variables.DimensionDistanceRoundingValue; // $DIMRND
            target.Header.DimensionLineExtension = source.Variables.DimensionLineExtension; // $DIMDLE
            target.Header.DimensionExtensionLineExtension = source.Variables.DimensionExtensionLineExtension; // $DIMEXE
            target.Header.DimensionPlusTolerance = source.Variables.DimensionPlusTolerance; // $DIMTP
            target.Header.DimensionMinusTolerance = source.Variables.DimensionMinusTolerance; // $DIMTM
            target.Header.DimensioningTextHeight = source.Variables.DimensioningTextHeight; // $DIMTXT
            target.Header.CenterMarkSize = source.Variables.CenterMarkSize; // $DIMCEN
            target.Header.DimensioningTickSize = source.Variables.DimensioningTickSize; // $DIMTSZ
            target.Header.GenerateDimensionTolerances = source.Variables.GenerateDimensionTolerances; // $DIMTOL
            target.Header.GenerateDimensionLimits = source.Variables.GenerateDimensionLimits; // $DIMLIM
            target.Header.DimensionTextInsideHorizontal = source.Variables.DimensionTextInsideHorizontal; // $DIMTIH
            target.Header.DimensionTextOutsideHorizontal = source.Variables.DimensionTextOutsideHorizontal; // $DIMTOH
            target.Header.SuppressFirstDimensionExtensionLine = source.Variables.SuppressFirstDimensionExtensionLine; // $DIMSE1
            target.Header.SuppressSecondDimensionExtensionLine = source.Variables.SuppressSecondDimensionExtensionLine; // $DIMSE2
            target.Header.TextAboveDimensionLine = source.Variables.TextAboveDimensionLine; // $DIMTAD
            target.Header.DimensionUnitZeroSuppression = (DxfUnitZeroSuppression)source.Variables.DimensionUnitZeroSuppression; // $DIMZIN
            target.Header.ArrowBlockName = source.Variables.ArrowBlockName; // $DIMBLK
            target.Header.CreateAssociativeDimensioning = source.Variables.CreateAssociativeDimensioning; // $DIMASO
            target.Header.RecomputeDimensionsWhileDragging = source.Variables.RecomputeDimensionsWhileDragging; // $DIMSHO
            target.Header.DimensioningSuffix = source.Variables.DimensioningSuffix; // $DIMPOST
            target.Header.AlternateDimensioningSuffix = source.Variables.AlternateDimensioningSuffix; // $DIMAPOST
            target.Header.UseAlternateDimensioning = source.Variables.UseAlternateDimensioning; // $DIMALT
            target.Header.AlternateDimensioningDecimalPlaces = source.Variables.AlternateDimensioningDecimalPlaces; // $DIMALTD
            target.Header.AlternateDimensioningScaleFactor = source.Variables.AlternateDimensioningScaleFactor; // $DIMALTF
            target.Header.DimensionLinearMeasurementsScaleFactor = source.Variables.DimensionLinearMeasurementsScaleFactor; // $DIMLFAC
            target.Header.ForceDimensionLineExtensionsOutsideIfTextIs = source.Variables.ForceDimensionLineExtensionsOutsideIfTextIs; // $DIMTOFL
            target.Header.DimensionVerticalTextPosition = source.Variables.DimensionVerticalTextPosition; // $DIMTVP
            target.Header.ForceDimensionTextInsideExtensions = source.Variables.ForceDimensionTextInsideExtensions; // $DIMTIX
            target.Header.SuppressOutsideExtensionDimensionLines = source.Variables.SuppressOutsideExtensionDimensionLines; // $DIMSOXD
            target.Header.UseSeparateArrowBlocksForDimensions = source.Variables.UseSeparateArrowBlocksForDimensions; // $DIMSAH
            target.Header.FirstArrowBlockName = source.Variables.FirstArrowBlockName; // $DIMBLK1
            target.Header.SecondArrowBlockName = source.Variables.SecondArrowBlockName; // $DIMBLK2
            // $DIMSTYLE will be handled elsewhere
            target.Header.DimensionLineColor = source.Variables.DimensionLineColor.ToDxfColor(); // $DIMCLRD
            target.Header.DimensionExtensionLineColor = source.Variables.DimensionExtensionLineColor.ToDxfColor(); // $DIMCLRE
            target.Header.DimensionTextColor = source.Variables.DimensionTextColor.ToDxfColor(); // $DIMCLRT
            target.Header.DimensionToleranceDisplayScaleFactor = source.Variables.DimensionToleranceDisplayScaleFactor; // $DIMTFAC
            target.Header.DimensionLineGap = source.Variables.DimensionLineGap; // $DIMGAP
            target.Header.DimensionTextJustification = (DxfDimensionTextJustification)source.Variables.DimensionTextJustification; // $DIMJUST
            target.Header.DimensionToleranceVerticalJustification = (DxfJustification)source.Variables.DimensionToleranceVerticalJustification; // $DIMTOLJ
            target.Header.DimensionToleranceZeroSuppression = (DxfToleranceZeroSuppression)source.Variables.DimensionToleranceZeroSuppression; // $DIMTZIN
            target.Header.AlternateDimensioningZeroSupression = (DxfAlternateUnitZeroSuppression)source.Variables.AlternateDimensioningZeroSupression; // $DIMALTZ
            target.Header.AlternateDimensioningToleranceZeroSupression = (DxfAlternateToleranceZeroSuppression)source.Variables.AlternateDimensioningToleranceZeroSupression; // $DIMALTTZ
            target.Header.DimensionTextAndArrowPlacement = (DxfDimensionFit)source.Variables.DimensionTextAndArrowPlacement; // $DIMFIT
            target.Header.DimensionCursorControlsTextPosition = source.Variables.DimensionCursorControlsTextPosition; // $DIMUPT
            target.Header.DimensionUnitFormat = (DxfUnitFormat)source.Variables.DimensionUnitFormat; // $DIMUNIT
            target.Header.DimensionUnitToleranceDecimalPlaces = source.Variables.DimensionUnitToleranceDecimalPlaces; // $DIMDEC
            target.Header.DimensionToleranceDecimalPlaces = source.Variables.DimensionToleranceDecimalPlaces; // $DIMTDEC
            target.Header.AlternateDimensioningUnits = (DxfUnitFormat)source.Variables.AlternateDimensioningUnits; // $DIMALTU
            target.Header.AlternateDimensioningToleranceDecimalPlaces = source.Variables.AlternateDimensioningToleranceDecimalPlaces; // $DIMALTTD
            // $DIMTXSTY will be handled elsewhere
            target.Header.DimensioningAngleFormat = (DxfAngleFormat)source.Variables.DimensioningAngleFormat; // $DIMAUNIT
            target.Header.UnitFormat = (DxfUnitFormat)source.Variables.UnitFormat; // $LUNITS
            target.Header.UnitPrecision = source.Variables.UnitPrecision; // $LUPREC
            target.Header.SketchRecordIncrement = source.Variables.SketchRecordIncrement; // $SKETCHINC
            target.Header.FilletRadius = source.Variables.FilletRadius; // $FILLETRAD
            target.Header.AngleUnitFormat = (DxfAngleFormat)source.Variables.AngleUnitFormat; // $AUNITS
            target.Header.AngleUnitPrecision = source.Variables.AngleUnitPrecision; // $AUPREC
            target.Header.Elevation = source.Variables.Elevation; // $ELEVATION
            target.Header.PaperspaceElevation = source.Variables.PaperSpaceElevation; // $PELEVATION
            target.Header.Thickness = source.Variables.Thickness; // $THICKNESS
            target.Header.UseLimitsChecking = source.Variables.UseLimitsChecking; // $LIMCHECK
            target.Header.BlipMode = source.Variables.BlipMode; // $BLIPMODE
            target.Header.FirstChamferDistance = source.Variables.FirstChamferDistance; // $CHAMFERA
            target.Header.SecondChamferDistance = source.Variables.SecondChamferDistance; // $CHAMFERB
            target.Header.ChamferLength = source.Variables.ChamferLength; // $CHAMFERC
            target.Header.ChamferAngle = source.Variables.ChamferAngle; // $CHAMFERD
            target.Header.PolylineSketchMode = (DxfPolySketchMode)source.Variables.PolylineSketchMode; // $SKPOLY
            target.Header.CreationDate = source.Variables.CreationDate; // $TDCREATE
            target.Header.UpdateDate = source.Variables.UpdateDate; // $TDUPDATE
            target.Header.TimeInDrawing = source.Variables.TimeInDrawing; // $TDINDWG
            target.Header.UserElapsedTimer = source.Variables.UserElapsedTimer; // $TDUSRTIMER
            target.Header.UserTimerOn = source.Variables.UserTimerOn; // $USRTIMER
            target.Header.AngleZeroDirection = source.Variables.AngleZeroDirection; // $ANGBASE
            target.Header.AngleDirection = (DxfAngleDirection)source.Variables.AngleDirection; // $ANGDIR
            target.Header.PointDisplayMode = source.Variables.PointDisplayMode; // $PDMODE
            target.Header.PointDisplaySize = source.Variables.PointDisplaySize; // $PDSIZE
            target.Header.DefaultPolylineWidth = source.Variables.DefaultPolylineWidth; // $PLINEWID
            target.Header.CoordinateDisplay = (DxfCoordinateDisplay)source.Variables.CoordinateDisplay; // $COORDS
            target.Header.DisplaySplinePolygonControl = source.Variables.DisplaySplinePolygonControl; // $SPLFRAME
            target.Header.PEditSplineCurveType = (DxfPolylineCurvedAndSmoothSurfaceType)source.Variables.PEditSplineCurveType; // $SPLINETYPE
            target.Header.LineSegmentsPerSplinePatch = source.Variables.LineSegmentsPerSplinePatch; // $SPLINESEGS
            target.Header.ShowAttributeEntryDialogs = source.Variables.ShowAttributeEntryDialogs; // $ATTDIA
            target.Header.PromptForAttributeOnInsert = source.Variables.PromptForAttributeOnInsert; // $ATTREQ
            target.Header.NextAvailableHandle = new DxfHandle(source.Variables.NextAvailableHandle.HandleOrOffset); // $HANDSEED
            target.Header.MeshTabulationsInFirstDirection = source.Variables.MeshTabulationsInFirstDirection; // $SURFTAB1
            target.Header.MeshTabulationsInSecondDirection = source.Variables.MeshTabulationsInSecondDirection; // $SURFTAB2
            target.Header.PEditSmoothSurfaceType = (DxfPolylineCurvedAndSmoothSurfaceType)source.Variables.PEditSmoothSurfaceType; // $SURFTYPE
            target.Header.PEditSmoothMDensity = source.Variables.PEditSmoothMDensity; // $SURFU
            target.Header.PEditSmoothNDensity = source.Variables.PEditSmoothNDensity; // $SURFV
            // $UCSNAME will be handled elsewhere
            target.Header.UCSOrigin = source.Variables.UCSOrigin.ToDxfPoint(); // $UCSORG
            target.Header.UCSXAxis = source.Variables.UCSXAxis.ToDxfVector(); // $UCSXDIR
            target.Header.UCSYAxis = source.Variables.UCSYAxis.ToDxfVector(); // $UCSYDIR
            // $PUCSNAME will be handled elsewhere
            target.Header.PaperspaceUCSOrigin = source.Variables.PaperSpaceUCSOrigin.ToDxfPoint(); // $PUCSORG
            target.Header.PaperspaceXAxis = source.Variables.PaperSpaceUCSXAxis.ToDxfVector(); // $PUCSXDIR
            target.Header.PaperspaceYAxis = source.Variables.PaperSpaceUCSYAxis.ToDxfVector(); // $PUCSYDIR
            target.Header.UserInt1 = source.Variables.UserInt1; // $USERI1
            target.Header.UserInt2 = source.Variables.UserInt2; // $USERI2
            target.Header.UserInt3 = source.Variables.UserInt3; // $USERI3
            target.Header.UserInt4 = source.Variables.UserInt4; // $USERI4
            target.Header.UserInt5 = source.Variables.UserInt5; // $USERI5
            target.Header.UserReal1 = source.Variables.UserReal1; // $USERR1
            target.Header.UserReal2 = source.Variables.UserReal2; // $USERR2
            target.Header.UserReal3 = source.Variables.UserReal3; // $USERR3
            target.Header.UserReal4 = source.Variables.UserReal4; // $USERR4
            target.Header.UserReal5 = source.Variables.UserReal5; // $USERR5
            target.Header.SetUCSToWCSInDViewOrVPoint = source.Variables.SetUCSToWCSInDViewOrVPoint; // $WORLDVIEW
            target.Header.EdgeShading = (DxfShadeEdgeMode)source.Variables.EdgeShading; // $SHADEDGE
            target.Header.PercentAmbientToDiffuse = source.Variables.PercentAmbientToDiffuse; // $SHADEDIF
            target.Header.PreviousReleaseTileCompatibility = source.Variables.PreviousReleaseTileCompatability; // $TILEMODE
            target.Header.MaximumActiveViewports = source.Variables.MaximumActiveViewports; // $MAXACTVP
            target.Header.PaperspaceInsertionBase = source.Variables.PaperSpaceInsertionBase.ToDxfPoint(); // $PINSBASE
            target.Header.LimitCheckingInPaperspace = source.Variables.LimitCheckingInPaperSpace; // $PLIMCHECK
            target.Header.PaperspaceMinimumDrawingExtents = source.Variables.PaperSpaceMinimumDrawingExtents.ToDxfPoint(); // $PEXTMIN
            target.Header.PaperspaceMaximumDrawingExtents = source.Variables.PaperSpaceMaximumDrawingExtents.ToDxfPoint(); // $PEXTMAX
            target.Header.PaperspaceMinimumDrawingLimits = source.Variables.PaperSpaceMinimumDrawingLimits.ToDxfPoint(); // $PLIMMIN
            target.Header.PaperspaceMaximumDrawingLimits = source.Variables.PaperSpaceMaximumDrawingLimits.ToDxfPoint(); // $PLIMMAX
            target.Header.DisplayFractionsInInput = source.Variables.DisplayFractionsInInput; // $UNITMODE
            target.Header.RetainXRefDependentVisibilitySettings = source.Variables.RetainXRefDependentVisibilitySettings; // $VISRETAIN
            target.Header.IsPolylineContinuousAroundVertices = source.Variables.IsPolylineContinuousAroundVerticies; // $PLINEGEN
            target.Header.ScaleLineTypesInPaperspace = source.Variables.ScaleLineTypesInPaperSpace; // $PSLTSCALE
            target.Header.SpacialIndexMaxDepth = source.Variables.SpacialIndexMaxDepth; // $TREEDEPTH
            target.Header.PickStyle = (DxfPickStyle)source.Variables.PickStyle; // $PICKSTYLE
            // $CMLSTYLE will be handled elsewhere
            target.Header.CurrentMultilineJustification = (DxfJustification)source.Variables.CurrentMultilineJustification; // $CMLJUST
            target.Header.CurrentMultilineScale = source.Variables.CurrentMultilineScale; // $CMLSCALE
            target.Header.SaveProxyGraphics = source.Variables.SaveProxyGraphics; // $PROXYGRAPHICS
            // $CMATERIAL will be handled elsewhere
        }

        private static DxfEntity? ConvertEntity(DwgEntity entity)
        {
            return entity switch
            {
                DwgArc arc => arc.ToDxfArc(),
                DwgCircle circle => circle.ToDxfCircle(),
                DwgDimensionAligned aligned => aligned.ToDxfAlignedDimension(),
                DwgDimensionOrdinate ordinate => ordinate.ToDxfRotatedDimension(),
                DwgEllipse ellipse => ellipse.ToDxfEllipse(),
                DwgInsert insert => insert.ToDxfInsert(),
                DwgLine line => line.ToDxfLine(),
                DwgLocation location => location.ToDxfModelPoint(),
                DwgLwPolyline lwpolyline => lwpolyline.ToDxfLwPolyline(),
                DwgPolyline2D polyline2d => polyline2d.ToDxfPolyline(),
                DwgPolyline3D polyline3D => polyline3D.ToDxfPolyline(),
                DwgSolid solid => solid.ToDxfSolid(),
                DwgSpline spline => spline.ToDxfSpline(),
                DwgText text => text.ToDxfText(),
                _ => null,
            };
        }
    }
}
