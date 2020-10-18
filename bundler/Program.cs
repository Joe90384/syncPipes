using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Combine_Partial_Classes
{
    /// <summary>
    /// This takes the individual files with their partial classes and creates a single file.
    /// </summary>
    class Program
    {
        private static Regex usingRegex = new Regex("^using (?<Using>.*);", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        //regex to read the seed file
        private static Regex primaryRegex = 
            new Regex(@"
namespace Oxide.Plugins
\{(?<Comments>(?:\r\n.*)*)
.*(?<Info>\[Info.*\])
.*partial class (?<Class>[a-zA-Z_-]*) *: RustPlugin
.*\{(?<Content>(?:\r\n.*)*)
.*\}
.*\}", 
                RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.ExplicitCapture);


        static void Main(string[] args)
        {
            #region check args and get settings
            if (args.Length < 1)
            {
                Console.WriteLine("Please set a valid seed file and the output directory.");
                Console.ReadKey();
                return;
            }

            var seedFile = new FileInfo(args[0]);
            var seedDirectory = seedFile.Directory;
            var outputDirectory = Environment.ExpandEnvironmentVariables(args[1]);
            var outputFile =  Path.Combine(outputDirectory, seedFile.Name);
            var deployFile = Path.Combine(@"..\..\..\deploy", seedFile.Name);

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
            var usings = new List<string>();
            var info = "";
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
                    primaryClass = classRead.Groups["Class"]?.Value;
                    classComments = classRead.Groups["Comments"]?.Value;
                    partialClassContents.Add(seedFile.Name.Replace(seedFile.Extension, ""), classRead.Groups["Content"]?.Value);
                }
            }
            #endregion

            #region pull in any other parts of the partial class from the seed file
            var secondaryRegex =
                new Regex(
                    $@".*partial *class *{primaryClass}.*\r\n.*\{{ *(?<Content>(?:\r\n.*)*)\r\n.*\}} *\r\n.*\}}", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
            foreach (var file in seedDirectory.GetFiles("*.cs").Where(a=>a.Name != seedFile.Name && a.FullName != outputFile))
            {
                using (var sr = new StreamReader(file.FullName))
                {
                    var data = sr.ReadToEnd();
                    var classRead = secondaryRegex.Match(data);
                    if (classRead.Success && (classRead.Groups["Content"]?.Success ?? false))
                    {
                        partialClassContents.Add(file.Name.Replace(file.Extension, ""), classRead.Groups["Content"]?.Value);

                        usings.AddRange(usingRegex.Matches(data).OfType<Match>()
                            .Where(a => a.Success && (a.Groups["Using"]?.Success ?? false)).Select(a => a.Groups["Using"].Value)
                            .Except(usings));
                    }
                }
            }
            #endregion

            #region Output everything to a single file
            using (var sw = new StreamWriter(deployFile, false))
            {
                foreach (var usng in usings.OrderBy(a=>a.Length))
                    sw.WriteLine($"using {usng};");
                sw.WriteLine("namespace Oxide.Plugins");
                sw.WriteLine($"{{{classComments}");
                sw.WriteLine($"    {info}");
                sw.WriteLine($"    class {primaryClass} : RustPlugin");
                sw.WriteLine("    {");
                foreach (var content in partialClassContents)
                {
                    sw.WriteLine($"        #region {content.Key}");
                    sw.WriteLine(content.Value);
                    sw.WriteLine("        #endregion");
                }
                sw.WriteLine("    }");
                sw.WriteLine("}");
            }
            #endregion

            File.Copy(deployFile, outputFile, true);
        }
    }
}
