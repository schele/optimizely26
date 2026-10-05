using System.Globalization;
using EPiServer.Web.Routing;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Optimizely26.Business;
using Optimizely26.Business.Search;
using Optimizely26.Models.Find;
using Optimizely26.Models.Pages;

namespace Optimizely26.Services
{
	/// <summary>Searches the site's own Lucene index (see <see cref="SearchIndex"/>) and loads the hits from the CMS.</summary>
	public class FindService(SearchIndex index, VisitorAccess visitorAccess, IContentLoader contentLoader, IUrlResolver urlResolver) : IFindService
	{
		/// <summary>Words after this many are ignored.</summary>
		private const int MaxWords = 10;

		/// <summary>Shorter words only match whole words, so "fi" doesn't match everything that starts with "fi".</summary>
		private const int MinPrefixLength = 3;

		/// <summary>A whole word counts this much more than a word that only starts with it.</summary>
		private const float ExactWordBoost = 2f;

		/// <summary>Extra score when all the words appear together, in this order.</summary>
		private const float NamePhraseBoost = 6f;
		private const float ContentPhraseBoost = 2f;

		/// <summary>How much a match in each field counts.</summary>
		private static readonly (string Field, float Weight)[] WeightedFields =
		[
			(SearchFields.Name, 3f),
			(SearchFields.Description, 2f),
			(SearchFields.Content, 1f),
		];

		private readonly SearchIndex _index = index;
		private readonly IContentLoader _contentLoader = contentLoader;
		private readonly IUrlResolver _urlResolver = urlResolver;
		private readonly VisitorAccess _visitorAccess = visitorAccess;

		public FindResult FindContent(string query, CultureInfo culture, int page, int pageSize)
		{
			var words = GetWords(query);

			if (words.Count == 0)
			{
				return FindResult.Empty;
			}

			var result = _index.Search(BuildQuery(words), BuildFilter(culture), Math.Max(page, 1), pageSize);
			var hits = result.Documents
				.Select(document => ToHit(document, culture))
				.OfType<Hit>()
				.ToList();

			return new FindResult(hits, result.TotalHits, result.Page);
		}

		/// <summary>The query split into words the same way the index splits the text, so they can be looked up as they are.</summary>
		private List<string> GetWords(string query)
		{
			var words = new List<string>();

			if (string.IsNullOrWhiteSpace(query))
			{
				return words;
			}

			using var tokenStream = _index.Analyzer.GetTokenStream(SearchFields.Name, query);
			var term = tokenStream.AddAttribute<ICharTermAttribute>();
			tokenStream.Reset();

			while (words.Count < MaxWords && tokenStream.IncrementToken())
			{
				words.Add(term.ToString());
			}

			tokenStream.End();

			return words;
		}

		/// <summary>
		/// Every word must match in at least one field. Built in code rather than parsed, so nothing the visitor types
		/// (quotes, *, AND, field:value) can cause a syntax error.
		/// </summary>
		private static Query BuildQuery(IReadOnlyList<string> words)
		{
			var query = new BooleanQuery();

			foreach (var word in words)
			{
				var wordQuery = new BooleanQuery();

				foreach (var (field, weight) in WeightedFields)
				{
					wordQuery.Add(new TermQuery(new Term(field, word)) { Boost = weight * ExactWordBoost }, Occur.SHOULD);

					if (word.Length >= MinPrefixLength)
					{
						wordQuery.Add(new PrefixQuery(new Term(field, word)) { Boost = weight }, Occur.SHOULD);
					}
				}

				query.Add(wordQuery, Occur.MUST);
			}

			if (words.Count > 1)
			{
				query.Add(Phrase(SearchFields.Name, words, NamePhraseBoost), Occur.SHOULD);
				query.Add(Phrase(SearchFields.Content, words, ContentPhraseBoost), Occur.SHOULD);
			}

			return query;
		}

		private static PhraseQuery Phrase(string field, IEnumerable<string> words, float boost)
		{
			var phrase = new PhraseQuery { Boost = boost };

			foreach (var word in words)
			{
				phrase.Add(new Term(field, word));
			}

			return phrase;
		}

		/// <summary>Only the current language, and not expired. A filter, so it doesn't change the scores.</summary>
		private static Filter BuildFilter(CultureInfo culture) => new QueryWrapperFilter(new BooleanQuery
		{
			{ new TermQuery(new Term(SearchFields.Language, culture.Name.ToLowerInvariant())), Occur.MUST },
			{ NumericRangeQuery.NewInt64Range(SearchFields.StopPublish, DateTime.UtcNow.Ticks, long.MaxValue, false, true), Occur.MUST },
		});

		/// <summary>
		/// The hit as the page is now; null when it no longer loads, or is no longer published or public. The index catches up
		/// shortly, but until then a visitor must never see the name of a page they may not read.
		/// </summary>
		private Hit? ToHit(Document document, CultureInfo culture)
		{
			var contentLink = new ContentReference(int.Parse(document.Get(SearchFields.ContentId), CultureInfo.InvariantCulture));

			if (!_contentLoader.TryGet<SitePageData>(contentLink, new LoaderOptions { LanguageLoaderOption.Specific(culture) }, out var page)
				|| !_visitorAccess.CanSee(page))
			{
				return null;
			}

			return new Hit
			{
				Name = page.Name,
				Description = page.MetaDescription,
				Url = _urlResolver.GetUrl(page.ContentLink, culture.Name) ?? string.Empty,
				Changed = page.Changed,
			};
		}
	}
}
