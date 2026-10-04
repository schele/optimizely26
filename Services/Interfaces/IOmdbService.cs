using Optimizely26.Models.Omdb;

namespace Optimizely26.Services
{
	public interface IOmdbService
	{
		Task<List<OmdbMovie>> SearchAsync(OmdbSearchModel search);
	}
}
