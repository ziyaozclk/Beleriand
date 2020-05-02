using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace Beleriand.Console
{
    class ApplicationConfiguration
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public ApplicationConfiguration(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost");

            MultiLevelCache multiLevelCache =
                new MultiLevelCache("ApplicationConfiguration", connectionMultiplexer);

            /*
            var dict = new Dictionary<string, ApplicationConfiguration>();
            dict.Add("ApplicationConfiguration:1", new ApplicationConfiguration("Ziya","1"));
            dict.Add("ApplicationConfiguration:2", new ApplicationConfiguration("Ahmet","9"));

            multiLevelCache.Set(dict);
            */
            
            multiLevelCache.Clear();
            
            System.Console.WriteLine("Finito :D");
        }
    }
}