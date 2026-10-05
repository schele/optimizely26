using EPiServer.Web.Mvc;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Business.ContactForm;
using Optimizely26.Models.Blocks;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Blocks.ContactForm
{
	public class ContactFormBlockComponent(IContentLoader contentLoader, ContactFormToken formToken) : AsyncBlockComponent<ContactFormBlock>
	{
		private readonly IContentLoader _contentLoader = contentLoader;
		private readonly ContactFormToken _formToken = formToken;

		protected override async Task<IViewComponentResult> InvokeComponentAsync(ContactFormBlock currentContent)
		{
			// The page the block is shown on, in the language the visitor is reading
			var pageLink = HttpContext.GetContentLink();
			SitePageData? page = null;

			if (!ContentReference.IsNullOrEmpty(pageLink))
			{
				_contentLoader.TryGet(pageLink, out page);
			}

			var model = new ContactFormViewModel
			{
				Heading = currentContent.Heading,
				Intro = currentContent.Intro,
				PageId = page?.ContentLink.ID ?? 0,
				Language = page?.Language.Name ?? string.Empty,
				FormToken = page == null ? null : _formToken.Create(page.ContentLink.ID, page.Language.Name),
				Status = HttpContext.Request.Query["contact"].ToString(),
			};

			return await Task.FromResult(View("~/Views/Shared/ContactForm.cshtml", model));
		}
	}
}
