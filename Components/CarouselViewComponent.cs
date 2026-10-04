using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Interfaces;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Components
{

	public class CarouselViewComponent : ViewComponent
	{
		private readonly IContentLoader _contentLoader;

		public CarouselViewComponent(IContentLoader contentLoader)
		{
			_contentLoader = contentLoader;
		}

		public IViewComponentResult Invoke()
		{
			var model = new CarouselViewModel();
			var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);

			model.Pages.AddRange(GetCarouselPages(startPage));

			return View("~/Views/Shared/carousel.cshtml", model);
		}

		private IEnumerable<CarouselPage> GetCarouselPages(StartPage startPage)
		{
			// Carousel pages live under a container page beneath the start page.
			var container = _contentLoader
				.GetChildren<PageData>(startPage.ContentLink)
				.FirstOrDefault(x => x is IContainerPage);

			if (container != null)
			{
				return _contentLoader.GetChildren<CarouselPage>(container.ContentLink);
			}

			// Fall back to any carousel pages placed directly in the start page content area.
			if (startPage.Carousel != null)
			{
				return startPage.Carousel.Items
					.Select(x => x.LoadContent())
					.OfType<CarouselPage>();
			}

			return [];
		}
	}


}
