// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UpdateRepo
{
    public static class UpdateTest
    {
        public static void DemoteTestCases(string csFile, IEnumerable<string> testCases)
        {
            // NOTE: this is pretty cheesy, should use a real code analyzer

            var buffer = new List<string>();
            bool isDirty = false;
            Encoding csEncoding = null;

            using (var reader = File.OpenText(csFile))
            {
                csEncoding = reader.CurrentEncoding;    
                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (buffer.Count > 0 && buffer[buffer.Count - 1].IndexOf("[Fact]") > -1)
                    {
                        bool skipAdd = false;

                        // just found a fact, see if the following line is a test case we should demote
                        // NOTE: assumes function name is immediately after the Fact attribute
                        foreach (var testCase in testCases)
                        {
                            if (line.IndexOf(testCase) > -1)
                            {
                                // demote the test case by removing the Fact attribute
                                buffer[buffer.Count - 1] = line;
                                isDirty = true;
                                skipAdd = true;
                                break;
                            }
                        }

                        if (skipAdd)
                            continue;
                    }

                    buffer.Add(line);
                }
            }

            if (isDirty)
                File.WriteAllLines(csFile, buffer, csEncoding);
        }
    }
}
