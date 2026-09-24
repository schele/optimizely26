using EPiServer.Web;
using Optimizely26.Business;
using Optimizely26.Models.Blocks;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{
    [ContentType(
        GUID = "ED11646D-BDB3-4AEE-860F-EA2D8489CA8E",
        GroupName = Globals.GroupNames.Specialized
    )]
    [ImageUrl("/pages/CMS-icon-page-02.png")]
    [AvailableContentTypes(
        Availability.Specific,
        Include = new[] {
            typeof(SettingsPage),
            typeof(ContainerPage),
            typeof(ErrorPage),
            typeof(XmlSitemap)
        }
    )]
    public class StartPage : SitePageData
    {
        [Display(
            GroupName = SystemTabNames.Content,
            Order = 10
        )]
        [CultureSpecific]
        public virtual string Title { get; set; } = string.Empty;

        [Display(
            GroupName = SystemTabNames.Content,
            Order = 20
        )]
        [CultureSpecific]
        [UIHint(UIHint.Textarea)]
        public virtual string Preamble { get; set; } = string.Empty;

        [Display(
            GroupName = SystemTabNames.Content,
            Order = 30
        )]
        [CultureSpecific]
        [ScaffoldColumn(false)]
        public virtual XhtmlString MainBody { get; set; }

        [Display(
            GroupName = SystemTabNames.Content,
            Order = 40
        )]
        [UIHint(UIHint.Image)]
        public virtual ContentReference Image { get; set; }

        [Display(
            GroupName = SystemTabNames.Content,
            Order = 0
        )]
        [CultureSpecific]
        [AllowedTypes(
            typeof(CarouselBlock)
        )]
        public virtual ContentArea Carousel { get; set; }
    }
}