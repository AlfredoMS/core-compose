// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Versioning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace UpdateRepo
{
    public class Program
    {
        private static Regex packageNameRegex = new Regex(@"(?<name>.*)\.(?<version>\d+\.\d+\.\d+)(-(?<prerelease>.*)?)?");
        private static string repoRoot;
        private static string coreClrVersion;
        private static string jitVersion;
        private static string sharedFrameworkVersion;
        private static Dictionary<string, NuGetVersion> versions;
        public static void Main(string[] args)
        {
            repoRoot = args[0];
            string[] packageDrops = new string[args.Length - 1];
            Array.Copy(args, 1, packageDrops, 0, args.Length - 1);

            var packageItems = GatherPackageInformationFromDrops(packageDrops);
            versions = new Dictionary<string, NuGetVersion>();
            if (packageItems.ContainsKey("Microsoft.NETCore.Runtime.CoreCLR"))
            {
                versions.Add("CoreCLRVersion", new NuGetVersion(packageItems["Microsoft.NETCore.Runtime.CoreCLR"]));
            }
            if (packageItems.ContainsKey("Microsoft.NETCore.Jit"))
            {
                versions.Add("JitVersion", new NuGetVersion(packageItems["Microsoft.NETCore.Jit"]));
            }
            if (packageItems.ContainsKey("Microsoft.NETCore.App"))
            {
                versions.Add("SharedFrameworkVersion", new NuGetVersion(packageItems["Microsoft.NETCore.App"]));
            }

            UpdateDependencies();
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
                        if (match.Groups["prerelease"] != null)
                        {
                            string prerelease = match.Groups["prerelease"].Value; ;
                            if (prerelease.EndsWith(".nupkg") ||
                                prerelease.EndsWith(".zip") ||
                                prerelease.EndsWith(".msi"))
                            {
                                prerelease = Path.GetFileNameWithoutExtension(prerelease);
                            }
                            version += "-" + prerelease;
                            if (!packageItems.ContainsKey(name))
                            {
                                packageItems.Add(name, version);
                            }
                        }
                    }
                }
            }
            return packageItems;
        }

        static void UpdateDependencies()
        {
            if (File.Exists(Path.Combine(repoRoot, @"build_projects\shared-build-targets-utils\DependencyVersions.cs")))
            {
                UpdateDependencyVersions u = new UpdateDependencyVersions(repoRoot, @"build_projects\shared-build-targets-utils\DependencyVersions.cs");

                u.Execute(versions);
            }
            if (File.Exists(Path.Combine(repoRoot, @"build_projects\dotnet-cli-build\CliDependencyVersions.cs")))
            {
                UpdateDependencyVersions u = new UpdateDependencyVersions(repoRoot, @"build_projects\dotnet-cli-build\CliDependencyVersions.cs");

                u.Execute(versions);
            }


            // project.json under here doesn't have a Windows 10 RID
            UpdateProjectJson.Execute(Directory.GetFiles(Path.Combine(repoRoot, @"build_projects"),
                "project.json", SearchOption.AllDirectories), versions, new List<string> { "win7-x64" });

            IEnumerable<string> projectJsonFiles =
                Directory.GetFiles(Path.Combine(repoRoot, "TestAssets"), "project.json", SearchOption.AllDirectories);
            if (Directory.Exists(Path.Combine(repoRoot, "test")))
            {
                projectJsonFiles = projectJsonFiles.Union(Directory.GetFiles(Path.Combine(repoRoot, "test"), "project.json", SearchOption.AllDirectories));
            }
            projectJsonFiles = projectJsonFiles.Union(new string[] {
                Path.Combine(repoRoot, @"tools\Archiver\project.json"),
                Path.Combine(repoRoot, @"tools\MultiProjectValidator\project.json"),
                Path.Combine(repoRoot, @"src\dotnet\project.json"),
                Path.Combine(repoRoot, @"src\compilers\project.json"),
                Path.Combine(repoRoot, @"src\dotnet-archive\project.json"),
                Path.Combine(repoRoot, @"src\dotnet-compile-fsc\project.json"),
                Path.Combine(repoRoot, @"pkg\projects\Microsoft.NETCore.App\project.json")
            });
            
            

            UpdateProjectJson.Execute(projectJsonFiles, versions, new List<string> { "win7-x64" });

            // NOTE: assumes running on Windows 10
            UpdateProjectJson.Execute(new string[] { Path.Combine(repoRoot, @"TestAssets\TestProjects\StandaloneApp\project.json") }, versions, new List<string> { "win10-x64" });
            // NOTE: assumes running on Windows 10
            UpdateProjectJson.Execute(new string[] { Path.Combine(repoRoot, @"TestAssets\TestProjects\StandaloneTestApp\project.json") }, versions, new List<string> { "win10-x64" });

        }
    }
}
