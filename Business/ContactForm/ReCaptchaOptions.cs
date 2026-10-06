namespace Optimizely26.Business.ContactForm
{
	/// <summary>
	/// The <c>ReCaptcha</c> section of appsettings: the keys of a reCAPTCHA v3 site, from
	/// https://www.google.com/recaptcha/admin. Keep the secret key out of git: appsettings.Development.json is committed in
	/// this repo, so use user secrets (<c>dotnet user-secrets set ReCaptcha:SecretKey …</c>) or an environment variable
	/// (<c>ReCaptcha__SecretKey</c>).
	/// </summary>
	public class ReCaptchaOptions
	{
		public const string SectionName = "ReCaptcha";

		/// <summary>Public, rendered on the page.</summary>
		public string? SiteKey { get; set; }

		public string? SecretKey { get; set; }

		/// <summary>From 0.0 (a bot) to 1.0 (a person); posts scoring lower are refused. Google suggests starting at 0.5.</summary>
		public double MinimumScore { get; set; } = 0.5;
	}
}
