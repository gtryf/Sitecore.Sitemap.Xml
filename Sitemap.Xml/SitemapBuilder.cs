using LD.Sitemap.Xml.Pipelines;
using Sitecore.Pipelines;
using Sitecore.Sites;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace LD.Sitemap.Xml
{
    public class SitemapBuilder
    {
        private const int SizeLimit = 10 * 1000 * 1024; // Slightly biased towards 0
        private const int NodeLimit = 10000;

        public void OnRebuild(object sender, EventArgs ea)
        {
            var configuredSites = Sitecore.Configuration.Factory.GetConfigNodes("sitemap/site")
                .Cast<XmlNode>()
                .Select(node => XmlUtil.GetAttribute("name", node));
            var availableSites = Sitecore.Configuration.Factory.GetSiteInfoList().Where(s => configuredSites.Contains(s.Name));

            foreach (var website in availableSites)
            {
                using (new SiteContextSwitcher(new SiteContext(website)))
                {
                    var folder = Sitecore.IO.FileUtil.MapPath($"{Sitecore.Configuration.Settings.GetSetting("Sitemap.Xml.TempDirectory").TrimEnd('/')}/{website.Name}");
                    var di = Directory.CreateDirectory(folder);

                    foreach (var file in di.EnumerateFiles("*.tmp"))
                        file.Delete();

                    var segments = new List<string>(); // to hold each sitemap segment filename
                    var args = new CreateSitemapXmlArgs(website);
                    CorePipeline.Run("createSitemapXml", args);

                    int count = 0, suffix = 0;
                    var sb = new StringBuilder();
                    foreach (var node in args.Nodes)
                    {
                        sb.Append("<url>")
                            .AppendFormat("<loc>{0}</loc>", node.Location)
                            .AppendFormat("<lastmod>{0}</lastmod>", node.LastModified.ToString("yyyy-MM-dd"))
                            .Append("</url>");
                        count++;

                        if (count >= NodeLimit || sb.Length >= SizeLimit)
                        {
                            var fileName = $"sitemap_segment_{++suffix}.tmp";
                            File.WriteAllText(folder + "\\" + fileName, "<?xml version=\"1.0\" encoding=\"utf-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" + sb.ToString() + "</urlset>");
                            segments.Add(fileName);
                            sb.Clear();
                            count = 0;
                        }
                    }

                    if (segments.Any())
                    {
                        // Prepare sitemap index
                        var sbIndex = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?>")
                            .Append("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
                        // The following kludge uses the first sitemap node to generate the sitemmap segment prefix
                        Uri firstLoc = new Uri(args.Nodes.First().Location);
                        var prefix = $"{firstLoc.Scheme}://{firstLoc.Host}/";
                        foreach (var segment in segments)
                        {
                            sbIndex.Append("<sitemap>")
                                .AppendFormat("<loc>{0}</loc>", prefix + segment.Replace(".tmp", ".xml"))
                                .AppendFormat("<lastmod>{0}</lastmod>", DateTime.Now.ToString("yyyy-MM-dd"))
                                .Append("</sitemap>");
                        }
                        sbIndex.Append("</sitemapindex>");
                        File.WriteAllText($"{folder}\\sitemap_index.tmp", sbIndex.ToString());
                    }
                    else
                    {
                        // Dump whole sitemap
                        File.WriteAllText($"{folder}\\sitemap.tmp", "<?xml version=\"1.0\" encoding=\"utf-8\"?><urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">" + sb.ToString() + "</urlset>");
                    }

                    foreach (var file in di.EnumerateFiles("*.xml"))
                        file.Delete();

                    foreach (var file in di.EnumerateFiles("*.tmp"))
                        file.MoveTo(Path.ChangeExtension(file.FullName, ".xml"));
                }
            }
        }
    }
}
