using Sitecore.Pipelines;
using LD.Sitemap.Xml.Events;

namespace LD.Sitemap.Xml.Pipelines
{
    public class InitializeRemoteEvents
    {
        public virtual void InitializeFromPipeline(PipelineArgs args)
        {
            Sitecore.Eventing.EventManager.Subscribe<BuildSitemapRemoteEvent>(e =>
            {
                Sitecore.Events.Event.RaiseEvent("sitemap:rebuild:remote", new object[] { });
            });
        }
    }
}
