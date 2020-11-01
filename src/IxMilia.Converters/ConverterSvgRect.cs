namespace IxMilia.Converters
{
    public class ConverterSvgRect
    {
        public double Width { get; }
        public double Height { get; }

        public ConverterSvgRect()
        {
            Width = 0;
            Height = 0;
        }

        public ConverterSvgRect(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}
