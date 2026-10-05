using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Optimizely26.Business.ContactForm
{
	public enum ContactFormTokenResult
	{
		Valid,

		/// <summary>Tampered with, expired, for another page, or not there at all.</summary>
		Invalid,

		/// <summary>Posted sooner after the form was shown than a person can fill it in.</summary>
		TooFast,
	}

	/// <summary>
	/// The signed token in every contact form. It proves the form was rendered by this site, for this page, less than
	/// <see cref="Lifetime"/> ago. Data Protection encrypts it and signs it with an HMAC, so it can't be forged or altered.
	/// </summary>
	public class ContactFormToken(IDataProtectionProvider dataProtectionProvider)
	{
		/// <summary>Long enough for a visitor who leaves the tab open over lunch; a replay within it is no worse than a new page load.</summary>
		public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

		/// <summary>People need longer than this to fill in the form; a faster post comes from a bot.</summary>
		public static readonly TimeSpan MinimumFillTime = TimeSpan.FromSeconds(3);

		private readonly ITimeLimitedDataProtector _protector = dataProtectionProvider
			.CreateProtector("Optimizely26.ContactForm.Token")
			.ToTimeLimitedDataProtector();

		public string Create(int pageId, string language)
		{
			var payload = string.Join('|', pageId.ToString(CultureInfo.InvariantCulture), language, DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

			return _protector.Protect(payload, Lifetime);
		}

		public ContactFormTokenResult Validate(string? token, int pageId, string language)
		{
			if (string.IsNullOrEmpty(token))
			{
				return ContactFormTokenResult.Invalid;
			}

			string payload;

			try
			{
				payload = _protector.Unprotect(token);
			}
			catch (Exception e) when (e is CryptographicException or FormatException)
			{
				// Tampered with, expired, or protected with a key that's gone
				return ContactFormTokenResult.Invalid;
			}

			var parts = payload.Split('|');

			if (parts.Length != 3
				|| parts[0] != pageId.ToString(CultureInfo.InvariantCulture)
				|| !string.Equals(parts[1], language, StringComparison.OrdinalIgnoreCase)
				|| !long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var issuedTicks))
			{
				return ContactFormTokenResult.Invalid;
			}

			return DateTime.UtcNow.Ticks - issuedTicks < MinimumFillTime.Ticks ? ContactFormTokenResult.TooFast : ContactFormTokenResult.Valid;
		}
	}
}
