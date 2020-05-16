namespace IxMilia.Converters
{
    public struct ConverterSvgRect
    {
        public double ElementWidth { get; }
        public double ElementHeight { get; }

        public ConverterSvgRect(double elementWidth, double elementHeight)
        {
            ElementWidth = elementWidth;
            ElementHeight = elementHeight;
        }
    }
}
