using System.Globalization;
using EPiServer.Data.Dynamic;
using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Optimizely26.Business;
using Optimizely26.Models.Blocks;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
	/// <summary>
	/// Creates and publishes the contact page: an article ("Kontakt", "Contact") under the start page with a contact form block
	/// in its "For this page" folder, in every language the start page has. Once per database (a <c>SetupMarker</c>), and only
	/// when no article under the start page, or in the trash, already holds a contact form block.
	/// </summary>
	/// <remarks>Runs after <see cref="SampleArticlesInitialization"/>, which only seeds a site without articles.</remarks>
	[InitializableModule]
	[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
	[ModuleDependency(typeof(SampleArticlesInitialization))]
	public class ContactPageInitialization : IInitializableModule
	{
		private const string MarkerName = "contact-page";

		private record Texts(string PageName, string MetaDescription, string MainBody, string Heading, string Intro);

		private static readonly Texts Swedish = new(
			"Kontakt",
			"Frågor om en film eller om webbplatsen? Skicka ett meddelande till oss.",
			"<p>Vi läser alla meddelanden och svarar så snart vi kan.</p>",
			"Kontakta oss",
			"Fyll i formuläret så hör vi av oss.");

		private static readonly Texts English = new(
			"Contact",
			"Questions about a movie or the site? Send us a message.",
			"<p>We read every message and reply as soon as we can.</p>",
			"Contact us",
			"Fill in the form and we'll get back to you.");

		public void Initialize(InitializationEngine context)
		{
			var contentRepository = context.Services.GetRequiredService<IContentRepository>();
			var contentAssetHelper = context.Services.GetRequiredService<ContentAssetHelper>();
			var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() };
			var startPage = contentRepository.GetChildren<StartPage>(ContentReference.RootPage, loaderOptions).FirstOrDefault();

			if (startPage == null)
			{
				// Nothing to serve yet; the contact page is created on a later startup.
				return;
			}

			// Once per database, so a contact page an editor deleted, even from the trash, doesn't come back on the next startup
			var setupMarkers = new SetupMarkers(context.Services.GetRequiredService<DynamicDataStoreFactory>());

			if (setupMarkers.Exists(MarkerName))
			{
				return;
			}

			// A site that already has a contact form (from before the marker existed, or made by editors) needs no new one
			var hasContactForm = contentRepository.GetDescendents(startPage.ContentLink)
				.Concat(contentRepository.GetDescendents(ContentReference.WasteBasket))
				.Select(contentLink => contentRepository.TryGet<ArticlePage>(contentLink, loaderOptions, out var article) ? article : null)
				.Any(article => article?.MainContentArea?.Items.Any(item => contentRepository.TryGet<ContactFormBlock>(item.ContentLink, out _)) == true);

			if (!hasContactForm)
			{
				try
				{
					CreateContactPage(contentRepository, contentAssetHelper, startPage);
				}
				catch (Exception e)
				{
					// The contact page must never stop the site from starting; no marker, so the next startup tries again
					context.Services.GetRequiredService<ILogger<ContactPageInitialization>>().LogError(e, "Creating the contact page failed");
					return;
				}
			}

			setupMarkers.Add(MarkerName);
		}

		public void Uninitialize(InitializationEngine context)
		{
		}

		private static void CreateContactPage(IContentRepository contentRepository, ContentAssetHelper contentAssetHelper, StartPage startPage)
		{
			var masterLanguage = startPage.MasterLanguage;
			var otherLanguages = startPage.ExistingLanguages.Where(language => !language.Equals(masterLanguage)).ToList();

			// A draft first: the block goes in the page's own asset folder, which needs a saved page
			var page = contentRepository.GetDefault<ArticlePage>(startPage.ContentLink, masterLanguage);
			SetTexts(page, TextsFor(masterLanguage));
			var pageLink = contentRepository.Save(page, SaveAction.Default, AccessLevel.NoAccess);

			try
			{
				var blockLink = CreateBlock(contentRepository, contentAssetHelper.GetOrCreateAssetFolder(pageLink).ContentLink, masterLanguage, otherLanguages);

				var draft = (ArticlePage)contentRepository.Get<ArticlePage>(pageLink).CreateWritableClone();
				draft.MainContentArea = new ContentArea();
				draft.MainContentArea.Items.Add(new ContentAreaItem { ContentLink = blockLink });
				contentRepository.Save(draft, SaveAction.Publish, AccessLevel.NoAccess);

				foreach (var language in otherLanguages)
				{
					var languageBranch = contentRepository.CreateLanguageBranch<ArticlePage>(pageLink.ToReferenceWithoutVersion(), language);
					SetTexts(languageBranch, TextsFor(language));

					contentRepository.Save(languageBranch, SaveAction.Publish, AccessLevel.NoAccess);
				}
			}
			catch
			{
				// Don't leave a half-made page behind (its asset folder and block go with it); the next startup tries again.
				// A failing delete must not hide the original error.
				try
				{
					contentRepository.Delete(pageLink.ToReferenceWithoutVersion(), true, AccessLevel.NoAccess);
				}
				catch
				{
				}

				throw;
			}
		}

		private static ContentReference CreateBlock(IContentRepository contentRepository, ContentReference folderLink, CultureInfo masterLanguage, IEnumerable<CultureInfo> otherLanguages)
		{
			var block = contentRepository.GetDefault<ContactFormBlock>(folderLink, masterLanguage);
			((IContent)block).Name = "Contact form";
			SetTexts(block, TextsFor(masterLanguage));

			var blockLink = contentRepository.Save((IContent)block, SaveAction.Publish, AccessLevel.NoAccess).ToReferenceWithoutVersion();

			foreach (var language in otherLanguages)
			{
				var languageBranch = contentRepository.CreateLanguageBranch<ContactFormBlock>(blockLink, language);
				((IContent)languageBranch).Name = ((IContent)block).Name;
				SetTexts(languageBranch, TextsFor(language));

				contentRepository.Save((IContent)languageBranch, SaveAction.Publish, AccessLevel.NoAccess);
			}

			return blockLink;
		}

		/// <summary>Swedish for Swedish, English for every other language.</summary>
		private static Texts TextsFor(CultureInfo language)
			=> language.Name.StartsWith("sv", StringComparison.OrdinalIgnoreCase) ? Swedish : English;

		private static void SetTexts(ArticlePage page, Texts texts)
		{
			page.Name = texts.PageName;
			page.MetaDescription = texts.MetaDescription;
			page.MainBody = new XhtmlString(texts.MainBody);
		}

		private static void SetTexts(ContactFormBlock block, Texts texts)
		{
			block.Heading = texts.Heading;
			block.Intro = texts.Intro;
		}
	}
}
