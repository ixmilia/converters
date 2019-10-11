namespace IxMilia.Converters.Test
{
    public abstract class TestBase<TSource, TDest>
    {
        public abstract IConverter<TSource, TDest> GetConverter();

        public TDest Convert(TSource source)
        {
            return GetConverter().Convert(source);
        }

        public static string NormalizeCrLf(string value)
        {
            return value.Replace("\r", "").Replace("\n", "\r\n");
        }
    }
}
