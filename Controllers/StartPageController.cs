using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Controllers
{
    public class StartPageController(IContentLoader contentLoader, IPublishedStateAssessor publishedStateAssessor, IUrlResolver urlResolver) : PageControllerBase<StartPage>
    {
        private readonly IContentLoader _contentLoader = contentLoader;
        private readonly IPublishedStateAssessor _publishedStateAssessor = publishedStateAssessor;
        private readonly IUrlResolver _urlResolver = urlResolver;

        public IActionResult Index(StartPage currentPage)
        {
            var model = new StartPageViewModel(currentPage)
            {
                MoviePageUrl = GetMoviePageUrl(currentPage)
            };

            return View(model);
        }

        /// <summary>The URL of the first published movie page under the start page, so visitors are never sent to a 404.</summary>
        private string? GetMoviePageUrl(StartPage startPage)
        {
            // TryGet, not Get: a page that doesn't exist in the current language is skipped instead of throwing
            var moviePage = _contentLoader.GetDescendents(startPage.ContentLink)
                .Select(contentLink => _contentLoader.TryGet<MoviePage>(contentLink, out var page) ? page : null)
                .FirstOrDefault(page => page != null && _publishedStateAssessor.IsPublished(page, PublishedStateCondition.None));

            return moviePage == null ? null : _urlResolver.GetUrl(moviePage.ContentLink);
        }
    }
}
