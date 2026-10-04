using Optimizely26.Models.Omdb;

namespace Optimizely26.Services
{
	public interface IOmdbService
	{
		Task<List<OmdbMovie>> SearchAsync(OmdbSearchModel search);

		/// <summary>The full details of one title, or null when OMDb doesn't know the id or can't be reached.</summary>
		Task<OmdbMovieDetails?> GetByIdAsync(string imdbId);
	}
}
