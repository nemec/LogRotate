using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using clipr;
using clipr.Utils;
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
            [TypeConverter(typeof(PurePathStringConverter))]
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
                else if (!File.Exists(ConfigFile.ToString()))
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
                    text, new NtPathJsonConverter());
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

        private class NtPathJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(WindowsPath);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.String)
                {
                    throw new InvalidDataException("Unexpected token parsing WindowsPath. Expected string, got " +
                                                   reader.TokenType);
                }

                return new WindowsPath((string)reader.Value);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var path = value as WindowsPath;
                if (path == null)
                {
                    throw new InvalidDataException("Value is not a WindowsPath.");
                }
                writer.WriteValue(path.ToString());
            }
        }

        private class PurePathStringConverter : StringTypeConverter<IPath>
        {
            public override IPath ConvertFrom(CultureInfo culture, string value)
            {
                return ConcretePath.FromString(value);
            }

            public override bool IsValid(string value)
            {
                try
                {
                    ConcretePath.FromString(value);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public class DictionaryWithSpecialEnumKeyConverter : JsonConverter
        {
            public override bool CanWrite
            {
                get { return false; }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                var valueType = objectType.GetGenericArguments()[1];
                var intermediateDictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
                var intermediateDictionary = (IDictionary)Activator.CreateInstance(intermediateDictionaryType);
                serializer.Populate(reader, intermediateDictionary);

                var finalDictionary = (IDictionary)Activator.CreateInstance(objectType);
                foreach (DictionaryEntry pair in intermediateDictionary)
                {
                    var converter = new PurePathStringConverter();
                    var key = converter.ConvertFromInvariantString((string)pair.Key);
                    finalDictionary.Add(key ?? "", pair.Value);
                }

                return finalDictionary;
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType.IsA(typeof(IDictionary<,>)) &&
                       objectType.GetGenericArguments()[0].IsA(typeof(WindowsPath));
            }

        }

    }

    public static class Extensions
    {
        public static bool IsA(this Type type, Type typeToBe)
        {
            if (!typeToBe.IsGenericTypeDefinition)
                return typeToBe.IsAssignableFrom(type);

            var toCheckTypes = new List<Type> { type };
            if (typeToBe.IsInterface)
                toCheckTypes.AddRange(type.GetInterfaces());

            var basedOn = type;
            while (basedOn.BaseType != null)
            {
                toCheckTypes.Add(basedOn.BaseType);
                basedOn = basedOn.BaseType;
            }

            return toCheckTypes.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeToBe);
        }
    }
}