namespace Optimizely26.Models.ViewModels
{
	public class ContactFormViewModel
	{
		public string? Heading { get; init; }

		public string? Intro { get; init; }

		/// <summary>The page the form is on; 0 when the block is shown outside a page, and the form can't be posted.</summary>
		public int PageId { get; init; }

		public string Language { get; init; } = string.Empty;

		/// <summary>The signed form token; null together with <see cref="PageId"/> 0.</summary>
		public string? FormToken { get; init; }

		/// <summary>The public reCAPTCHA v3 key; null when reCAPTCHA isn't configured, and the form is sent without a token.</summary>
		public string? ReCaptchaSiteKey { get; init; }

		/// <summary>The outcome of the last post, from <c>?contact=</c>: sent, invalid, expired or unverified.</summary>
		public string? Status { get; init; }

		/// <summary>What the visitor typed before an invalid or expired post, to fill the form in again.</summary>
		public string? Name { get; init; }

		public string? Email { get; init; }

		public string? Comment { get; init; }
	}
}
