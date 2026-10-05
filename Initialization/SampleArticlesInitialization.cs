using EPiServer.Data.Dynamic;
using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Optimizely26.Business;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
	/// <summary>
	/// Creates and publishes a handful of sample articles under the start page, in Swedish and English, so the site search
	/// has something to find. Once per database (a <c>SetupMarker</c>), and only when the site has no articles yet.
	/// </summary>
	/// <remarks>
	/// Runs after <see cref="SearchIndexInitialization"/>, so the publish events reach the search index.
	/// </remarks>
	[InitializableModule]
	[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
	[ModuleDependency(typeof(SiteSetupInitialization))]
	[ModuleDependency(typeof(SearchIndexInitialization))]
	public class SampleArticlesInitialization : IInitializableModule
	{
		private const string MarkerName = "sample-articles";

		private record SampleText(string Name, string MetaDescription, string MainBody);

		private record SampleArticle(SampleText Swedish, SampleText English);

		private static readonly SampleArticle[] Articles =
		[
			new(
				new("Så hittar du rätt film",
					"Tips för att söka bland tusentals filmer, serier och avsnitt i Movie Finder.",
					"<p>Skriv en titel i sökrutan på startsidan, till exempel <strong>Batman</strong>, så hämtar Movie Finder träffar från OMDb. Klicka på en film för att se handling, skådespelare och betyg.</p><p>Hittar du inte det du letar efter? Prova en kortare titel eller originaltiteln på engelska.</p>"),
				new("How to find the right movie",
					"Tips for searching thousands of movies, series and episodes in Movie Finder.",
					"<p>Type a title in the search box on the start page, for example <strong>Batman</strong>, and Movie Finder fetches matches from OMDb. Click a movie to see its plot, cast and ratings.</p><p>Can't find what you're looking for? Try a shorter title or the original English title.</p>")),
			new(
				new("Betygsätt filmer du har sett",
					"Ge filmer mellan en halv och fem stjärnor och läs vad andra besökare tyckte.",
					"<p>På varje filmsida kan du sätta ett betyg från en halv till fem stjärnor och skriva en kommentar. Ditt namn är frivilligt.</p><p>Snittbetyget och de senaste omdömena visas direkt under filmens detaljer.</p>"),
				new("Rate the movies you have seen",
					"Give movies between half a star and five stars and read what other visitors thought.",
					"<p>On every movie page you can give a rating from half a star to five stars and write a comment. Your name is optional.</p><p>The average rating and the latest reviews are shown right below the movie's details.</p>")),
			new(
				new("Klassiska skräckfilmer att se i höst",
					"Från The Shining till Halloween: skräckklassiker som fortfarande skrämmer.",
					"<p>Höstmörkret är perfekt för skräck. <em>The Shining</em> bygger upp obehaget långsamt, medan <em>Halloween</em> visar hur mycket spänning en enkel melodi kan skapa.</p><p>Vill du ha något nyare? <em>Get Out</em> blandar skräck med skarp samhällskritik.</p>"),
				new("Classic horror movies to watch this autumn",
					"From The Shining to Halloween: horror classics that still scare.",
					"<p>Dark autumn evenings are perfect for horror. <em>The Shining</em> builds its dread slowly, while <em>Halloween</em> shows how much tension a simple melody can create.</p><p>Want something newer? <em>Get Out</em> mixes horror with sharp social commentary.</p>")),
			new(
				new("Komedier för en fredagskväll",
					"Skratt garanterat: komedier som passar hela sällskapet.",
					"<p><em>Måndag hela veckan</em> är en komedi som blir bättre för varje gång du ser den. <em>I hetaste laget</em> visar att svartvita komedier fortfarande håller.</p><p>För en modernare fredag passar <em>Superbad</em> eller <em>The Grand Budapest Hotel</em>.</p>"),
				new("Comedies for a Friday night",
					"Laughs guaranteed: comedies the whole group will enjoy.",
					"<p><em>Groundhog Day</em> is a comedy that gets better every time you watch it. <em>Some Like It Hot</em> proves that black-and-white comedies still hold up.</p><p>For a more modern Friday, try <em>Superbad</em> or <em>The Grand Budapest Hotel</em>.</p>")),
			new(
				new("Dramer som berör",
					"Filmer om hopp, vänskap och svåra val som stannar kvar länge.",
					"<p><em>Nyckeln till frihet</em> handlar om hopp i en hopplös miljö och toppar ofta listor över världens bästa filmer.</p><p><em>Manchester by the Sea</em> och <em>Moonlight</em> är stillsammare dramer som berör på djupet.</p>"),
				new("Dramas that stay with you",
					"Movies about hope, friendship and hard choices that linger.",
					"<p><em>The Shawshank Redemption</em> is about hope in a hopeless place and often tops lists of the best movies ever made.</p><p><em>Manchester by the Sea</em> and <em>Moonlight</em> are quieter dramas that move you deeply.</p>")),
			new(
				new("Thrillers med oväntade slut",
					"Spänning till sista minuten: thrillers som överraskar.",
					"<p><em>Seven</em> och <em>Gone Girl</em> är två thrillers av David Fincher där slutet förändrar allt du trodde dig veta.</p><p><em>De misstänkta</em> har ett av filmhistoriens mest omtalade slut.</p>"),
				new("Thrillers with unexpected endings",
					"Suspense to the last minute: thrillers that surprise.",
					"<p><em>Se7en</em> and <em>Gone Girl</em> are two David Fincher thrillers whose endings change everything you thought you knew.</p><p><em>The Usual Suspects</em> has one of the most talked-about endings in film history.</p>")),
		];

		public void Initialize(InitializationEngine context)
		{
			var contentRepository = context.Services.GetRequiredService<IContentRepository>();
			var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() };
			var startPage = contentRepository.GetChildren<StartPage>(ContentReference.RootPage, loaderOptions).FirstOrDefault();

			if (startPage == null)
			{
				// Nothing to serve yet; the articles are created on a later startup.
				return;
			}

			// Once per database, so articles an editor deleted, even from the trash, don't come back on the next startup
			var setupMarkers = new SetupMarkers(context.Services.GetRequiredService<DynamicDataStoreFactory>());

			if (setupMarkers.Exists(MarkerName))
			{
				return;
			}

			// A site that already has articles (from before the marker existed, or made by editors) needs no samples
			var hasArticles = contentRepository.GetDescendents(startPage.ContentLink)
				.Concat(contentRepository.GetDescendents(ContentReference.WasteBasket))
				.Any(contentLink => contentRepository.TryGet<ArticlePage>(contentLink, loaderOptions, out _));

			if (!hasArticles)
			{
				try
				{
					CreateArticles(contentRepository, startPage);
				}
				catch (Exception e)
				{
					// Sample content must never stop the site from starting; no marker, so the next startup tries again
					context.Services.GetRequiredService<ILogger<SampleArticlesInitialization>>().LogError(e, "Creating the sample articles failed");
					return;
				}
			}

			setupMarkers.Add(MarkerName);
		}

		public void Uninitialize(InitializationEngine context)
		{
		}

		private static void CreateArticles(IContentRepository contentRepository, StartPage startPage)
		{
			foreach (var article in Articles)
			{
				var page = contentRepository.GetDefault<ArticlePage>(startPage.ContentLink, startPage.MasterLanguage);
				SetTexts(page, TextFor(article, startPage.MasterLanguage));

				var pageLink = contentRepository.Save(page, SaveAction.Publish, AccessLevel.NoAccess);

				foreach (var language in startPage.ExistingLanguages.Where(language => !language.Equals(startPage.MasterLanguage)))
				{
					var languageBranch = contentRepository.CreateLanguageBranch<ArticlePage>(pageLink, language);
					SetTexts(languageBranch, TextFor(article, language));

					contentRepository.Save(languageBranch, SaveAction.Publish, AccessLevel.NoAccess);
				}
			}
		}

		/// <summary>Swedish for Swedish, English for every other language.</summary>
		private static SampleText TextFor(SampleArticle article, System.Globalization.CultureInfo language)
			=> language.Name.StartsWith("sv", StringComparison.OrdinalIgnoreCase) ? article.Swedish : article.English;

		private static void SetTexts(ArticlePage page, SampleText text)
		{
			page.Name = text.Name;
			page.MetaDescription = text.MetaDescription;
			page.MainBody = new XhtmlString(text.MainBody);
		}
	}
}
