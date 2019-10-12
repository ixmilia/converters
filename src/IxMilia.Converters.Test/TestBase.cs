namespace IxMilia.Converters.Test
{
    public abstract class TestBase<TSource, TDest>
    {
        public static string NormalizeCrLf(string value)
        {
            return value.Replace("\r", "").Replace("\n", "\r\n");
        }
    }
}
