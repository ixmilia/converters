namespace IxMilia.Converters
{
    public interface IConverter<TSource, TDest>
    {
        TDest Convert(TSource source);
    }
}
