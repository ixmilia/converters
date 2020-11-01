namespace IxMilia.Converters
{
    public struct ConverterSvgRect
    {
        public double Width { get; }
        public double Height { get; }

        public ConverterSvgRect(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}
