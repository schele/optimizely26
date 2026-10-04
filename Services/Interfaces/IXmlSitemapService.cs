using Optimizely26.Models.Pages;

namespace Optimizely26.Services
{

	public interface IXmlSitemapService
	{
		IEnumerable<SitePageData> GetPages(XmlSitemap currentPage);
	}


}