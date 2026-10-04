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
				var apiKey = _configuration["Omdb:ApiKey"];
				var url = $"https://www.omdbapi.com/?s={Uri.EscapeDataString(search.Query)}&apikey={apiKey}";
				var result = await _httpClient.GetFromJsonAsync<OmdbSearchResponse>(url);

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
	}
}
