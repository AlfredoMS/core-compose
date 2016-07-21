using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UpdateRepo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var u = new UpdateDependencyVersions(@"D:\git\core-setup",
                @"build_projects\shared-build-targets-utils\DependencyVersions.cs", new List<string>{ "CoreCLRVersion", "JitVersion" });

            u.Execute("1.0.4-beta-12345-6");
        }
    }
}
