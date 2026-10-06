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
        public virtual ContentReference? LinkToMovies { get; set; }

        /// <summary>
        /// The pages in the top menu, in this order. The same in every language; each link shows the page's name in the
        /// visitor's language, and pages the visitor can't open are left out.
        /// </summary>
        [Display(
            Name = "Menu items",
            Description = "The pages in the menu at the top of every page, in this order.",
            GroupName = Globals.GroupNames.Menu,
            Order = 10
        )]
        [AllowedTypes(typeof(PageData))]
        public virtual IList<ContentReference>? MenuItems { get; set; }
    }
}