using EPiServer.Data.Dynamic;
using Optimizely26.Models.Ratings;

namespace Optimizely26.Services
{
	/// <remarks>
	/// The Dynamic Data Store has no async API, so these run synchronously behind the Task-based interface.
	/// A store instance isn't thread-safe, so every call gets its own.
	/// </remarks>
	public class MovieRatingService(DynamicDataStoreFactory dataStoreFactory) : IMovieRatingService
	{
		private readonly DynamicDataStoreFactory _dataStoreFactory = dataStoreFactory;

		public Task<MovieRatingSummary> GetSummaryAsync(string imdbId, int page, int pageSize)
		{
			var ratings = GetStore().Items<MovieRating>().Where(x => x.ImdbId == imdbId);

			// Averaged in memory, like umbraco26, so rounding is the same
			var scores = ratings.Select(x => x.Score).ToList();
			var pageRatings = ratings
				.OrderByDescending(x => x.CreatedUtc)
				.Skip((Math.Max(page, 1) - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			return Task.FromResult(new MovieRatingSummary
			{
				Average = scores.Count > 0 ? Math.Round(scores.Average(), 1) : null,
				Count = scores.Count,
				Ratings = pageRatings,
			});
		}

		public Task<MovieRating> AddAsync(string imdbId, double score, string? name, string? comment)
		{
			if (string.IsNullOrWhiteSpace(imdbId))
			{
				throw new ArgumentException("An IMDb id is required.", nameof(imdbId));
			}

			// NaN or infinity would survive Math.Clamp and be stored as is
			if (!double.IsFinite(score))
			{
				throw new ArgumentOutOfRangeException(nameof(score), score, "The score must be a number.");
			}

			// Snap to half stars within range, whatever the client sent
			score = Math.Clamp(Math.Round(score * 2, MidpointRounding.AwayFromZero) / 2, MovieRating.MinScore, MovieRating.MaxScore);

			var rating = new MovieRating
			{
				ImdbId = imdbId.Trim(),
				Score = score,
				Name = Truncate(name, MovieRating.NameMaxLength),
				Comment = Truncate(comment, MovieRating.CommentMaxLength),
				CreatedUtc = DateTime.UtcNow,
			};

			GetStore().Save(rating);

			return Task.FromResult(rating);
		}

		private DynamicDataStore GetStore()
			=> _dataStoreFactory.GetStore(typeof(MovieRating)) ?? _dataStoreFactory.CreateStore(typeof(MovieRating));

		private static string? Truncate(string? value, int maxLength)
		{
			value = value?.Trim();

			if (string.IsNullOrEmpty(value))
			{
				return null;
			}

			return value.Length > maxLength ? value[..maxLength] : value;
		}
	}
}
