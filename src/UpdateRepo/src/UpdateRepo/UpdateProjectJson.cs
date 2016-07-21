using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UpdateRepo
{
    public static class UpdateProjectJson
    {
        public static void Execute(string repoRoot, List<PackageInfo> packageInfos)
        {
            const string noUpdateFileName = ".noautoupdate";

            IEnumerable<string> projectJsonFiles = Enumerable.Union(
                Directory.GetFiles(repoRoot, "project.json", SearchOption.AllDirectories),
                Directory.GetFiles(Path.Combine(repoRoot, @"src\dotnet\commands\dotnet-new"), "project.json.template", SearchOption.AllDirectories))
                .Where(p => !File.Exists(Path.Combine(Path.GetDirectoryName(p), noUpdateFileName)) &&
                    !Path.GetDirectoryName(p).EndsWith("CSharp_Web", StringComparison.Ordinal));

            foreach (string projectJsonFile in projectJsonFiles)
            {
                var projectRoot = ReadProject(projectJsonFile);

                if (projectRoot == null)
                    throw new Exception($"A non valid JSON file was encountered '{projectJsonFile}'. Skipping file.");

                bool changedAnyPackage = FindAllDependencyProperties(projectRoot)
                    .Select(dependencyProperty => ReplaceDependencyVersion(dependencyProperty, packageInfos))
                    .ToArray()
                    .Any(shouldWrite => shouldWrite);

                if (changedAnyPackage)
                {
                    Console.WriteLine($"Writing changes to {projectJsonFile}");
                    WriteProject(projectRoot, projectJsonFile);
                }
            }
        }

        static IEnumerable<JProperty> FindAllDependencyProperties(JObject projectJsonRoot)
        {
            return projectJsonRoot
                .Descendants()
                .OfType<JProperty>()
                .Where(property => property.Name == "dependencies")
                .Select(property => property.Value)
                .SelectMany(o => o.Children<JProperty>());
        }

        static bool ReplaceDependencyVersion(JProperty dependencyProperty, List<PackageInfo> packageInfos)
        {
            string id = dependencyProperty.Name;
            foreach (var packageInfo in packageInfos)
            {
                if (id == packageInfo.Id)
                {
                    string oldVersion;
                    if (dependencyProperty.Value is JObject)
                    {
                        oldVersion = (string)dependencyProperty.Value["version"];
                    }
                    else
                    {
                        oldVersion = (string)dependencyProperty.Value;
                    }

                    string newVersion = packageInfo.Version.ToNormalizedString();
                    if (oldVersion != newVersion)
                    {
                        if (dependencyProperty.Value is JObject)
                        {
                            dependencyProperty.Value["version"] = $"[{newVersion}]";
                        }
                        else
                        {
                            dependencyProperty.Value = $"[{newVersion}]";
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        static JObject ReadProject(string projectJsonPath)
        {
            using (TextReader projectFileReader = File.OpenText(projectJsonPath))
            {
                var projectJsonReader = new JsonTextReader(projectFileReader);

                var serializer = new JsonSerializer();
                return serializer.Deserialize<JObject>(projectJsonReader);
            }
        }

        static void WriteProject(JObject projectRoot, string projectJsonPath)
        {
            string projectJson = JsonConvert.SerializeObject(projectRoot, Formatting.Indented);

            File.WriteAllText(projectJsonPath, projectJson + Environment.NewLine);
        }
    }
}
