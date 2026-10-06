using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
	/// <summary>
	/// Creates and publishes the settings page ("Settings") under the start page, in every language the start page has, with
	/// an empty menu for editors to fill, unless the start page already has a settings page.
	/// </summary>
	/// <remarks>Runs after <see cref="SiteSetupInitialization"/>, which sets up the site on an empty database.</remarks>
	[InitializableModule]
	[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
	[ModuleDependency(typeof(SiteSetupInitialization))]
	public class SettingsPageInitialization : IInitializableModule
	{
		private const string PageName = "Settings";

		public void Initialize(InitializationEngine context)
		{
			var contentRepository = context.Services.GetRequiredService<IContentRepository>();
			var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() };
			var startPage = contentRepository.GetChildren<StartPage>(ContentReference.RootPage, loaderOptions).FirstOrDefault();

			if (startPage == null)
			{
				// Nothing to serve yet; the settings page is created on a later startup.
				return;
			}

			// A child, as that is where the layout looks; published or not, so an editor's own page is left alone
			if (contentRepository.GetChildren<SettingsPage>(startPage.ContentLink, loaderOptions).Any())
			{
				return;
			}

			var settingsPage = contentRepository.GetDefault<SettingsPage>(startPage.ContentLink, startPage.MasterLanguage);
			settingsPage.Name = PageName;

			var settingsPageLink = contentRepository.Save(settingsPage, SaveAction.Publish, AccessLevel.NoAccess);

			foreach (var language in startPage.ExistingLanguages.Where(language => !language.Equals(startPage.MasterLanguage)))
			{
				var languageBranch = contentRepository.CreateLanguageBranch<SettingsPage>(settingsPageLink, language);
				languageBranch.Name = PageName;

				contentRepository.Save(languageBranch, SaveAction.Publish, AccessLevel.NoAccess);
			}
		}

		public void Uninitialize(InitializationEngine context)
		{
		}
	}
}
