using EPiServer.Web;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;
using Optimizely26.Services;

namespace Optimizely26.Controllers
{
	public class MoviePageController(IOmdbService omdbService, IContextModeResolver contextModeResolver) : PageControllerBase<MoviePage>
	{
		private readonly IOmdbService _omdbService = omdbService;
		private readonly IContextModeResolver _contextModeResolver = contextModeResolver;

		/// <summary>The title comes from <c>?id={imdbId}</c>.</summary>
		public async Task<IActionResult> Index(MoviePage currentPage, string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				// Editors open the page without an id; show them a note instead of the error page
				if (_contextModeResolver.CurrentMode is ContextMode.Edit or ContextMode.Preview)
				{
					return View(new MoviePageViewModel(currentPage, null));
				}

				return NotFound();
			}

			var movie = await _omdbService.GetByIdAsync(id);

			if (movie == null)
			{
				return NotFound();
			}

			return View(new MoviePageViewModel(currentPage, movie));
		}
	}
}
