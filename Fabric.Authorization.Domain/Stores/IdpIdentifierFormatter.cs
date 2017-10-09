namespace Fabric.Authorization.Domain.Stores
{
    public class IdpIdentifierFormatter
    {
        private const string BackslashReplacementChars = "::";

        public string Format(string id)
        {
            return ReplaceBackslash(id).ToLower();
        }

        private static string ReplaceBackslash(string id)
        {
            return id.Replace(@"\", BackslashReplacementChars);
        }
    }
}