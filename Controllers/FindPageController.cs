using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Controllers
{
	public class FindPageController : PageControllerBase<FindPage>
	{
		/// <summary><paramref name="q"/> and <paramref name="page"/> come from the address bar, so a search can be linked to and reloaded.</summary>
		public IActionResult Index(FindPage currentPage, string? q, int page = 1)
		{
			var model = new FindPageViewModel(currentPage)
			{
				Query = q?.Trim() ?? string.Empty,
				Page = Math.Max(page, 1),
			};

			return View(model);
		}
	}
}
