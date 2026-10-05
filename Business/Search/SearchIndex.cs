using System.Globalization;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Optimizely26.Business.Search
{
	/// <summary>
	/// The site search's Lucene index in App_Data/SearchIndex, and the only class that touches its files. It owns the single
	/// writer, commits after every change, and searches the latest commit.
	/// </summary>
	/// <remarks>Only <see cref="SearchIndexWorker"/> calls the write methods, one at a time.</remarks>
	public sealed class SearchIndex : IDisposable
	{
		/// <summary>Raise this when the fields or the analyzer change; the next startup then wipes and rebuilds the index.</summary>
		public const int LayoutVersion = 1;

		private const string LayoutVersionKey = "layoutVersion";
		private const LuceneVersion Version = LuceneVersion.LUCENE_48;

		private readonly FSDirectory _directory;
		private readonly IndexWriter _writer;
		private readonly SearcherManager _searcherManager;
		private volatile bool _isCurrent;

		public SearchIndex(IWebHostEnvironment environment, ILogger<SearchIndex> logger)
		{
			_directory = FSDirectory.Open(new DirectoryInfo(Path.Combine(environment.ContentRootPath, "App_Data", "SearchIndex")));
			_isCurrent = ReadLayoutVersion(logger) == LayoutVersion;

			Analyzer = new StandardAnalyzer(Version, CharArraySet.Empty);

			// A missing, outdated or unreadable index starts out empty here; SearchIndexInitialization then queues a rebuild
			var config = new IndexWriterConfig(Version, Analyzer) { OpenMode = _isCurrent ? OpenMode.APPEND : OpenMode.CREATE };
			_writer = new IndexWriter(_directory, config);
			_searcherManager = new SearcherManager(_writer, applyAllDeletes: true, searcherFactory: null);
		}

		/// <summary>Splits text into lowercased words, with no stop words and no stemming. Used for indexing and for queries.</summary>
		public Analyzer Analyzer { get; }

		/// <summary>False until the index holds a full build with the current <see cref="LayoutVersion"/>.</summary>
		public bool IsCurrent => _isCurrent;

		/// <summary>Replaces every document of each page with the given ones (none removes the page), in one commit.</summary>
		public void ReplacePages(IReadOnlyList<(int ContentId, IReadOnlyList<Document> Documents)> pages)
		{
			foreach (var (contentId, documents) in pages)
			{
				_writer.DeleteDocuments(ContentIdTerm(contentId));

				foreach (var document in documents)
				{
					_writer.AddDocument(document);
				}
			}

			Commit();
		}

		public void RemovePages(IReadOnlyList<int> contentIds)
		{
			_writer.DeleteDocuments(contentIds.Select(ContentIdTerm).ToArray());
			Commit();
		}

		/// <summary>Replaces the whole index in one commit; searches see the previous index until then.</summary>
		public void ReplaceAll(IReadOnlyList<Document> documents)
		{
			_writer.DeleteAll();

			foreach (var document in documents)
			{
				_writer.AddDocument(document);
			}

			_writer.SetCommitData(new Dictionary<string, string> { [LayoutVersionKey] = LayoutVersion.ToString(CultureInfo.InvariantCulture) });
			Commit();
			_isCurrent = true;
		}

		/// <summary>Page <paramref name="page"/> (1-based) of the matches, best first. A page past the end gives the last page.</summary>
		public SearchIndexPage Search(Query query, Filter filter, int page, int pageSize)
		{
			var searcher = _searcherManager.Acquire();

			try
			{
				// Lucene caps the count at the number of documents; the long keeps a huge page number from overflowing
				var count = (int)Math.Min((long)page * pageSize, int.MaxValue);
				var topDocs = searcher.Search(query, filter, count);
				var lastPage = Math.Max(1, (int)Math.Ceiling(topDocs.TotalHits / (double)pageSize));
				page = Math.Min(page, lastPage);

				var documents = topDocs.ScoreDocs
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.Select(scoreDoc => searcher.Doc(scoreDoc.Doc))
					.ToList();

				return new SearchIndexPage(documents, topDocs.TotalHits, page);
			}
			finally
			{
				_searcherManager.Release(searcher);
			}
		}

		public void Dispose()
		{
			_searcherManager.Dispose();
			_writer.Dispose();
			Analyzer.Dispose();
			_directory.Dispose();
		}

		private static Term ContentIdTerm(int contentId) => new(SearchFields.ContentId, contentId.ToString(CultureInfo.InvariantCulture));

		private void Commit()
		{
			_writer.Commit();
			_searcherManager.MaybeRefresh();
		}

		/// <summary>The layout version the index was built with; null when there is no index or it can't be read.</summary>
		private int? ReadLayoutVersion(ILogger logger)
		{
			try
			{
				if (!DirectoryReader.IndexExists(_directory))
				{
					return null;
				}

				using var reader = DirectoryReader.Open(_directory);

				return reader.IndexCommit.UserData.TryGetValue(LayoutVersionKey, out var value)
					&& int.TryParse(value, CultureInfo.InvariantCulture, out var version) ? version : null;
			}
			catch (Exception e)
			{
				logger.LogWarning(e, "The search index can't be read; it will be rebuilt");
				return null;
			}
		}
	}
}
