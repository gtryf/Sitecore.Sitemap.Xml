using Sitecore.Xml;
using System.Linq;
using System.Web;
using System.Xml;
using System;
using System.IO;
using System.Text;
using LD.Sitemap.Xml.Events;

namespace LD.Sitemap.Xml
{
    public class RobotsHandler : IHttpHandler
    {
        public bool IsReusable { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            if (!string.IsNullOrEmpty(context.Request.QueryString["build"]))
            {
                Sitecore.Events.Event.RaiseEvent("sitemap:rebuild", new EventArgs());
                Sitecore.Eventing.EventManager.QueueEvent(new BuildSitemapRemoteEvent());
                context.Response.Write("DONE");
                return;
            }

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

            if (context.Request.Path.TrimEnd('/') != "/robots.txt")
            {
                context.Response.StatusCode = 404;
                return;
            }

            var folder = Sitecore.IO.FileUtil.MapPath($"{Sitecore.Configuration.Settings.GetSetting("Sitemap.Xml.TempDirectory").TrimEnd('/')}/{website.Name}");
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *")
                .AppendLine("Allow: /")
                .AppendLine();

            var prefix = $"{context.Request.Url.Scheme}://{context.Request.Url.Host}/";
            if (File.Exists(folder + "\\sitemap.xml"))
            {
                sb.AppendLine(prefix + "sitemap.xml");
            }
            else if (File.Exists(folder + "\\sitemap_index.xml"))
            {
                sb.AppendLine("Sitemap: " + prefix + "sitemap_index.xml");
                var di = new DirectoryInfo(folder);
                var files = di.EnumerateFiles("*.xml").Where(i => i.Name != "sitemap_index.xml");
                foreach (var file in files)
                {
                    sb.AppendLine("Sitemap: " + prefix + file.Name);
                }
            }

            context.Response.Write(sb.ToString());
        }
    }
}
