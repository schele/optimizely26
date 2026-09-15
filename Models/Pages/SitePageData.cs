using Optimizely26.Business;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{
    public class SitePageData : PageData
    {
        [Display(
            GroupName = Globals.GroupNames.Metadata,
            Order = 100
        )]
        [CultureSpecific]
        public virtual string MetaDescription
        {
            get
            {
                var metaDescription = this.GetPropertyValue(p => p.MetaDescription);

                return !string.IsNullOrEmpty(metaDescription) ? metaDescription : this.GetPropertyValue(p => p.PageName);
            }
            set => this.SetPropertyValue(p => p.MetaDescription, value);
        }
    }
}