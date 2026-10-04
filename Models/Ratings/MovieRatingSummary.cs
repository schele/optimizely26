namespace Optimizely26.Models.Ratings
{
	/// <summary>The MovieFinder rating of a title: the average score and one page of ratings, newest first.</summary>
	public class MovieRatingSummary
	{
		public double? Average { get; init; }

		/// <summary>The number of ratings in total, across all pages.</summary>
		public int Count { get; init; }

		public List<MovieRating> Ratings { get; init; } = [];
	}
}
