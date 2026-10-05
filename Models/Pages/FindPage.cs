using Optimizely26.Business;

namespace Optimizely26.Models.Pages
{
	/// <summary>The site search. One is created under the start page at startup, and the navbar links to it.</summary>
	[ContentType(
		GUID = "D82A2A78-CAE2-4FA4-A78C-3BD2D903A980",
		GroupName = Globals.GroupNames.Specialized,
		DisplayName = "Find Page",
		Description = "Searches the site's pages. Created automatically under the start page."
	)]
	public class FindPage : SitePageData
	{
	}
}
