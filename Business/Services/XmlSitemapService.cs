using Optimizely26.Business.Extensions;
using Optimizely26.Business.Services.Interfaces;
using Optimizely26.Models.Pages;

namespace Optimizely26.Business.Services
{
    public class XmlSitemapService(IContentLoader contentLoader) : IXmlSitemapService
    {
        private readonly IContentLoader _contentLoader = contentLoader;

        public IEnumerable<SitePageData> GetPages(XmlSitemap currentPage)
        {
            var startPage = _contentLoader.GetAncestors(currentPage.ContentLink).
                FirstOrDefault(x => x is StartPage) as PageData;

            var descendants = Enumerable.Empty<SitePageData>();

            if (startPage != null)
            {
                foreach (var page in _contentLoader.GetDescendantsAndSelf(startPage.ContentLink))
                {
                    yield return page;
                }
            }
        }
    }
}