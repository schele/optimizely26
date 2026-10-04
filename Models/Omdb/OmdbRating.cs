namespace Optimizely26.Models.Omdb
{
	/// <summary>One of the outside ratings OMDb lists for a title, e.g. Rotten Tomatoes "91%".</summary>
	public class OmdbRating
	{
		public string Source { get; set; } = string.Empty;

		public string Value { get; set; } = string.Empty;
	}
}
