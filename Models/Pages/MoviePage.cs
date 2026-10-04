using Optimizely26.Business;

namespace Optimizely26.Models.Pages
{
	/// <summary>Shows one OMDb title, picked by <c>?id={imdbId}</c>, with visitor ratings.</summary>
	[ContentType(
		GUID = "6E2B7C1A-4F3D-4B8E-9C2A-7D5E1F3A9B64",
		GroupName = Globals.GroupNames.Specialized,
		DisplayName = "Movie Page",
		Description = "Shows one movie from OMDb, picked by ?id={imdbId}, with visitor ratings."
	)]
	public class MoviePage : SitePageData
	{
	}
}
