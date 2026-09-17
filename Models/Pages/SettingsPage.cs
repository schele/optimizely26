using EPiServer.Web;
using Optimizely26.Business;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{
    [ContentType(
        GUID = "311C16B9-12FF-4BE4-90F2-DAF916EF4B56",
        GroupName = Globals.GroupNames.Specialized
    )]
    [ImageUrl("/pages/CMS-icon-page-16.png")]
    public class SettingsPage : SitePageData
    {
        [Display(
            GroupName = SystemTabNames.Content,
            Order = 40
        )]
        [UIHint(UIHint.Image)]
        public virtual PageReference LinkToMovies { get; set; }
    }
}