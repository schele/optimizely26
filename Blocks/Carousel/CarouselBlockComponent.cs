using EPiServer.Web.Mvc;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Blocks;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Blocks.Carousel
{
	public class CarouselBlockComponent : AsyncBlockComponent<CarouselBlock>
	{
		protected override async Task<IViewComponentResult> InvokeComponentAsync(CarouselBlock currentContent)
		{
			var model = new CarouselViewModel();

			if (currentContent.Carousel != null)
			{
				foreach (var item in currentContent.Carousel.Items.Select(x => x.LoadContent()))
				{
					if (item is CarouselPage carouselPage)
					{
						model.Pages.Add(carouselPage);
					}
				}
			}
			
			return await Task.FromResult(View("~/Views/Shared/carousel.cshtml", model));
		}
	}
}
