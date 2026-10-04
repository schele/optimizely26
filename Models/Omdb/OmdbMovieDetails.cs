namespace Optimizely26.Models.Omdb
{
	/// <summary>A single title from the OMDb API (<c>?i={imdbId}</c>). OMDb uses "N/A" for missing values.</summary>
	public class OmdbMovieDetails
	{
		public string Title { get; set; } = string.Empty;

		public string Year { get; set; } = string.Empty;

		public string Rated { get; set; } = string.Empty;

		public string Released { get; set; } = string.Empty;

		public string Runtime { get; set; } = string.Empty;

		public string Genre { get; set; } = string.Empty;

		public string Director { get; set; } = string.Empty;

		public string Writer { get; set; } = string.Empty;

		public string Actors { get; set; } = string.Empty;

		public string Plot { get; set; } = string.Empty;

		public string Language { get; set; } = string.Empty;

		public string Country { get; set; } = string.Empty;

		public string Awards { get; set; } = string.Empty;

		public string Poster { get; set; } = string.Empty;

		public List<OmdbRating> Ratings { get; set; } = [];

		public string Metascore { get; set; } = string.Empty;

		public string ImdbRating { get; set; } = string.Empty;

		public string ImdbVotes { get; set; } = string.Empty;

		public string ImdbID { get; set; } = string.Empty;

		public string Type { get; set; } = string.Empty;

		public string BoxOffice { get; set; } = string.Empty;

		/// <summary>"True", or "False" when OMDb doesn't know the id.</summary>
		public string Response { get; set; } = string.Empty;
	}
}
