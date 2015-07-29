using System;
using System.Collections.Generic;
using clipr;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Newtonsoft.Json;
using PathLib;

[assembly: XmlConfigurator]

namespace LogRotate
{
    internal class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof (Program));

        [ApplicationInfo(
            Name = "LogRotate",
            Description = "Automatically archives old log files.")]
        public class CommandLineOptions
        {
            [PositionalArgument(0)]
            public IPath ConfigFile { get; set; }

            [NamedArgument('v', "verbose", Action = ParseAction.StoreTrue,
                Description = "Verbose details will be written to the console.")]
            public bool Verbose { get; set; }

            [NamedArgument('d', "dryrun", Action = ParseAction.StoreTrue,
                Description = "Enable dry run mode. Actions will be printed, but no files will be modified.")]
            public bool DryRun { get; set; }

            [NamedArgument('f', "force", Action = ParseAction.StoreTrue,
                Description = "Force all logs to rotate, even if they are not otherwise eligible.")]
            public bool Force { get; set; }

            [PostParse]
            public void EnsureConfigFileExists()
            {
                if (ConfigFile == null)
                {
                    Console.Error.WriteLine(
                        "Configuration file is null.");
                    Environment.Exit(2);
                }
                else if (!ConfigFile.IsFile())
                {
                    Console.Error.WriteLine(
                        "Configuration file '{0}' does not exist.", ConfigFile);
                    Environment.Exit(2);
                }
            }

            [PostParse]
            public void SetLog4NetVerbose()
            {
                if (!Verbose)
                {
                    return;
                }

                var layout = new PatternLayout
                {
                    ConversionPattern = "%date: %message%newline"
                };
                layout.ActivateOptions();
                var consoleAppender = new ConsoleAppender
                {
                    Layout = layout
                };

                //Configure the root logger.
                var h = (Hierarchy)LogManager.GetRepository();
                var rootLogger = h.Root;
                rootLogger.AddAppender(consoleAppender);
                rootLogger.Level = Level.Debug;
                h.Configured = true;
            }
        }

        private static void Main(string[] args)
        {
            var options = CliParser.StrictParse<CommandLineOptions>(args);
            try
            {
                var text = options.ConfigFile.ReadAsText();
                var dictionary = JsonConvert.DeserializeObject<Dictionary<WindowsPath, ConfigFileOptions>>(
                    text);
                var logRotator = new LogRotator(options.DryRun);
                foreach (var keyValuePair in dictionary)
                {
                    var sourceFile = keyValuePair.Key;
                    var config = keyValuePair.Value;
                    if (options.DryRun)
                    {
                        config.Cleanup = CleanupBehavior.None;
                    }
                    
                    var strategy = new DateRotationStrategy
                    {
                        Compression = config.Compress
                    };

                    logRotator.Rotate(sourceFile, strategy, config, options.Force);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Error(null, ex);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Unhandled exception encountered.", ex);
            }
        }
    }
}