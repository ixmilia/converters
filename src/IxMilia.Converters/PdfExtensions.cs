using IxMilia.Pdf;

namespace IxMilia.Converters
{
    public static class PdfExtensions
    {
        public static PdfMatrix ToPdfMatrix(this Matrix4 matrix)
        {
            return new PdfMatrix(matrix.M11, matrix.M12, matrix.M21, matrix.M22, matrix.M14, matrix.M24);
        }
    }
}
