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
        private StartPage _startPage;

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

            _startPage = _contentLoader.Get<StartPage>(startPageContentLink);

            return new LayoutModel()
            {
                StartPage = _startPage,
                SettingsPage = GetSettingsPage()
            };
        }

        private SettingsPage? GetSettingsPage()
        {
            if (_startPage != null)
            {
                var settingsPage = _contentLoader.GetChildren<SettingsPage>(_startPage.ContentLink).FirstOrDefault();

                if (settingsPage != null)
                {
                    return settingsPage;
                }
            }

            return null;
        }
    }
}
