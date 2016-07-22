// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace UpdateRepo
{
    public class UpdateDependencyVersions
    {
        private string m_repoRoot;
        private string m_depsCsFile;

        public UpdateDependencyVersions(string repoRoot, string depsCsFile)
        {
            m_repoRoot = repoRoot;
            m_depsCsFile = depsCsFile;
        }

        public void Execute(Dictionary<string, NuGetVersion> versions)
        {
            ReplaceFileContents(Path.Combine(m_repoRoot, m_depsCsFile), fileContents =>
            {
                foreach (var version in versions)
                    fileContents = ReplaceDependencyVersion(fileContents, version.Key, version.Value.ToNormalizedString());

                return fileContents;
            });
        }

        private void ReplaceFileContents(string depsVersCsFile, Func<string, string> replacement)
        {
            string contents = File.ReadAllText(depsVersCsFile);
            contents = replacement(contents);
            Console.WriteLine($"Writing changes to {depsVersCsFile}");
            File.WriteAllText(depsVersCsFile, contents, Encoding.UTF8);
        }

        private string ReplaceDependencyVersion(string fileContents, string dependencyPropertyName, string version)
        {
            Regex regex = new Regex($@"{dependencyPropertyName} = ""(?<version>.*)"";");
            return regex.ReplaceGroupValue(fileContents, "version", version);
        }
    }
}
