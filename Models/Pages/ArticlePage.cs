using Optimizely26.Business;
using Optimizely26.Models.Blocks;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Pages
{
	/// <summary>A page with body text; the site search finds it by its name, meta description and body.</summary>
	[ContentType(
		GUID = "909B9E09-E992-4AD2-8003-D3972F8B02DE",
		GroupName = Globals.GroupNames.Specialized,
		DisplayName = "Article Page",
		Description = "A page with a heading and body text."
	)]
	[ImageUrl("/pages/CMS-icon-page-03.png")]
	[AvailableContentTypes(
		Availability.Specific,
		Include = new[] { typeof(ArticlePage) }
	)]
	public class ArticlePage : SitePageData
	{
		[Display(
			GroupName = SystemTabNames.Content,
			Order = 10
		)]
		[CultureSpecific]
		public virtual XhtmlString? MainBody { get; set; }

		/// <summary>Blocks below the body, such as a contact form. Shared by all languages; the blocks have their own translations.</summary>
		[Display(
			GroupName = SystemTabNames.Content,
			Order = 20
		)]
		[AllowedTypes(typeof(ContactFormBlock))]
		public virtual ContentArea? MainContentArea { get; set; }
	}
}
