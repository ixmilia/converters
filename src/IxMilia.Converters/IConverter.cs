namespace IxMilia.Converters
{
    public interface IConverter<TSource, TDest, TConvertOptions>
    {
        TDest Convert(TSource source, TConvertOptions options);
    }
}
