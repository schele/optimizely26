using Optimizely26.Business;
using Optimizely26.Models.Pages;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Blocks
{


	[ContentType(
		GUID = "BC10B5E1-2D6A-48A0-92DB-C578901692D6",
		DisplayName = "Carousel Block",
		GroupName = Globals.GroupNames.Specialized,
		Description = "A block that contains a carousel of images or content."
	)]
	public class CarouselBlock : BlockData
	{
		[Display(			
			GroupName = SystemTabNames.Content,
			Order = 10
		)]
		[AllowedTypes(typeof(CarouselPage))]
		public virtual ContentArea? Carousel { get; set; }
	}



}