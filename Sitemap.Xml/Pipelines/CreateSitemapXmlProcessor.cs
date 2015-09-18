using Sitecore.Xml;
using System.Collections.Generic;
using System.Xml;

namespace LD.Sitemap.Xml.Pipelines
{
    public abstract class CreateSitemapXmlProcessor
    {
        private Dictionary<string, SiteDefinition> _configuration;

        protected Dictionary<string, SiteDefinition> Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    _configuration = new Dictionary<string, SiteDefinition>();
                    foreach (XmlNode node in Sitecore.Configuration.Factory.GetConfigNodes("sitemap/site"))
                    {
                        var def = new SiteDefinition
                        {
                            IncludedBaseTemplates = new List<string>(),
                            IncludedTemplates = new List<string>(),
                            ExcludedItems = new List<string>(),

                            EmbedLanguage = true,
                            SiteName = XmlUtil.GetAttribute("name", node),
                        };
                        var embed = XmlUtil.GetAttribute("embedLanguage", node);
                        if (!string.IsNullOrEmpty(embed))
                        {
                            bool embedVal;
                            if (!bool.TryParse(embed, out embedVal))
                                embedVal = true;
                            def.EmbedLanguage = embedVal;
                        }
                        
                        var baseTemplates = XmlUtil.FindChildNode("includeBaseTemplates", node, false);
                        var includeTemplates = XmlUtil.FindChildNode("includeTemplates", node, false);
                        var excludeItems = XmlUtil.FindChildNode("excludeItems", node, false);
                        if (baseTemplates != null)
                        {
                            foreach (XmlNode tmpl in baseTemplates.ChildNodes)
                                def.IncludedBaseTemplates.Add(tmpl.InnerText);
                        }
                        if (includeTemplates != null)
                        {
                            foreach (XmlNode tmpl in includeTemplates.ChildNodes)
                                def.IncludedTemplates.Add(tmpl.InnerText);
                        }
                        if (excludeItems != null)
                        {
                            foreach (XmlNode tmpl in excludeItems.ChildNodes)
                                def.ExcludedItems.Add(tmpl.InnerText);
                        }

                        _configuration.Add(XmlUtil.GetAttribute("name", node), def);
                    }
                }
                return _configuration;
            }
        }

        public abstract void Process(CreateSitemapXmlArgs args);
    }
}
