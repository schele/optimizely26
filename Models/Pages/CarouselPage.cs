using EPiServer.Web;
using Optimizely26.Business;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{
    [ContentType(
        DisplayName = "Carousel Page",
        GUID = "d2b8c8e4-8f4b-4c8b-9f8b-8f8b8f8b8f8b",
        Description = "A page that contains a carousel.",
        GroupName = Globals.GroupNames.Specialized
    )]
    public class CarouselPage : SitePageData
    {
        [Display(
            GroupName = SystemTabNames.Content,
            Name = "Carousel Page",
            Description = "The page to display in the carousel."
        )]
        [UIHint(UIHint.Image)]
        public virtual ContentReference Image { get; set; }
    }
}