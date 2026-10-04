namespace Optimizely26.Models.Omdb
{
	// OMDb omits "Search" when nothing matches, so it defaults to an empty list.
	public class OmdbSearchResponse
	{
		public List<OmdbMovie> Search { get; set; } = [];
	}
}
