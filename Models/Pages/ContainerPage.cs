using Optimizely26.Business;
using Optimizely26.Models.Interfaces;

namespace Optimizely26.Models.Pages
{
    [ContentType(
        GUID = "72728975-C7A6-45C1-A7C6-BD93C626E132",
        GroupName = Globals.GroupNames.Specialized,
        DisplayName = "Container Page",
        Description = "A page that serves as a container for other pages."
    )]
    [AvailableContentTypes(
        Availability.Specific,
        Include = new[] { typeof(CarouselPage) }
    )]
    [ImageUrl("")]
    public class ContainerPage : PageData, IContainerPage
    {
    }
}
