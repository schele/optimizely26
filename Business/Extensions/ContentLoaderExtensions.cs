using Optimizely26.Models.Pages;

namespace Optimizely26.Business.Extensions
{
    public static class ContentLoaderExtensions
    {
        public static IEnumerable<SitePageData> GetDescendantsAndSelf(this IContentLoader contentLoader, ContentReference contentReference)
        {
            var startPage = contentLoader.Get<SitePageData>(contentReference);
            var descendants = contentLoader.GetDescendents(contentReference)
                .Select(contentLoader.Get<IContent>)
                .Where(content => content is SitePageData and not XmlSitemap and not ErrorPage)
                .Cast<SitePageData>();

            return new[] { startPage }.Concat(descendants);
        }
    }
}