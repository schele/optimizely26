using EPiServer.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Services;

namespace Optimizely26.Controllers
{
	/// <summary>The admin page that lists contact form submissions. Linked from the CMS menu by <c>ContactSubmissionsMenuProvider</c>.</summary>
	[Authorize(Policy = CmsPolicyNames.CmsAdmin)]
	[Route(Path)]
	public class ContactSubmissionsController(IContactSubmissionService submissions) : Controller
	{
		public const string Path = "contact-submissions";

		private const int PageSize = 20;

		private readonly IContactSubmissionService _submissions = submissions;

		[HttpGet("")]
		public IActionResult Index(int page = 1)
		{
			return View(_submissions.GetPage(page, PageSize));
		}
	}
}
