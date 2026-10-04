using Optimizely26.Models.Omdb;

namespace Optimizely26.Services
{
	public class OmdbService(HttpClient httpClient, IConfiguration configuration, ILogger<OmdbService> logger) : IOmdbService
	{
		private readonly HttpClient _httpClient = httpClient;
		private readonly IConfiguration _configuration = configuration;
		private readonly ILogger<OmdbService> _logger = logger;

		public async Task<List<OmdbMovie>> SearchAsync(OmdbSearchModel search)
		{
			try
			{
				var result = await _httpClient.GetFromJsonAsync<OmdbSearchResponse>(ApiUrl($"s={Uri.EscapeDataString(search.Query)}"));

				if (result != null)
				{
					return result.Search;
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "OMDb search for {Query} failed", search.Query);
			}

			return [];
		}

		public async Task<OmdbMovieDetails?> GetByIdAsync(string imdbId)
		{
			try
			{
				var movie = await _httpClient.GetFromJsonAsync<OmdbMovieDetails>(ApiUrl($"i={Uri.EscapeDataString(imdbId)}&plot=full"));

				// OMDb answers unknown ids with 200 and Response "False"
				if (movie != null && movie.Response == "True")
				{
					return movie;
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "OMDb lookup of {ImdbId} failed", imdbId);
			}

			return null;
		}

		private string ApiUrl(string query) => $"https://www.omdbapi.com/?{query}&apikey={_configuration["Omdb:ApiKey"]}";
	}
}
