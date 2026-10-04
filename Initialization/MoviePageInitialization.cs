using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
	/// <summary>
	/// Creates and publishes the movie page that search results link to ("Movie", under the start page),
	/// in every language the start page has, unless a movie page already exists under the start page.
	/// </summary>
	/// <remarks>Runs after <see cref="SiteSetupInitialization"/>, which sets up the site on an empty database.</remarks>
	[InitializableModule]
	[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
	[ModuleDependency(typeof(SiteSetupInitialization))]
	public class MoviePageInitialization : IInitializableModule
	{
		private const string MoviePageName = "Movie";

		public void Initialize(InitializationEngine context)
		{
			var contentRepository = context.Services.GetRequiredService<IContentRepository>();
			var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() };
			var startPage = contentRepository.GetChildren<StartPage>(ContentReference.RootPage, loaderOptions).FirstOrDefault();

			if (startPage == null)
			{
				// Nothing to serve yet; the movie page is created on a later startup.
				return;
			}

			// Any movie page counts, published or not, so an editor's own page or an unpublished one is left alone
			var hasMoviePage = contentRepository.GetDescendents(startPage.ContentLink)
				.Any(contentLink => contentRepository.TryGet<MoviePage>(contentLink, loaderOptions, out _));

			if (hasMoviePage)
			{
				return;
			}

			var moviePage = contentRepository.GetDefault<MoviePage>(startPage.ContentLink, startPage.MasterLanguage);
			moviePage.Name = MoviePageName;

			var moviePageLink = contentRepository.Save(moviePage, SaveAction.Publish, AccessLevel.NoAccess);

			foreach (var language in startPage.ExistingLanguages.Where(language => !language.Equals(startPage.MasterLanguage)))
			{
				var languageBranch = contentRepository.CreateLanguageBranch<MoviePage>(moviePageLink, language);
				languageBranch.Name = MoviePageName;

				contentRepository.Save(languageBranch, SaveAction.Publish, AccessLevel.NoAccess);
			}
		}

		public void Uninitialize(InitializationEngine context)
		{
		}
	}
}
