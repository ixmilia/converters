using IxMilia.Pdf;

namespace IxMilia.Converters
{
    public static class VectorExtensions
    {
        public static PdfPoint ToPdfPoint(this Vector vector, PdfMeasurementType measurementType)
        {
            return new PdfPoint(new PdfMeasurement(vector.X, measurementType), new PdfMeasurement(vector.Y, measurementType));
        }
    }
}
