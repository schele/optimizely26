using EPiServer.Applications;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Business
{
    [ServiceConfiguration]
    public class PageViewContextFactory
    {
        private readonly IContentLoader _contentLoader;
        private readonly IApplicationResolver _applicationResolver;
        private readonly IPublishedStateAssessor _publishedStateAssessor;
        private readonly IUrlResolver _urlResolver;

        public PageViewContextFactory(IContentLoader contentLoader, IApplicationResolver applicationResolver, IPublishedStateAssessor publishedStateAssessor, IUrlResolver urlResolver)
        {
            _contentLoader = contentLoader;
            _applicationResolver = applicationResolver;
            _publishedStateAssessor = publishedStateAssessor;
            _urlResolver = urlResolver;
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
                SettingsPage = GetSettingsPage(startPage),
                FindPageUrl = GetFindPageUrl(startPage)
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

        /// <summary>The first published find page under the start page, so visitors are never sent to a 404.</summary>
        private string? GetFindPageUrl(StartPage? startPage)
        {
            if (startPage == null)
            {
                return null;
            }

            var findPage = _contentLoader.GetChildren<FindPage>(startPage.ContentLink)
                .FirstOrDefault(page => _publishedStateAssessor.IsPublished(page, PublishedStateCondition.None));

            return findPage == null ? null : _urlResolver.GetUrl(findPage.ContentLink);
        }
    }
}
