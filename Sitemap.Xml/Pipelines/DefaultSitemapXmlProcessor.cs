using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Sites;
using System.Linq;
using System.Text;

namespace LD.Sitemap.Xml.Pipelines
{
    public class DefaultSitemapXmlProcessor : CreateSitemapXmlProcessor
    {
        private string indexName;

        public DefaultSitemapXmlProcessor(string indexName)
        {
            this.indexName = indexName;
        }

        public override void Process(CreateSitemapXmlArgs args)
        {
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
