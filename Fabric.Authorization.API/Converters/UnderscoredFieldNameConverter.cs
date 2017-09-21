using System.Linq;
using Nancy.ModelBinding;

namespace Fabric.Authorization.API.Converters
{
    /// <summary>
    /// Custom converter to handle underscores and property names to support Nancy model binding (e.g., converts client_id to clientId).
    /// Invokes the built-in Nancy DefaultFieldNameConverter, which converts Camel case to Pascal case.
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
            if (!fieldName.Contains("_"))
            {
                return _defaultFieldNameConverter.Convert(fieldName);
            }

            var result = string.Concat(fieldName.Select((x, i) =>
                i > 0 && fieldName[i - 1] == '_'
                    ? char.ToUpper(fieldName[i])
                    : fieldName[i]));

            return _defaultFieldNameConverter.Convert(result.Replace("_", string.Empty));
        }
    }
}
