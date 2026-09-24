using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Initialization;
using EPiServer.ServiceLocation;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(CmsCoreInitialization))]
    public class UpdateSitemapDateOnPublish : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var events = ServiceLocator.Current.GetInstance<IContentEvents>();
            events.PublishingContent += OnPublishingContent;
        }

        private void OnPublishingContent(object sender, ContentEventArgs e)
        {
            if (e.Content is SitePageData page)
            {
                page.XmlSitemapDate = DateTime.Now;
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
