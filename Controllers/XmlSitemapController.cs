using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;
using Optimizely26.Services;

namespace Optimizely26.Controllers
{
	
	public class XmlSitemapController(IXmlSitemapService sitemapService) : PageControllerBase<XmlSitemap>
	{
		private readonly IXmlSitemapService _sitemapService = sitemapService;

		public IActionResult Index(XmlSitemap currentPage)
		{
			var model = new XmlSitemapViewModel(currentPage)
			{
				Pages = _sitemapService.GetPages(currentPage)
			};

			return View(model);
		}
	}


}
