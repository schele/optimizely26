using Microsoft.AspNetCore.Mvc;
using Optimizely26.Business.Services.Interfaces;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Controllers
{
    public class XmlSitemapController(IXmlSitemapService sitemapService) : PageControllerBase<XmlSitemap>
    {
        private readonly IXmlSitemapService _sitemapService = sitemapService;

        public IActionResult Index (XmlSitemap currentPage)
        {
            var viewModel = new XmlSitemapViewModel(currentPage)
            {
                Pages = _sitemapService.GetPages(currentPage)
            };

            return View(viewModel);
        }
    }
}