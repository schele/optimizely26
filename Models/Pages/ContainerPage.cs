using Optimizely26.Business;
using Optimizely26.Models.Interfaces;

namespace Optimizely26.Models.Pages
{


	[ContentType(
		DisplayName = "Container Page", 
		GUID = "464F2705-6CDA-4E6E-B161-D6BAA97DFDF3", 
		GroupName = Globals.GroupNames.Specialized,
		Description = "A container page that can hold other pages"
	)]
	[AvailableContentTypes(
		Availability = Availability.Specific,
		Include = [typeof(CarouselPage)]
	)]
	[ImageUrl("pages/container.png")]
	public class ContainerPage : PageData, IContainerPage
	{
	}



}
