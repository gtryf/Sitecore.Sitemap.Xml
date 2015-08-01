using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Sites;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LD.Sitemap.Xml.Pipelines
{
    public class DefaultSitemapXmlProcessor : CreateSitemapXmlProcessor
    {
        private string indexName;

        public DefaultSitemapXmlProcessor(string indexName)
        {
            this.indexName = indexName;
        }

        private Dictionary<string, SiteDefinition> Configuration { get; set; }

        private void ParseConfig()
        {
            Configuration = new Dictionary<string, SiteDefinition>();
            foreach (XmlNode node in Sitecore.Configuration.Factory.GetConfigNodes("sitemap/site"))
            {
                var def = new SiteDefinition
                {
                    IncludedBaseTemplates = new List<string>(),
                    IncludedTemplates = new List<string>(),
                    ExcludedItems = new List<string>(),
                };

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

                Configuration.Add(XmlUtil.GetAttribute("name", node), def);
            }
        }

        private bool ImplementsTemplate(Sitecore.Data.Items.Item item, Sitecore.Data.ID templateID)
        {
            var itemTemplate = Sitecore.Data.Managers.TemplateManager.GetTemplate(item);
            return itemTemplate.InheritsFrom(templateID);
        }

        public override void Process(CreateSitemapXmlArgs args)
        {
            ParseConfig();

            var langs = Sitecore.Data.Managers.LanguageManager.GetLanguages(Sitecore.Context.Database);
            var homeItem = Sitecore.Context.Database.GetItem(args.Site.RootPath + args.Site.StartItem);

            IProviderSearchContext ctx;
            if (string.IsNullOrEmpty(this.indexName))
                ctx = ContentSearchManager.GetIndex((SitecoreIndexableItem)homeItem).CreateSearchContext();
            else
                ctx = ContentSearchManager.GetIndex(this.indexName).CreateSearchContext();

            try
            {
                foreach (var lang in langs)
                {
                    var results = ctx.GetQueryable<SearchResultItem>()
                        .Where(i => i.Paths.Contains(homeItem.ID) && i.Language == lang.Name);
                    var tmplPred = PredicateBuilder.False<SearchResultItem>();
                    foreach (var tmpl in Configuration[args.Site.Name].IncludedBaseTemplates)
                        tmplPred = tmplPred.Or(i => i["_templates"] == tmpl);
                    foreach (var tmpl in Configuration[args.Site.Name].IncludedTemplates.Select(i => Sitecore.Data.ID.Parse(i)))
                        tmplPred = tmplPred.Or(i => i.TemplateId == tmpl);
                    var itemPred = PredicateBuilder.True<SearchResultItem>();
                    foreach (var id in Configuration[args.Site.Name].ExcludedItems.Select(i => Sitecore.Data.ID.Parse(i)))
                        itemPred = itemPred.And(i => i.ItemId != id);
                    results = results.Where(tmplPred.And(itemPred));
                    
                    var items = results
                        .Select(i => Sitecore.Configuration.Factory.GetDatabase(i.DatabaseName).GetItem(i.ItemId, Sitecore.Globalization.Language.Parse(i.Language), Sitecore.Data.Version.Latest))
                        .ToList();

                    var sb = new StringBuilder();
                    var options = Sitecore.Links.UrlOptions.DefaultOptions;
                    options.SiteResolving = Sitecore.Configuration.Settings.Rendering.SiteResolving;
                    options.Site = SiteContext.GetSite(args.Site.Name);
                    options.AlwaysIncludeServerUrl = true;
                    options.Language = lang;
                    foreach (var item in items)
                    {
                        if (item.Versions.Count > 0)
                            args.Nodes.Add(new UrlDefinition(Sitecore.Links.LinkManager.GetItemUrl(item, options), item.Statistics.Updated));
                    }
                }
            }
            finally
            {
                ctx.Dispose();
            }
        }
    }
}
