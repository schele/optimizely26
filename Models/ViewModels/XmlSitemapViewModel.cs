using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public class XmlSitemapViewModel : PageViewModel<XmlSitemap>
    {
        public XmlSitemapViewModel(XmlSitemap currentPage) : base(currentPage)
        {
        }

        public IEnumerable<SitePageData> Pages { get; set; }
    }
}