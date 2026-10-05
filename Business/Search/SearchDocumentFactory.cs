using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using EPiServer.Framework.Web;
using EPiServer.Security;
using EPiServer.Web;
using Lucene.Net.Documents;
using Optimizely26.Models.Pages;

namespace Optimizely26.Business.Search
{
	/// <summary>Decides which languages of a page belong in the search index, and turns each one into a Lucene document.</summary>
	public partial class SearchDocumentFactory(
		IContentLoader contentLoader,
		IContentTypeRepository contentTypeRepository,
		IPublishedStateAssessor publishedStateAssessor,
		ITemplateResolver templateResolver,
		IContentAccessEvaluator contentAccessEvaluator)
	{
		/// <summary>A visitor who isn't signed in: no name and no roles, so only pages Everyone or Anonymous may read pass.</summary>
		private static readonly IPrincipal AnonymousVisitor = new GenericPrincipal(new GenericIdentity(string.Empty), []);

		private readonly IContentLoader _contentLoader = contentLoader;
		private readonly IContentTypeRepository _contentTypeRepository = contentTypeRepository;
		private readonly IPublishedStateAssessor _publishedStateAssessor = publishedStateAssessor;
		private readonly ITemplateResolver _templateResolver = templateResolver;
		private readonly IContentAccessEvaluator _contentAccessEvaluator = contentAccessEvaluator;

		/// <summary>One document per language in which the page belongs in the index; empty when it belongs in none.</summary>
		/// <remarks>Also empty for content that isn't a <see cref="SitePageData"/>, such as blocks, media and container pages.</remarks>
		public IReadOnlyList<Document> Create(ContentReference contentLink)
		{
			var masterLanguage = new LoaderOptions { LanguageLoaderOption.MasterLanguage() };

			if (!_contentLoader.TryGet<SitePageData>(contentLink.ToReferenceWithoutVersion(), masterLanguage, out var master) || !BelongsInIndex(master))
			{
				return [];
			}

			// Specific: a language the page isn't translated to must not come back as the master language
			return master.ExistingLanguages
				.Select(culture => _contentLoader.TryGet<SitePageData>(master.ContentLink, new LoaderOptions { LanguageLoaderOption.Specific(culture) }, out var page) ? page : null)
				.OfType<SitePageData>()
				.Where(IsVisibleToVisitors)
				.Select(CreateDocument)
				.ToList();
		}

		/// <summary>The rules that are the same in every language.</summary>
		private bool BelongsInIndex(SitePageData page)
		{
			// Error and sitemap pages aren't content, the movie page is a 404 without ?id=, and the find page would find itself
			if (page.IsDeleted || page is ErrorPage or XmlSitemap or MoviePage or FindPage)
			{
				return false;
			}

			// Pages without a template, such as carousel and settings pages, can't be opened
			if (!_templateResolver.HasTemplate(page, TemplateTypeCategories.Request))
			{
				return false;
			}

			return page is StartPage || _contentLoader.GetAncestors(page.ContentLink).OfType<StartPage>().Any();
		}

		private bool IsVisibleToVisitors(SitePageData page)
			=> _publishedStateAssessor.IsPublished(page, PublishedStateCondition.None)
				&& _contentAccessEvaluator.HasAccess(page, AnonymousVisitor, AccessLevel.Read);

		private Document CreateDocument(SitePageData page)
		{
			var contentId = page.ContentLink.ID.ToString(CultureInfo.InvariantCulture);
			var language = page.Language.Name.ToLowerInvariant();

			return new Document
			{
				new StringField(SearchFields.Key, $"{contentId}:{language}", Field.Store.YES),
				new StringField(SearchFields.ContentId, contentId, Field.Store.YES),
				new StringField(SearchFields.Language, language, Field.Store.YES),
				new TextField(SearchFields.Name, page.Name ?? string.Empty, Field.Store.NO),
				// The property's own value: the MetaDescription getter falls back to the name, which would count it twice
				new TextField(SearchFields.Description, page.Property[nameof(SitePageData.MetaDescription)]?.Value as string ?? string.Empty, Field.Store.NO),
				new TextField(SearchFields.Content, GetContentText(page), Field.Store.NO),
				new Int64Field(SearchFields.StopPublish, page.StopPublish?.ToUniversalTime().Ticks ?? long.MaxValue, Field.Store.NO),
			};
		}

		/// <summary>The text of every searchable string and XHTML property except the meta description, without HTML.</summary>
		private string GetContentText(SitePageData page)
		{
			var texts = _contentTypeRepository.Load(page.ContentTypeID).PropertyDefinitions
				.Where(definition => definition.IndexingType is IndexingType.Default or IndexingType.Searchable)
				.Where(definition => definition.Name != nameof(SitePageData.MetaDescription))
				.Select(definition => page.Property[definition.Name]?.Value switch
				{
					XhtmlString xhtml => WebUtility.HtmlDecode(HtmlTag().Replace(xhtml.ToHtmlString(), " ")),
					string text => text,
					_ => null,
				})
				.Where(text => !string.IsNullOrWhiteSpace(text));

			return string.Join(" ", texts);
		}

		[GeneratedRegex("<[^>]*>")]
		private static partial Regex HtmlTag();
	}
}
