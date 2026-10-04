using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{

	public class CarouselViewModel
	{
		public string Id { get; set; } = $"carousel-{Guid.NewGuid():N}";

		public List<CarouselPage> Pages { get; set; } = [];
	}


}
