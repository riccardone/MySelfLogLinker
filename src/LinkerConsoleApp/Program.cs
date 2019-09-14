﻿using System;
using System.Collections.Generic;
using System.IO;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Linker;
using Microsoft.Extensions.Configuration;
using NLog;

namespace LinkerConsoleApp
{
    class Program
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            Log.Info("Building services...");

            var env = Environment.GetEnvironmentVariable("CORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
            var config = builder.Build();

            var origin = config.GetSection("origin").Get<Origin>();
            var destination = config.GetSection("destination").Get<Destination>();
            var filterService = new FilterService(new List<Filter>
            {
                new Filter { FilterOperation = FilterOperation.Exclude, FilterType = FilterType.Stream, Value = "diary-*" },
                new Filter { FilterOperation = FilterOperation.Include, FilterType = FilterType.Stream, Value = "*" }
            });

            var service = new LinkerService(new LinkerConnectionBuilder(new Uri(origin.ConnectionString),
                    ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials(origin.User, origin.Pass)),
                    origin.ConnectionName), new LinkerConnectionBuilder(new Uri(destination.ConnectionString),
                    ConnectionSettings.Create().SetDefaultUserCredentials(new UserCredentials(destination.User, destination.Pass)),
                    destination.ConnectionName), filterService, Settings.Default(), new NLogger());
            service.Start().Wait();

            Log.Info("Replica Service started");
            Log.Info("Press enter to exit the program");
            Console.ReadLine();
        }
    }
}
