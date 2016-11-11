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
        private static Dictionary<string, NuGetVersion> versions;
        private static string rid;

        public static void Main(string[] args)
        {
            rid = RuntimeEnvironment.GetRuntimeIdentifier();
            if (args[0].ToLower() != "getruntimeid")
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

                UpdateDependencies();
            }
            Console.WriteLine("Runtime Identifier: {0}", rid);
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
            string updateRid = rid;
            if (rid.Contains("win"))
            {
                updateRid = "win7-x64";
            }

            string relativePath = NormalizeDirPath(@"build_projects\shared-build-targets-utils\DependencyVersions.cs");
            if (File.Exists(Path.Combine(repoRoot, relativePath)))
            {
                UpdateDependencyVersions u = new UpdateDependencyVersions(repoRoot, relativePath);
                u.Execute(versions);
            }

            relativePath = NormalizeDirPath(@"build_projects\dotnet-cli-build\CliDependencyVersions.cs");
            if (File.Exists(Path.Combine(repoRoot, relativePath)))
            {
                UpdateDependencyVersions u = new UpdateDependencyVersions(repoRoot, relativePath);
                u.Execute(versions);
            }

            relativePath = NormalizeDirPath(@"pkg/projects/Microsoft.NETCore.App/project.json");
            if (File.Exists(Path.Combine(repoRoot, relativePath)))
            {
                UpdateProjectJson.AddRuntimeId(new string[]{ Path.Combine(repoRoot, relativePath) }, updateRid);
            }

            IEnumerable<string> projectJsonFiles =
                Directory.GetFiles(Path.Combine(repoRoot, "TestAssets"), "project.json", SearchOption.AllDirectories);
            if (Directory.Exists(Path.Combine(repoRoot, "test")))
            {
                projectJsonFiles = projectJsonFiles.Union(Directory.GetFiles(Path.Combine(repoRoot, "test"), "project.json", SearchOption.AllDirectories));
            }

            relativePath = NormalizeDirPath(@"src\dotnet\commands\dotnet-new");
            if (Directory.Exists(Path.Combine(repoRoot, relativePath)))
            {
                projectJsonFiles = projectJsonFiles.Union(Directory.GetFiles(Path.Combine(repoRoot, relativePath), "project.json.template", SearchOption.AllDirectories));
            }

            projectJsonFiles = projectJsonFiles.Union(new string[] {
                Path.Combine(repoRoot, NormalizeDirPath(@"pkg\projects\Microsoft.NETCore.App\project.json"))
            });
            
            UpdateProjectJson.Execute(projectJsonFiles, versions, new List<string> { updateRid });

            // NOTE: assumes running on Windows 10
            UpdateProjectJson.Execute(new string[] { Path.Combine(repoRoot, NormalizeDirPath(@"TestAssets\TestProjects\StandaloneApp\project.json")) }, versions, new List<string> { rid });
            // NOTE: assumes running on Windows 10
            UpdateProjectJson.Execute(new string[] { Path.Combine(repoRoot, NormalizeDirPath(@"TestAssets\TestProjects\StandaloneTestApp\project.json")) }, versions, new List<string> { rid });

            relativePath = NormalizeDirPath(@"test\dotnet-publish.Tests\PublishTests.cs");
            if (File.Exists(Path.Combine(repoRoot, relativePath)))
            {
                // this test wants to cross-publish a core app, since it's likely there aren't any runtime packages
                // for all of the RIDs the test will fail.  as this test isn't super-interesting for testing isolated
                // changes to coreclr we just remove the Fact attribute so the test is never executed.
                UpdateTest.DemoteTestCases(Path.Combine(repoRoot, relativePath), new string[] { "CrossPublishingSucceedsAndHasExpectedArtifacts" });
            }
        }

        static string NormalizeDirPath(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
