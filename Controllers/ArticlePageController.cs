using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Controllers
{
	public class ArticlePageController : PageControllerBase<ArticlePage>
	{
		public IActionResult Index(ArticlePage currentPage)
		{
			return View(PageViewModel.Create(currentPage));
		}
	}
}
