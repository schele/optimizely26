using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Components.Carousel
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
            var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
            var model = new CarouselViewModel();

            if (startPage != null)
            {
                foreach (var items in startPage.Carousel.Items.Select(x => x.LoadContent()))
                {
                    if (items is CarouselPage carouselPage)
                    {
                        model.Pages.Add(carouselPage);
                    }
                }

            }

            return View("~/views/shared/carousel.cshtml", model);
        }
    }
}