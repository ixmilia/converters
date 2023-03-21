namespace IxMilia.Converters
{
    public struct DimensionSettings
    {
        public double TextHeight { get; }
        public double ExtensionLineOffset { get; }
        public double ExtensionLineExtension { get; }
        public double DimensionLineGap { get; }
        public double ArrowSize { get; }

        public DimensionSettings(
            double textHeight,
            double extensionLineOffset,
            double extensionLineExtension,
            double dimensionLineGap,
            double arrowSize)
        {
            TextHeight = textHeight;
            ExtensionLineOffset = extensionLineOffset;
            ExtensionLineExtension = extensionLineExtension;
            DimensionLineGap = dimensionLineGap;
            ArrowSize = arrowSize;
        }
    }
}
