using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Fabric.Authorization.ApdexCalculator
{
    class Program
    {
        private static double ApdexThreshold = .8;
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appSettings.json")
                .SetBasePath(Directory.GetCurrentDirectory())
                .Build();

            var appConfig = new AppSettings();
            ConfigurationBinder.Bind(config, appConfig);
            var samples = ReadResultsFile(args[0]);
            var results = new Dictionary<string, double>();
            foreach (var measure in appConfig.Measures)
            {
                var apdex = CalculateApdex(measure, samples);
                results.Add(measure.Name, apdex);
            }
            foreach (var result in results)
            {
                if (result.Value < ApdexThreshold)
                {
                    Environment.Exit(1);
                }
            }
        }

        private static double CalculateApdex(Measure measure, IEnumerable<Sample> samples)
        {
            
            var satisfiedCount = (double)samples.Count(s => s.Label == measure.Name &&
                                                    s.Elapsed <= measure.TolerationThreshold);
            var toleratedCount = (double)samples.Count(s => s.Label == measure.Name &&
                                                    s.Elapsed > measure.TolerationThreshold &&
                                                    s.Elapsed <= measure.FrustrationThreshold);
            var totalSamples = (double)samples.Count(s => s.Label == measure.Name);
            var apdex = (satisfiedCount + toleratedCount/2) / totalSamples;
            Console.ForegroundColor = apdex < ApdexThreshold ? ConsoleColor.Red : ConsoleColor.Gray;
            Console.WriteLine($"Name: {measure.Name}, Total Samples: {totalSamples}, SatisfiedCount: {satisfiedCount}, ToleratedCount: {toleratedCount}, Apdex: {apdex.ToString("F3", CultureInfo.InvariantCulture)}");
            return apdex;
        }

        private static IEnumerable<Sample> ReadResultsFile(string filePath)
        {
            var samples = new List<Sample>();
            var allLines = File.ReadAllLines(filePath);
            foreach(var line in allLines) { 
                var segments = line.Split(',');
                if (segments[0] == "timeStamp")
                {
                    continue;
                }
                samples.Add(new Sample
                {
                    Elapsed = Int32.Parse(segments[1]),
                    Label = segments[2],
                    Success = bool.Parse(segments[7])
                });
            }
            return samples;
        }
    }
}