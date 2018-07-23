namespace Catalyst.Fabric.Authorization.Client.FunctionalTests
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    public static class FunctionalTestExtensions
    {
        public static string ToJson(this object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        public static T FromJson<T>(this string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }

        public static string ToBase64Encoded(this string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }
    }
}
