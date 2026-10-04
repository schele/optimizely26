using EPiServer.ServiceLocation;
using EPiServer.Web;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Business
{
    [ServiceConfiguration]
    public class PageViewContextFactory
    {
        private readonly IContentLoader _contentLoader;

        public PageViewContextFactory(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public virtual LayoutModel GetLayoutModel(ContentReference contentReference, HttpContext httpContext)
        {
            var startPageContentLink = SiteDefinition.Current.StartPage;

            if (contentReference.CompareToIgnoreWorkID(startPageContentLink))
            {
                startPageContentLink = contentReference;
            }

            var startPage = _contentLoader.Get<StartPage>(startPageContentLink);

            return new LayoutModel()
            {
                StartPage = startPage,
                SettingsPage = GetSettingsPage(startPage)
            };
        }

        private SettingsPage? GetSettingsPage(StartPage? startPage)
        {
            if (startPage == null)
            {
                return null;
            }

            return _contentLoader.GetChildren<SettingsPage>(startPage.ContentLink).FirstOrDefault();
        }
    }
}
