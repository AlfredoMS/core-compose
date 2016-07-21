// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private IReadOnlyList<string> m_versions;

        public UpdateDependencyVersions(string repoRoot, string depsCsFile, IReadOnlyList<string> versions)
        {
            m_repoRoot = repoRoot;
            m_depsCsFile = depsCsFile;
            m_versions = versions;
        }

        public void Execute(string coreClrAndJitVer)
        {
            Execute(coreClrAndJitVer, coreClrAndJitVer);
        }

        public void Execute(string coreClrVer, string jitVer)
        {
            ReplaceFileContents(Path.Combine(m_repoRoot, m_depsCsFile), fileContents =>
            {
                foreach (var version in m_versions)
                    fileContents = ReplaceDependencyVersion(fileContents, version, coreClrVer);

                return fileContents;
            });
        }

        private void ReplaceFileContents(string depsVersCsFile, Func<string, string> replacement)
        {
            string contents = File.ReadAllText(depsVersCsFile);
            contents = replacement(contents);
            File.WriteAllText(depsVersCsFile, contents, Encoding.UTF8);
        }

        private string ReplaceDependencyVersion(string fileContents, string dependencyPropertyName, string version)
        {
            Regex regex = new Regex($@"{dependencyPropertyName} = ""(?<version>.*)"";");
            return regex.ReplaceGroupValue(fileContents, "version", version);
        }
    }
}
