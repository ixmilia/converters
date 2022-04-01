using System.Threading.Tasks;

namespace IxMilia.Converters
{
    public interface IConverter<TSource, TDest, TConvertOptions>
    {
        Task<TDest> Convert(TSource source, TConvertOptions options);
    }
}
