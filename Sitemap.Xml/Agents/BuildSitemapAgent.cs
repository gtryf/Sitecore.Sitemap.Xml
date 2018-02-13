using LD.Sitemap.Xml.Events;
using System;

namespace LD.Sitemap.Xml.Agents
{
    public class BuildSitemapAgent
    {
        public void Run()
        {
            Sitecore.Events.Event.RaiseEvent("sitemap:rebuild", new EventArgs());
            Sitecore.Eventing.EventManager.QueueEvent(new BuildSitemapRemoteEvent());
        }
    }
}
