using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Fabric.Authorization.MetadataFetcher
{
    public static class Program
    {
        private static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            var props = builder.Properties.ToDictionary(d => d.Key, d => d.Value.ToString());

            new LocalADGroupFetcher().FetchAllGroups(props);
            Console.ReadLine();
            new AzureADGroupFetcher().FetchAllGroups(props);
            Console.ReadLine();
        }
    }
}