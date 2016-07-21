// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UpdateRepo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            UpdateCoreSetup();
        }

        static void UpdateCoreSetup()
        {
            var feeds = new Dictionary<string, string> { { "coreclr", @"D:\git\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg" } };

            var nc = new UpdateNuGetConfig(@"D:\git\core-setup\NuGet.Config");
            nc.Execute(feeds);

            var u = new UpdateDependencyVersions(@"D:\git\core-setup", @"build_projects\shared-build-targets-utils\DependencyVersions.cs");

            var versions = new List<Tuple<string, NuGetVersion>>
            {
                Tuple.Create("CoreCLRVersion", new NuGetVersion("1.0.4-beta-24318-0")),
                Tuple.Create("JitVersion", new NuGetVersion("1.0.4-beta-24318-0"))
            };

            u.Execute(versions);

            IEnumerable<string> projectJsonFiles = Enumerable.Union(Enumerable.Union(
                Directory.GetFiles(@"D:\git\core-setup\TestAssets", "project.json", SearchOption.AllDirectories),
                Directory.GetFiles(@"D:\git\core-setup\build_projects", "project.json", SearchOption.AllDirectories)),
                Directory.GetFiles(@"D:\git\core-setup\pkg", "project.json", SearchOption.AllDirectories))
                .Where(p => !File.Exists(Path.Combine(Path.GetDirectoryName(p), ".noautoupdate")) &&
                    !Path.GetDirectoryName(p).EndsWith("CSharp_Web", StringComparison.Ordinal));

            var packages = new List<PackageInfo>
            {
                new PackageInfo { Id = "Microsoft.NETCore.Runtime.CoreCLR", Version = new NuGetVersion("1.0.4-beta-24318-0") }
            };

            var rids = new List<string> { "win7-x64" };
            UpdateProjectJson.Execute(projectJsonFiles, packages, rids);
        }

        static void UpdateCli()
        {
            var feeds = new Dictionary<string, string> { { "coreclr", @"D:\git\coreclr\bin\Product\Windows_NT.x64.Release\.nuget\pkg" } };

            var nc = new UpdateNuGetConfig(@"D:\git\core-setup\NuGet.Config");
            nc.Execute(feeds);

            var u = new UpdateDependencyVersions(@"D:\git\core-setup", @"build_projects\shared-build-targets-utils\DependencyVersions.cs");

            var versions = new List<Tuple<string, NuGetVersion>>
            {
                Tuple.Create("CoreCLRVersion", new NuGetVersion("1.0.4-beta-24318-0")),
                Tuple.Create("JitVersion", new NuGetVersion("1.0.4-beta-24318-0"))
            };

            u.Execute(versions);

            IEnumerable<string> projectJsonFiles = Enumerable.Union(
                Directory.GetFiles(@"D:\git\core-setup", "project.json", SearchOption.AllDirectories),
                Directory.GetFiles(Path.Combine(@"D:\git\core-setup", @"src\dotnet\commands\dotnet-new"), "project.json.template", SearchOption.AllDirectories))
                .Where(p => !File.Exists(Path.Combine(Path.GetDirectoryName(p), ".noautoupdate")) &&
                    !Path.GetDirectoryName(p).EndsWith("CSharp_Web", StringComparison.Ordinal));

            var packages = new List<PackageInfo>
            {
                new PackageInfo { Id = "Microsoft.NETCore.Runtime.CoreCLR", Version = new NuGetVersion("1.0.4-beta-24318-0") }
            };

            var rids = new List<string> { "win7-x64" };
            UpdateProjectJson.Execute(projectJsonFiles, packages, rids);
        }
    }
}
