using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Sites;
using System.Collections.Generic;
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

        private IEnumerable<UrlDefinition> ProcessSite(Sitecore.Data.Items.Item homeItem, SiteDefinition def, Sitecore.Globalization.Language language)
        {
            IProviderSearchContext ctx;
            if (string.IsNullOrEmpty(this.indexName))
                ctx = ContentSearchManager.GetIndex((SitecoreIndexableItem)homeItem).CreateSearchContext();
            else
                ctx = ContentSearchManager.GetIndex(this.indexName).CreateSearchContext();

            try
            {
                var results = ctx.GetQueryable<SitemapResultItem>()
                    .Where(i => i.Paths.Contains(homeItem.ID) && i.Language == language.Name);
                var tmplPred = PredicateBuilder.False<SitemapResultItem>();
                foreach (var tmpl in def.IncludedBaseTemplates.Select(i => Sitecore.Data.ID.Parse(i)))
                    tmplPred = tmplPred.Or(i => i.AllTemplates.Contains(tmpl));
                foreach (var tmpl in def.IncludedTemplates.Select(i => Sitecore.Data.ID.Parse(i)))
                    tmplPred = tmplPred.Or(i => i.TemplateId == tmpl);
                var itemPred = PredicateBuilder.True<SitemapResultItem>();
                foreach (var id in def.ExcludedItems.Select(i => Sitecore.Data.ID.Parse(i)))
                    itemPred = itemPred.And(i => i.ItemId != id);
                results = results.Where(tmplPred.And(itemPred));

                var items = results
                    .Select(i => Sitecore.Configuration.Factory.GetDatabase(i.DatabaseName).GetItem(i.ItemId, Sitecore.Globalization.Language.Parse(i.Language), Sitecore.Data.Version.Latest))
                    .ToList();

                var sb = new StringBuilder();
                var options = Sitecore.Links.UrlOptions.DefaultOptions;
                options.SiteResolving = Sitecore.Configuration.Settings.Rendering.SiteResolving;
                options.Site = SiteContext.GetSite(def.SiteName);
                if (def.EmbedLanguage)
                    options.LanguageEmbedding = Sitecore.Links.LanguageEmbedding.Always;
                else
                    options.LanguageEmbedding = Sitecore.Links.LanguageEmbedding.Never;
                options.AlwaysIncludeServerUrl = true;
                options.Language = language;
                foreach (var item in items)
                {
                    if (item.Versions.Count > 0)
                        yield return new UrlDefinition(Sitecore.Links.LinkManager.GetItemUrl(item, options), item.Statistics.Updated);
                }
            }
            finally
            {
                ctx.Dispose();
            }
        }

        public override void Process(CreateSitemapXmlArgs args)
        {
            var langs = Sitecore.Data.Managers.LanguageManager.GetLanguages(Sitecore.Context.Database);
            var homeItem = Sitecore.Context.Database.GetItem(args.Site.RootPath + args.Site.StartItem);

            var def = this.Configuration[args.Site.Name];
            if (def.EmbedLanguage)
            {
                foreach (var lang in langs)
                    args.Nodes.AddRange(ProcessSite(homeItem, def, lang));
            }
            else
            {
                args.Nodes.AddRange(ProcessSite(homeItem, def, Sitecore.Context.Language));
            }
        }
    }

    class SitemapResultItem : SearchResultItem
    {
        [IndexField("_templates")]
        public IEnumerable<Sitecore.Data.ID> AllTemplates { get; set; }
    }
}
