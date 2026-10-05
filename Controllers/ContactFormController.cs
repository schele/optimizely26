using System.Net.Mail;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Business;
using Optimizely26.Business.ContactForm;
using Optimizely26.Models.Contact;
using Optimizely26.Models.Pages;
using Optimizely26.Services;

namespace Optimizely26.Controllers
{
	/// <summary>
	/// Receives the contact form and sends the visitor back to the page (Post/Redirect/Get) with <c>?contact=</c>
	/// sent, invalid or expired. Bots get "sent" too, so they can't tell they were caught.
	/// </summary>
	[Route("contact-form")]
	public class ContactFormController(
		IAntiforgery antiforgery,
		ContactFormToken formToken,
		IContactSubmissionService submissions,
		IContentLoader contentLoader,
		VisitorAccess visitorAccess,
		IUrlResolver urlResolver,
		ILanguageBranchRepository languageBranchRepository,
		ILogger<ContactFormController> logger) : Controller
	{
		/// <summary>TempData keys for what the visitor typed, so a form that comes back invalid or expired is still filled in.</summary>
		public const string NameKey = "ContactForm.Name";
		public const string EmailKey = "ContactForm.Email";
		public const string CommentKey = "ContactForm.Comment";

		private const string Sent = "sent";
		private const string Invalid = "invalid";
		private const string Expired = "expired";

		private readonly IAntiforgery _antiforgery = antiforgery;
		private readonly ContactFormToken _formToken = formToken;
		private readonly IContactSubmissionService _submissions = submissions;
		private readonly IContentLoader _contentLoader = contentLoader;
		private readonly VisitorAccess _visitorAccess = visitorAccess;
		private readonly IUrlResolver _urlResolver = urlResolver;
		private readonly ILanguageBranchRepository _languageBranchRepository = languageBranchRepository;
		private readonly ILogger<ContactFormController> _logger = logger;

		[HttpPost("submit")]
		[IgnoreAntiforgeryToken] // Validated below, so a failure sends the visitor back instead of answering 400
		[RequestSizeLimit(64 * 1024)] // Far more than three fields need; nothing bigger is read into memory
		public async Task<IActionResult> Submit(ContactFormPost post)
		{
			if (!await _antiforgery.IsRequestValidAsync(HttpContext))
			{
				return BackToPage(post, Expired, keepInput: true);
			}

			// Warning, not Information: an autofill extension filling the honeypot would drop a real message, and that must show up
			if (!string.IsNullOrEmpty(post.Website))
			{
				_logger.LogWarning("Contact form on page {PageId}: the honeypot was filled in; nothing stored", post.PageId);
				return BackToPage(post, Sent);
			}

			switch (_formToken.Validate(post.FormToken, post.PageId, post.Language))
			{
				case ContactFormTokenResult.Invalid:
					return BackToPage(post, Expired, keepInput: true);

				case ContactFormTokenResult.TooFast:
					_logger.LogWarning("Contact form on page {PageId}: posted faster than a person can type; nothing stored", post.PageId);
					return BackToPage(post, Sent);
			}

			// Browsers count a line break as one character for maxlength but send it as two (CRLF)
			var comment = post.Comment?.ReplaceLineEndings("\n").Trim();

			if (!ModelState.IsValid || !IsPlainEmailAddress(post.Email!.Trim()) || comment is null || comment.Length > ContactSubmission.CommentMaxLength)
			{
				return BackToPage(post, Invalid, keepInput: true);
			}

			_submissions.Add(post.Name!.Trim(), post.Email!.Trim(), comment, post.PageId, post.Language);

			return BackToPage(post, Sent);
		}

		/// <summary>
		/// A single address and nothing else. [EmailAddress] only checks for one @, so it lets through things like
		/// <c>a%40b.se?cc=c@d.se</c>, which would add hidden recipients to the admin page's mailto link.
		/// </summary>
		private static bool IsPlainEmailAddress(string email)
			=> MailAddress.TryCreate(email, out var address)
				&& address.Address == email
				&& email.IndexOfAny(['?', '&', '%', '/', ':']) < 0;

		/// <summary>
		/// Back to the form's page. The URL is resolved from the page id, never taken from the request, so this can't redirect
		/// off the site, and only to a page anonymous visitors may see, so forged ids can't reveal the URLs of drafts or
		/// restricted pages.
		/// </summary>
		private RedirectResult BackToPage(ContactFormPost post, string status, bool keepInput = false)
		{
			if (keepInput)
			{
				TempData[NameKey] = post.Name;
				TempData[EmailKey] = post.Email;
				TempData[CommentKey] = post.Comment;
			}

			var isKnownLanguage = _languageBranchRepository.ListEnabled()
				.Any(language => string.Equals(language.LanguageID, post.Language, StringComparison.OrdinalIgnoreCase));

			var isVisiblePage = post.PageId > 0
				&& _contentLoader.TryGet<SitePageData>(new ContentReference(post.PageId), out var page)
				&& _visitorAccess.CanSee(page);

			var url = isVisiblePage && isKnownLanguage
				? _urlResolver.GetUrl(new ContentReference(post.PageId), post.Language)
				: null;

			return Redirect($"{(string.IsNullOrEmpty(url) ? "/" : url)}?contact={status}#contact-form");
		}
	}
}
