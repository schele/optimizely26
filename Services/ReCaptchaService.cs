using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Optimizely26.Business.ContactForm;

namespace Optimizely26.Services
{
	/// <summary>
	/// Checks reCAPTCHA v3 tokens with Google's siteverify endpoint. The visitor's IP address isn't sent along,
	/// as Google doesn't need it to score the token.
	/// </summary>
	public class ReCaptchaService(HttpClient httpClient, IOptions<ReCaptchaOptions> options, ILogger<ReCaptchaService> logger)
		: IReCaptchaService
	{
		public const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

		/// <summary>Error codes that mean our configuration is wrong rather than the visitor's token.</summary>
		private static readonly string[] SecretErrorCodes = ["missing-input-secret", "invalid-input-secret"];

		private readonly HttpClient _httpClient = httpClient;
		private readonly ReCaptchaOptions _options = options.Value;
		private readonly ILogger<ReCaptchaService> _logger = logger;

		public bool IsEnabled => !string.IsNullOrWhiteSpace(_options.SiteKey) && !string.IsNullOrWhiteSpace(_options.SecretKey);

		public string? SiteKey => _options.SiteKey;

		public async Task<ReCaptchaStatus> VerifyAsync(string? token, string expectedAction, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(token))
			{
				// A bot that skips the script, or a visitor whose browser blocked Google
				return ReCaptchaStatus.Failed;
			}

			SiteVerifyResponse? result;

			try
			{
				using var content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					["secret"] = _options.SecretKey ?? string.Empty,
					["response"] = token,
				});
				using var response = await _httpClient.PostAsync(VerifyUrl, content, cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogWarning("reCAPTCHA verification answered {StatusCode}; the post was let through.", (int)response.StatusCode);
					return ReCaptchaStatus.Unavailable;
				}

				result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken);
			}
			catch (Exception ex) when (ex is HttpRequestException or JsonException
				|| (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
			{
				_logger.LogWarning(ex, "reCAPTCHA verification failed or timed out; the post was let through.");
				return ReCaptchaStatus.Unavailable;
			}

			if (result == null)
			{
				_logger.LogWarning("reCAPTCHA verification gave an empty answer; the post was let through.");
				return ReCaptchaStatus.Unavailable;
			}

			if (!result.Success)
			{
				var errorCodes = result.ErrorCodes ?? [];

				if (errorCodes.Any(SecretErrorCodes.Contains))
				{
					_logger.LogError("reCAPTCHA turned down the secret key ({ErrorCodes}); check ReCaptcha:SecretKey. The post was let through.", errorCodes);
					return ReCaptchaStatus.Unavailable;
				}

				_logger.LogInformation("reCAPTCHA refused the token ({ErrorCodes}).", errorCodes);
				return ReCaptchaStatus.Failed;
			}

			if (!string.Equals(result.Action, expectedAction, StringComparison.Ordinal))
			{
				_logger.LogInformation("reCAPTCHA token was for action {Action}, not {ExpectedAction}.", result.Action, expectedAction);
				return ReCaptchaStatus.Failed;
			}

			if (result.Score is not double score || score < _options.MinimumScore)
			{
				_logger.LogInformation("reCAPTCHA scored the post {Score}, below the minimum of {MinimumScore}.", result.Score, _options.MinimumScore);
				return ReCaptchaStatus.Failed;
			}

			return ReCaptchaStatus.Passed;
		}

		private sealed record SiteVerifyResponse(
			[property: JsonPropertyName("success")] bool Success,
			[property: JsonPropertyName("score")] double? Score,
			[property: JsonPropertyName("action")] string? Action,
			[property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
	}
}
