using Optimizely26.Business;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{
    [ContentType(
        GUID = "ED11646D-BDB3-4AEE-860F-EA2D8489CA8E",
        GroupName = Globals.GroupNames.Specialized
    )]
    [ImageUrl("/pages/CMS-icon-page-02.png")]
    public class StartPage : SitePageData
    {
        [Display(
            GroupName = SystemTabNames.Content,
            Order = 10
        )]
        [CultureSpecific]
        public virtual string Title { get; set; } = string.Empty;
    }
}