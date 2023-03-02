namespace IxMilia.Converters
{
    public class ConverterSvgRect
    {
        public double Width { get; }
        public double Height { get; }

        public double LeftMargin { get; }
        public double RightMargin { get; }
        public double TopMargin { get; }
        public double BottomMargin { get; }

        public ConverterSvgRect()
        {
            Width = 0;
            Height = 0;
            LeftMargin = 0;
            RightMargin = 0;
            TopMargin = 0;
            BottomMargin = 0;
        }

        public ConverterSvgRect(double width, double height, double leftMargin = 0, double rightMargin = 0, double topMargin = 0, double bottomMargin = 0)
        {
            Width = width;
            Height = height;
            LeftMargin = leftMargin;
            RightMargin = rightMargin;
            TopMargin = topMargin;
            BottomMargin = bottomMargin;
        }
    }
}
