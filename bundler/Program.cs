using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Oxide.Plugins;

namespace Combine_Partial_Classes
{
    /// <summary>
    /// This takes the individual files with their partial classes and creates a single file.
    /// </summary>
    class Program
    {
        private static Regex usingRegex = new Regex("^using (?<Using>.*);", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        private static Regex attributeRegex = new Regex("public class (?<Attribute>.*) *:.*Attribute", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        //regex to read the seed file
        private static Regex primaryRegex = 
            new Regex(@"
namespace Oxide.Plugins
\{(?<Comments>(?:\r\n.*)*)
.*(?<Info>\[Info.*\])
.*(?<Description>\[Description.*\])
.*partial class (?<Class>[a-zA-Z_-]*) *: RustPlugin
.*\{(?<PreInit>(?:\r\n.*)*)
.*Init\(\)
.*\{(?<Init>(?:\r\n(?:(?:\{.*\})|(?:[^\}]))*))\}(?<PostInit>(?:\r\n.*)*)
.*\}
.*\}", 
                RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);


        /// <summary>
        /// Combine all the Enums used for localizations into a single list
        /// </summary>
        /// <param name="enums"></param>
        /// <returns></returns>
        static List<Enum> GetLanguageEnums(params Type[] enums) => enums.SelectMany(a => Enum.GetValues(a).OfType<Enum>()).ToList();

        private static string GenerateCommandTypes<T>(List<Enum> languageEnums)
        where T : Attribute
        {
            var sb = new StringBuilder();
            sb.AppendLine(" = new Dictionary<Enum, bool> {");
            foreach (var commandData in languageEnums.Select(GetAttribute<T>).Where(a=>a.Value != null))
            {
                var type = commandData.Key.GetType();
                sb.AppendLine($"                {{{type.DeclaringType.FullName.Replace("+", ".")}.{commandData.Key.GetType().Name}.{commandData.Key},true}},");
            }
            sb.AppendLine("            };");
            return sb.ToString();
        }

        private static string GenerateMessageTypes(List<Enum> languageEnums)
        {
            var sb = new StringBuilder();
            sb.AppendLine(" = new Dictionary<Enum, MessageType> {");
            foreach (var messageType in languageEnums.Select(GetAttribute<SyncPipesDevelopment.MessageTypeAttribute>)
                .Where(a => a.Value != null))
            {
                var type = messageType.Key.GetType();
                sb.AppendLine($"                {{{type.DeclaringType.FullName.Replace("+", ".")}.{messageType.Key.GetType().Name}.{messageType.Key}, MessageType.{messageType.Value.Type}}},");
            }
            sb.AppendLine("            };");
            return sb.ToString();
        }

        private static string GenerateLanguage(List<Enum> languageEnums, Type type)
        {
            var sb = new StringBuilder();
            sb.AppendLine("            {");
            foreach (var english in languageEnums.Select(a=> GetAttribute<SyncPipesDevelopment.LanguageAttribute>(a, type))
                .Where(a => a.Value != null))
            {
                sb.AppendLine($"                {{\"{english.Key.GetType().Name}.{english.Key}\", \"{english.Value.Text.Replace("\r", "").Replace("\n", "\\n")}\"}},");
            }

            sb.AppendLine("            };");
            return sb.ToString();
        }

        static void Main(string[] args)
        {
            var types = typeof(SyncPipesDevelopment).Assembly.GetTypes();
            var attributes = new List<string>();

            var langEnum = types.Where(a => a.GetCustomAttribute<SyncPipesDevelopment.EnumWithLanguageAttribute>() != null).ToArray();


            var languageEnums = GetLanguageEnums(langEnum);

            #region check args and get settings
            if (args.Length < 1)
            {
                Console.WriteLine("Please set a valid seed file and the output directory.");
                Console.ReadKey();
                return;
            }

            var seedFile = new FileInfo(@"..\..\..\src\Initialization.cs");
            var seedDirectory = seedFile.Directory;
            var outputDirectory = Environment.ExpandEnvironmentVariables(args[0]);
            var outputFile =  Path.Combine(outputDirectory, "SyncPipes.cs");
            var deployFile = Path.Combine(@"..\..\..\", "SyncPipes.cs");

            Console.WriteLine("Seed file: {0}", seedFile.FullName);
            Console.WriteLine("Output file: {0}", outputFile);

            if (!seedFile.Exists)
            {
                Console.WriteLine("Can't find seed file - {0}", seedFile.FullName);
                Console.ReadKey();
                return;
            }

            if (!Directory.Exists(outputDirectory))
            {
                Console.WriteLine("Can't find output directory - {0}", outputDirectory);
                Console.ReadKey();
                return;
            }
            #endregion

            #region variables
            var usings = new List<string>
            {
                "System",
                "System.Collections.Generic"
            };
            var info = "";
            var description = "";
            var primaryClass = "";
            var classComments = "";
            var partialClassContents = new Dictionary<string, string>();
            #endregion
            
            #region Load Seed file
            using (var sr = new StreamReader(seedFile.FullName))
            {
                var data = sr.ReadToEnd();

                usings.AddRange(usingRegex.Matches(data).OfType<Match>()
                    .Where(a => a.Success && (a.Groups["Using"]?.Success ?? false)).Select(a => a.Groups["Using"].Value)
                    .Except(usings));

                var classRead = primaryRegex.Match(data);
                if (classRead.Success)
                {
                    info = classRead.Groups["Info"]?.Value;
                    description = classRead.Groups["Description"]?.Value;
                    primaryClass = "SyncPipes";// classRead.Groups["Class"]?.Value;
                    classComments = classRead.Groups["Comments"]?.Value;
                    var sb = new StringBuilder();
                    sb.AppendLine(classRead.Groups["PreInit"]?.Value);
                    sb.AppendLine("        void Init()");
                    sb.AppendLine("        {");
                    sb.AppendLine(classRead.Groups["Init"]?.Value);
                    sb.AppendLine();
                    sb.AppendLine("            #region static data declarations");
                    sb.AppendLine($"            _chatCommands{GenerateCommandTypes<SyncPipesDevelopment.ChatCommandAttribute>(languageEnums)}");
                    sb.AppendLine($"            _bindingCommands{GenerateCommandTypes<SyncPipesDevelopment.BindingCommandAttribute>(languageEnums)}");
                    sb.AppendLine($"            _messageTypes{GenerateMessageTypes(languageEnums)}");
                    sb.AppendLine();
                    sb.AppendLine($"            _storageDetails = new Dictionary<{nameof(SyncPipesDevelopment.Storage)}, {nameof(SyncPipesDevelopment.StorageData)}>");
                    sb.AppendLine("            {");
                    foreach (var storageData in Enum.GetValues(typeof(SyncPipesDevelopment.Storage)).OfType<SyncPipesDevelopment.Storage>()
                        .ToDictionary(a => a, a => GetAttribute<SyncPipesDevelopment.StorageAttribute>(a).Value))
                    {
                        sb.AppendLine($"                {{{nameof(SyncPipesDevelopment.Storage)}.{storageData.Key}, new StorageData(\"{storageData.Value.ShortName}\", \"{storageData.Value.Url}\", new Vector3({storageData.Value.Offset.x}f, {storageData.Value.Offset.y}f, {storageData.Value.Offset.z}f), {(storageData.Value.PartialUrl ? "true" : "false")})}},");
                    }
                    sb.AppendLine("            };");
                    sb.AppendLine("            #endregion");
                    sb.AppendLine("        }");

                    sb.AppendLine(classRead.Groups["PostInit"]?.Value);
                    partialClassContents.Add(seedFile.Name.Replace(seedFile.Extension, ""), sb.ToString());
                }
            }
            #endregion

            #region pull in any other parts of the partial class from the seed file
            var secondaryRegex =
                new Regex(
                    $@".*partial *class *{primaryClass}.*\r\n.*\{{ *(?<Content>(?:\r\n.*)*)\r\n.*\}} *\r\n.*\}}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
            foreach (var file in seedDirectory.GetFiles("*.cs").Where(a=>a.Name != seedFile.Name))
            {
                using (var sr = new StreamReader(file.FullName))
                {
                    if (file.Name.Equals("Attributes.cs", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var line = "";
                        while ((line = sr.ReadLine()) != null)
                        {
                            var attribute = attributeRegex.Match(line).Groups["Attribute"];
                            if(!attribute.Success) continue;
                            attributes.Add(attribute.Value.Trim());
                            attributes.Add(attribute.Value.Replace("Attribute", "").Trim());
                        }
                    }
                    else
                    {
                        var data = sr.ReadToEnd();
                        var classRead = secondaryRegex.Match(data);
                        if (classRead.Success && (classRead.Groups["Content"]?.Success ?? false))
                        {
                            var content = classRead.Groups["Content"]?.Value;
                            partialClassContents.Add(file.Name.Replace(file.Extension, ""),
                                content);

                            usings.AddRange(usingRegex.Matches(data).OfType<Match>()
                                .Where(a => a.Success && (a.Groups["Using"]?.Success ?? false))
                                .Select(a => a.Groups["Using"].Value)
                                .Except(usings));
                        }
                    }
                }
            }
            #endregion

            var attributeReplaceRegex = new Regex($@"\r\n.*\[({string.Join("|", attributes)})(\(((@?""([^""]|\r|\n)*"")|.*)\))?\]", RegexOptions.Multiline | RegexOptions.Compiled);
            #region Output everything to a single file
            using (var sw = new StreamWriter(deployFile, false))
            {
                foreach (var usng in usings.OrderBy(a=>a.Length))
                    sw.WriteLine($"using {usng};");
                sw.WriteLine("namespace Oxide.Plugins");
                sw.WriteLine($"{{{classComments}");
                sw.WriteLine($"    {info}");
                sw.WriteLine($"    {description}");
                sw.WriteLine($"    class {primaryClass} : RustPlugin");
                sw.WriteLine("    {");
                foreach (var content in partialClassContents)
                {
                    sw.WriteLine($"        #region {content.Key}");
                    var value = attributeReplaceRegex.Replace(content.Value, "");
                    sw.WriteLine(value);
                    if (content.Key.Equals("Messages"))
                    {
                        sw.WriteLine();
                        sw.WriteLine("        protected override void LoadDefaultMessages()");
                        sw.WriteLine("        {");
                        var languages = types.Where(a => a.BaseType == typeof(SyncPipesDevelopment.LanguageAttribute)).ToArray();
                        foreach (var language in languages)
                        {
                            var lang = language.GetField("Language").GetRawConstantValue().ToString();
                            sw.WriteLine($"            var {lang} = new Dictionary<string, string>");
                            sw.WriteLine(GenerateLanguage(languageEnums, language));
                            sw.WriteLine($"            lang.RegisterMessages({lang}, Instance, \"{lang}\");");
                        }
                        sw.WriteLine("        }");
                        sw.WriteLine();
                    }

                    sw.WriteLine("        #endregion");
                }
                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
            #endregion

            File.Copy(deployFile, outputFile, true);
        }


        /// <summary>
        /// Get a custom attributes from an enum value
        /// </summary>
        /// <typeparam name="TAttribute">Custom Attribute to fetch</typeparam>
        /// <param name="value">Enum value to get custom attribute from</param>
        /// <returns>The custom attribute if it exists on the enum value or null if it doesn't</returns>
        private static KeyValuePair<Enum, TAttribute> GetAttribute<TAttribute>(Enum value)
            where TAttribute : Attribute
        {
            return GetAttribute<TAttribute>(value, typeof(TAttribute));
        }

        private static KeyValuePair<Enum, TAttribute> GetAttribute<TAttribute>(Enum value, Type attributeType) where TAttribute : Attribute
        {
            var enumType = value.GetType();
            var name = Enum.GetName(enumType, value);
            return new KeyValuePair<Enum, TAttribute>(value, enumType.GetField(name).GetCustomAttribute(attributeType, false) as TAttribute);
        }
    }
}
