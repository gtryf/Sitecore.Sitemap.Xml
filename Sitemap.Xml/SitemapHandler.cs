using System;
using Sitecore.Xml;
using System.Linq;
using System.Web;
using System.Xml;
using System.IO;

namespace LD.Sitemap.Xml
{
    public sealed class SitemapHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            var configuredSites = Sitecore.Configuration.Factory.GetConfigNodes("sitemap/site")
                .Cast<XmlNode>()
                .Select(node => XmlUtil.GetAttribute("name", node));

            var website = Sitecore.Configuration.Factory.GetSiteInfoList()
                .FirstOrDefault(i => i.HostName != null &&
                    i.HostName.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(n => string.Equals(n, context.Request.Url.Host, StringComparison.CurrentCultureIgnoreCase)));

            if (website == null || (website.Port > 0 && website.Port != context.Request.Url.Port))
            {
                context.Response.StatusCode = 404;
                return;
            }

            if (!configuredSites.Contains(website.Name))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var folder = Sitecore.IO.FileUtil.MapPath($"{Sitecore.Configuration.Settings.GetSetting("Sitemap.Xml.TempDirectory").TrimEnd('/')}/{website.Name}");
            if (context.Request.Path == "/sitemap.xml" && !File.Exists($"{folder}\\sitemap.xml") && File.Exists($"{folder}\\sitemap_index.xml"))
                context.Response.Redirect("/sitemap_index.xml");
            else
            {
                var path = $"{folder}\\{context.Request.Path.TrimStart('/')}";
                if (!File.Exists(path))
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                else
                {
                    context.Response.ContentType = "application/xml";
                    context.Response.Write(File.ReadAllText(path));
                }
            }
        }
    }
}
