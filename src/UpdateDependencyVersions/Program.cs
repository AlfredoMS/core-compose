using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UpdateDependencyVersions
{
    public static class Program
    {

        private static Regex packageNameRegex = new Regex(@"(?<name>.*)\.(?<version>\d+\.\d+\.\d+)(-(?<prerelease>.*)?)?");
        private static string repoRoot;
        private static string coreClrVersion;
        private static string jitVersion;
        private static string sharedFrameworkVersion;

        static void Main(string[] args)
        {
            repoRoot = args[0];
            string[] packageDrops = new string[args.Length - 1];
            Array.Copy(args, 1, packageDrops, 0, args.Length - 1);

            var packageItems = GatherPackageInformationFromDrops(packageDrops);

            if (packageItems.ContainsKey("Microsoft.NETCore.Runtime.CoreCLR"))
            {
                coreClrVersion = packageItems["Microsoft.NETCore.Runtime.CoreCLR"];
            }
            if (packageItems.ContainsKey("Microsoft.NETCore.Jit"))
            {
                jitVersion = packageItems["Microsoft.NETCore.Jit"];
            }
            if (packageItems.ContainsKey("Microsoft.NETCore.App"))
            {
                sharedFrameworkVersion = packageItems["Microsoft.NETCore.App"];
            }
            ReplaceDependencyVersions();
        }

        private static Dictionary<string, string> GatherPackageInformationFromDrops(string[] packagesDrops)
        {
            Dictionary<string, string> packageItems = new Dictionary<string, string>();

            foreach (string packageDrop in packagesDrops)
            {
                if (!Directory.Exists(packageDrop))
                {
                    Console.WriteLine("PackageDrop does not exist - '{0}'", packageDrop);
                    continue;
                }
                IEnumerable<string> packages = Directory.GetFiles(packageDrop, "*");

                foreach (var package in packages)
                {
                    Match match = packageNameRegex.Match(package);
                    if (match.Success)
                    {
                        string name = Path.GetFileName(match.Groups["name"].Value);
                        string version = match.Groups["version"].Value;
                        if(match.Groups["prerelease"] != null)
                        {
                            string prerelease = match.Groups["prerelease"].Value; ;
                            if(prerelease.EndsWith(".nupkg") ||
                                prerelease.EndsWith(".zip") ||
                                prerelease.EndsWith(".msi"))
                            {
                                prerelease = Path.GetFileNameWithoutExtension(prerelease);
                            }
                            version += "-" + prerelease;
                            if(!packageItems.ContainsKey(name))
                            {
                                packageItems.Add(name, version);
                            }
                        }
                    }
                }
            }
            return packageItems;
        }

        /// <summary>
        /// Replaces version numbers hard-coded in DependencyVersions.cs.
        /// </summary>
        public static void ReplaceDependencyVersions()
        {
            string dependencyVersionsFile = Path.Combine(repoRoot, @"build_projects\shared-build-targets-utils\DependencyVersions.cs");
            if (File.Exists(dependencyVersionsFile))
            {
                ReplaceFileContents(dependencyVersionsFile, fileContents =>
                {
                    fileContents = ReplaceDependencyVersion(fileContents, "CoreCLRVersion", coreClrVersion);
                    fileContents = ReplaceDependencyVersion(fileContents, "JitVersion", jitVersion);

                    return fileContents;
                });
            };
            string cliDependencyVersionsFile = Path.Combine(repoRoot, @"build_projects\dotnet-cli-build\CliDependencyVersions.cs");
            if (File.Exists(cliDependencyVersionsFile))
            {
                ReplaceFileContents(cliDependencyVersionsFile, fileContents =>
                {
                    fileContents = ReplaceDependencyVersion(fileContents, "SharedFrameworkVersion", sharedFrameworkVersion);

                    return fileContents;
                });
            }
            return;
        }
        private static string ReplaceDependencyVersion(string fileContents, string dependencyPropertyName, string newVersion)
        {
            Regex regex = new Regex($@"{dependencyPropertyName} = ""(?<version>.*)"";");

            return ReplaceGroupValue(regex, fileContents, "version", newVersion);
        }
        private static void ReplaceFileContents(string repoRelativePath, Func<string, string> replacement)
        {
            string fullPath = Path.Combine(repoRoot, repoRelativePath);
            string contents = File.ReadAllText(fullPath);

            contents = replacement(contents);

            File.WriteAllText(fullPath, contents, Encoding.UTF8);
        }

        private static string ReplaceGroupValue(this Regex regex, string input, string groupName, string newValue)
        {
            return regex.Replace(input, m =>
            {
                string replacedValue = m.Value;
                Group group = m.Groups[groupName];
                int startIndex = group.Index - m.Index;

                replacedValue = replacedValue.Remove(startIndex, group.Length);
                replacedValue = replacedValue.Insert(startIndex, newValue);

                return replacedValue;
            });
        }
    }
}
