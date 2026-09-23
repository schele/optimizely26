using Optimizely26.Business;
using Optimizely26.Models.Pages;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Blocks
{
    [ContentType(
        DisplayName = "Carousel Block", 
        GUID = "9564157F-0485-46B1-A83C-A13A19555AD3",
        GroupName = Globals.GroupNames.Specialized,
        Description = "A block for displaying a carousel of images"
    )]
    public class CarouselBlock : BlockData
    {
        [Display(
            GroupName = SystemTabNames.Content,
            Order = 0
        )]
        [AllowedTypes(typeof(CarouselPage))]
        public virtual ContentArea CarouselContentArea { get; set; }
    }
}