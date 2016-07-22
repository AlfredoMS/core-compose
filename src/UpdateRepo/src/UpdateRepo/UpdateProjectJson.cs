// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public static void Execute(IEnumerable<string> projectJsonFiles, Dictionary<string, NuGetVersion> versions, List<string> rids)
        {
            var packages = new List<PackageInfo>
            {
                new PackageInfo { Id = "Microsoft.NETCore.Runtime.CoreCLR", Version = versions["CoreCLRVersion"] },
                new PackageInfo { Id = "Microsoft.NETCore.Jit", Version= versions["JitVersion"]}
            };
            if (versions.ContainsKey("SharedFrameworkVersion"))
            {
                packages.Add(new PackageInfo { Id = "Microsoft.NETCore.App", Version = versions["SharedFrameworkVersion"] });
            }
            foreach (string projectJsonFile in projectJsonFiles)
            {
                if(!File.Exists(projectJsonFile) || projectJsonFile.Contains("IncorrectProjectJson"))
                {
                    continue;
                }
                Console.WriteLine("Updating {0}", projectJsonFile);
                var projectRoot = ReadProject(projectJsonFile);

                if (projectRoot == null)
                    throw new Exception($"A non valid JSON file was encountered '{projectJsonFile}'. Skipping file.");

                bool isDirty = false;
                if (packages != null)
                {
                    isDirty = FindAllDependencyProperties(projectRoot)
                        .Select(dependencyProperty => ReplaceDependencyVersion(dependencyProperty, packages))
                        .ToArray()
                        .Any(shouldWrite => shouldWrite);
                }

                if (rids != null)
                    isDirty |= FilterRIDs(projectRoot, rids);

                if (isDirty)
                {
                    Console.WriteLine($"Writing changes to {projectJsonFile}");
                    WriteProject(projectRoot, projectJsonFile);
                }
            }
        }

        static bool FilterRIDs(JObject projectJsonRoot, List<string> rids)
        {
            if (projectJsonRoot["runtimes"] == null)
                return false;

            // replace the existing set of RIDs with the contents of rids
            var runtimes = new JObject();
            foreach (var rid in rids)
                runtimes.Add(rid, new JObject());

            projectJsonRoot["runtimes"] = runtimes;
            return true;
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
                            dependencyProperty.Value["version"] = newVersion;
                        }
                        else
                        {
                            dependencyProperty.Value = newVersion;
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
