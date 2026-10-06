using EPiServer.Applications;
using EPiServer.Filters;
using EPiServer.Globalization;
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
        private readonly IContentLanguageAccessor _contentLanguageAccessor;

        public PageViewContextFactory(IContentLoader contentLoader, IApplicationResolver applicationResolver, IPublishedStateAssessor publishedStateAssessor, IUrlResolver urlResolver, IContentLanguageAccessor contentLanguageAccessor)
        {
            _contentLoader = contentLoader;
            _applicationResolver = applicationResolver;
            _publishedStateAssessor = publishedStateAssessor;
            _urlResolver = urlResolver;
            _contentLanguageAccessor = contentLanguageAccessor;
        }

        public virtual LayoutModel GetLayoutModel(ContentReference contentReference, HttpContext httpContext)
        {
            var startPageContentLink = (_applicationResolver.GetByContext() as IRoutableApplication)?.EntryPoint ?? ContentReference.EmptyReference;

            if (contentReference.CompareToIgnoreWorkID(startPageContentLink))
            {
                startPageContentLink = contentReference;
            }

            var startPage = _contentLoader.Get<StartPage>(startPageContentLink);
            var settingsPage = GetSettingsPage(startPage);

            return new LayoutModel()
            {
                StartPage = startPage,
                SettingsPage = settingsPage,
                FindPageUrl = GetFindPageUrl(startPage),
                MenuItems = GetMenuItems(settingsPage, startPage, contentReference)
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

        /// <summary>
        /// The pages picked on the settings page, in the current language, without those the visitor can't open: not
        /// translated to or not published in this language, restricted, or without a template.
        /// </summary>
        private IReadOnlyList<MenuItem> GetMenuItems(SettingsPage? settingsPage, StartPage startPage, ContentReference currentContentLink)
        {
            if (settingsPage?.MenuItems is not { Count: > 0 } menuItems)
            {
                return [];
            }

            var language = _contentLanguageAccessor.Language;

            // Specific: a page that isn't translated to this language must not show up in the master language
            var loaderOptions = new LoaderOptions { LanguageLoaderOption.Specific(language) };

            // Published, readable by the current user, and with a template
            var visitorFilter = new FilterContentForVisitor();
            var currentSection = GetCurrentSection(currentContentLink, startPage);

            return menuItems
                .Select(contentLink => _contentLoader.TryGet<PageData>(contentLink, loaderOptions, out var page) ? page : null)
                .OfType<PageData>()
                .Where(page => !visitorFilter.ShouldFilter(page))
                .Select(page => new MenuItem(
                    page.Name,
                    _urlResolver.GetUrl(page.ContentLink, language.Name) ?? string.Empty,
                    currentSection.Contains(page.ContentLink.ID)))
                .ToList();
        }

        /// <summary>
        /// The IDs of the current page and its ancestors, where a menu item counts as current. The start page is every page's
        /// ancestor, so it is only current on itself.
        /// </summary>
        private HashSet<int> GetCurrentSection(ContentReference currentContentLink, StartPage startPage)
        {
            if (ContentReference.IsNullOrEmpty(currentContentLink))
            {
                return [];
            }

            return _contentLoader.GetAncestors(currentContentLink)
                .Select(ancestor => ancestor.ContentLink.ID)
                .Where(id => id != startPage.ContentLink.ID)
                .Append(currentContentLink.ID)
                .ToHashSet();
        }
    }
}
