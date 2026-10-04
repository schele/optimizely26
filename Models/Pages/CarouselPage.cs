using EPiServer.Web;
using Optimizely26.Business;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{


	[ContentType(
        GUID = "8B00D88A-A7F9-4461-A574-BFFC6BFAD216",
        GroupName = Globals.GroupNames.Specialized,
		DisplayName = "Carousel Page",
		Description = "A page that contains a carousel of images or content."
	)]
	public class CarouselPage : SitePageData
	{
		[Display(
			GroupName = SystemTabNames.Content,
			Order = 40
		)]
		[UIHint(UIHint.Image)]
		public virtual ContentReference Image { get; set; }
	}


}
