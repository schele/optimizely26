using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
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
		IUrlResolver urlResolver,
		ILanguageBranchRepository languageBranchRepository,
		ILogger<ContactFormController> logger) : Controller
	{
		private const string Sent = "sent";
		private const string Invalid = "invalid";
		private const string Expired = "expired";

		private readonly IAntiforgery _antiforgery = antiforgery;
		private readonly ContactFormToken _formToken = formToken;
		private readonly IContactSubmissionService _submissions = submissions;
		private readonly IContentLoader _contentLoader = contentLoader;
		private readonly IUrlResolver _urlResolver = urlResolver;
		private readonly ILanguageBranchRepository _languageBranchRepository = languageBranchRepository;
		private readonly ILogger<ContactFormController> _logger = logger;

		[HttpPost("submit")]
		[IgnoreAntiforgeryToken] // Validated below, so a failure sends the visitor back instead of answering 400
		public async Task<IActionResult> Submit(ContactFormPost post)
		{
			if (!await _antiforgery.IsRequestValidAsync(HttpContext))
			{
				return BackToPage(post, Expired);
			}

			if (!string.IsNullOrEmpty(post.Website))
			{
				_logger.LogInformation("Contact form on page {PageId}: the honeypot was filled in; nothing stored", post.PageId);
				return BackToPage(post, Sent);
			}

			switch (_formToken.Validate(post.FormToken, post.PageId, post.Language))
			{
				case ContactFormTokenResult.Invalid:
					return BackToPage(post, Expired);

				case ContactFormTokenResult.TooFast:
					_logger.LogInformation("Contact form on page {PageId}: posted faster than a person can type; nothing stored", post.PageId);
					return BackToPage(post, Sent);
			}

			if (!ModelState.IsValid)
			{
				return BackToPage(post, Invalid);
			}

			_submissions.Add(post.Name!.Trim(), post.Email!.Trim(), post.Comment!.Trim(), post.PageId, post.Language);

			return BackToPage(post, Sent);
		}

		/// <summary>
		/// Back to the form's page. The URL is resolved from the page id, never taken from the request, so this can't
		/// redirect anywhere outside the site.
		/// </summary>
		private RedirectResult BackToPage(ContactFormPost post, string status)
		{
			var isKnownLanguage = _languageBranchRepository.ListEnabled()
				.Any(language => string.Equals(language.LanguageID, post.Language, StringComparison.OrdinalIgnoreCase));

			// Only a site page: a forged id must not send the visitor to, say, a CMS system node
			var isSitePage = post.PageId > 0 && _contentLoader.TryGet<SitePageData>(new ContentReference(post.PageId), out _);

			var url = isSitePage && isKnownLanguage
				? _urlResolver.GetUrl(new ContentReference(post.PageId), post.Language)
				: null;

			return Redirect($"{(string.IsNullOrEmpty(url) ? "/" : url)}?contact={status}#contact-form");
		}
	}
}
