using EPiServer.Applications;
using EPiServer.ServiceLocation;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Business
{
    [ServiceConfiguration]
    public class PageViewContextFactory
    {
        private readonly IContentLoader _contentLoader;
        private readonly IApplicationResolver _applicationResolver;

        public PageViewContextFactory(IContentLoader contentLoader, IApplicationResolver applicationResolver)
        {
            _contentLoader = contentLoader;
            _applicationResolver = applicationResolver;
        }

        public virtual LayoutModel GetLayoutModel(ContentReference contentReference, HttpContext httpContext)
        {
            var startPageContentLink = (_applicationResolver.GetByContext() as IRoutableApplication)?.EntryPoint ?? ContentReference.EmptyReference;

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
