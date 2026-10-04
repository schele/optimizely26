using Optimizely26.Models.Ratings;

namespace Optimizely26.Services
{
	public interface IMovieRatingService
	{
		Task<MovieRatingSummary> GetSummaryAsync(string imdbId, int page, int pageSize);

		Task<MovieRating> AddAsync(string imdbId, double score, string? name, string? comment);
	}
}
