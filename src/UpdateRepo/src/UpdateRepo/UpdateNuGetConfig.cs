// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UpdateRepo
{
    public class UpdateNuGetConfig
    {
        private string m_nugetConfig;

        public UpdateNuGetConfig(string nugetConfigFile)
        {
            if (!File.Exists(nugetConfigFile))
                throw new FileNotFoundException("The file does not exist.", nugetConfigFile);

            m_nugetConfig = nugetConfigFile;
        }

        public void Execute(Dictionary<string, string> feeds)
        {
            var nugetConfig = XDocument.Load(m_nugetConfig);

            // NOTE: assumes the clear element exists
            var addAfter =
                (from el in nugetConfig.Root.Descendants("clear")
                 select el).First();

            foreach (var feed in feeds)
            {
                XElement e = new XElement("add");
                e.SetAttributeValue("key", feed.Key);
                e.SetAttributeValue("value", feed.Value);
                addAfter.AddAfterSelf(e);
                addAfter = e;
            }

            Console.WriteLine($"Writing changes to {m_nugetConfig}");
            using (var writer = new FileStream(m_nugetConfig, FileMode.Create))
                nugetConfig.Save(writer);
        }
    }
}
