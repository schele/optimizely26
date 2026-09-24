using Optimizely26.Models.Pages;

namespace Optimizely26.Business.Services.Interfaces
{
    public interface IXmlSitemapService
    {
        IEnumerable<SitePageData> GetPages(XmlSitemap currentPage);
    }
}