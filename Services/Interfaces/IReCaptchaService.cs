namespace Optimizely26.Services
{
	public enum ReCaptchaStatus
	{
		Passed,

		/// <summary>No token, an invalid, expired or reused one, another action's, or a score below the minimum.</summary>
		Failed,

		/// <summary>Google couldn't be asked, or turned down our secret key; the post isn't held against the visitor.</summary>
		Unavailable,
	}

	/// <summary>
	/// Invisible reCAPTCHA v3: the page gets a token from Google when the form is sent, and the server asks Google
	/// how likely it is that a person sent it. Visitors never see a challenge.
	/// </summary>
	public interface IReCaptchaService
	{
		/// <summary>The action the page asks a token for and the server expects back.</summary>
		const string ContactAction = "contact";

		/// <summary>Whether both keys are configured; without them the form works as if there were no reCAPTCHA.</summary>
		bool IsEnabled { get; }

		string? SiteKey { get; }

		Task<ReCaptchaStatus> VerifyAsync(string? token, string expectedAction, CancellationToken cancellationToken = default);
	}
}
