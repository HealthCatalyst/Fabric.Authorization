using System.Linq;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Converters
{
    /// <summary>
    /// NOT USED
    /// </summary>
    public class UnderscoredFieldNameConverter : IFieldNameConverter
    {
        private readonly DefaultFieldNameConverter _defaultFieldNameConverter;

        public UnderscoredFieldNameConverter()
        {
            _defaultFieldNameConverter = new DefaultFieldNameConverter();
        }

        public string Convert(string fieldName)
        {
            var result = string.Concat(fieldName.Select((x, i) =>
                i > 0 && fieldName[i - 1] == '_'
                    ? char.ToUpper(fieldName[i])
                    : fieldName[i]));

            return _defaultFieldNameConverter.Convert(result.Replace("_", string.Empty));
        }
    }
}
