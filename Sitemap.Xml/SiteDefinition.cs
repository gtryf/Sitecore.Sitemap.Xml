using System.Collections.Generic;

namespace LD.Sitemap.Xml
{
    public sealed class SiteDefinition
    {
        public List<string> IncludedBaseTemplates { get; set; }
        public List<string> IncludedTemplates { get; set; }
        public List<string> ExcludedItems { get; set; }

        public bool EmbedLanguage { get; set; }
        public string SiteName { get; set; }
    }
}
