using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
	/// <summary>
	/// Creates and publishes the find page ("Sök", "Search") under the start page, in every language the start page has,
	/// unless a find page already exists under the start page.
	/// </summary>
	/// <remarks>Runs after <see cref="SiteSetupInitialization"/>, which sets up the site on an empty database.</remarks>
	[InitializableModule]
	[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
	[ModuleDependency(typeof(SiteSetupInitialization))]
	public class FindPageInitialization : IInitializableModule
	{
		/// <summary>The page name per language; other languages get <see cref="DefaultPageName"/>.</summary>
		private static readonly Dictionary<string, string> PageNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["sv"] = "Sök",
			["en"] = "Search",
		};

		private const string DefaultPageName = "Search";

		public void Initialize(InitializationEngine context)
		{
			var contentRepository = context.Services.GetRequiredService<IContentRepository>();
			var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() };
			var startPage = contentRepository.GetChildren<StartPage>(ContentReference.RootPage, loaderOptions).FirstOrDefault();

			if (startPage == null)
			{
				// Nothing to serve yet; the find page is created on a later startup.
				return;
			}

			// Any find page counts, published or not, so an editor's own page or an unpublished one is left alone
			var hasFindPage = contentRepository.GetDescendents(startPage.ContentLink)
				.Any(contentLink => contentRepository.TryGet<FindPage>(contentLink, loaderOptions, out _));

			if (hasFindPage)
			{
				return;
			}

			var findPage = contentRepository.GetDefault<FindPage>(startPage.ContentLink, startPage.MasterLanguage);
			findPage.Name = GetPageName(startPage.MasterLanguage);

			var findPageLink = contentRepository.Save(findPage, SaveAction.Publish, AccessLevel.NoAccess);

			foreach (var language in startPage.ExistingLanguages.Where(language => !language.Equals(startPage.MasterLanguage)))
			{
				var languageBranch = contentRepository.CreateLanguageBranch<FindPage>(findPageLink, language);
				languageBranch.Name = GetPageName(language);

				contentRepository.Save(languageBranch, SaveAction.Publish, AccessLevel.NoAccess);
			}
		}

		public void Uninitialize(InitializationEngine context)
		{
		}

		private static string GetPageName(System.Globalization.CultureInfo language)
			=> PageNames.TryGetValue(language.Name, out var name) ? name : DefaultPageName;
	}
}
