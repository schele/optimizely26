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
	/// <remarks>
	/// <para>Only <see cref="SearchIndexWorker"/> calls the write methods, one at a time.</para>
	/// <para>
	/// The files are opened on first use, not in the constructor: while another process holds the index (an overlapping
	/// restart, a second copy of the site) every method throws, and callers show "unavailable" instead of failing to start.
	/// The next call tries again.
	/// </para>
	/// </remarks>
	public sealed class SearchIndex(IWebHostEnvironment environment, SearchIndexQueue queue, ILogger<SearchIndex> logger) : IDisposable
	{
		/// <summary>Raise this when the fields or the analyzer change; the next startup then wipes and rebuilds the index.</summary>
		public const int LayoutVersion = 1;

		private const string LayoutVersionKey = "layoutVersion";
		private const LuceneVersion Version = LuceneVersion.LUCENE_48;

		private sealed record OpenIndex(FSDirectory Directory, IndexWriter Writer, SearcherManager SearcherManager);

		private readonly string _path = Path.Combine(environment.ContentRootPath, "App_Data", "SearchIndex");
		private readonly SearchIndexQueue _queue = queue;
		private readonly ILogger<SearchIndex> _logger = logger;
		private readonly object _openLock = new();
		private volatile OpenIndex? _open;

		/// <summary>An earlier attempt to open the index failed, so content events may have been dropped in the meantime.</summary>
		private bool _openFailed;

		/// <summary>Splits text into lowercased words, with no stop words and no stemming. Used for indexing and for queries.</summary>
		public Analyzer Analyzer { get; } = new StandardAnalyzer(Version, CharArraySet.Empty);

		/// <summary>
		/// Opens the index if it isn't open yet, and queues a full rebuild when it's missing, outdated or unreadable, or when
		/// earlier attempts failed. Throws while another process holds the index.
		/// </summary>
		public void EnsureOpen() => Open();

		/// <summary>Replaces every document of each page with the given ones (none removes the page), in one commit.</summary>
		public void ReplacePages(IReadOnlyList<(int ContentId, IReadOnlyList<Document> Documents)> pages)
		{
			var index = Open();

			foreach (var (contentId, documents) in pages)
			{
				index.Writer.DeleteDocuments(ContentIdTerm(contentId));

				foreach (var document in documents)
				{
					index.Writer.AddDocument(document);
				}
			}

			Commit(index);
		}

		public void RemovePages(IReadOnlyList<int> contentIds)
		{
			var index = Open();
			index.Writer.DeleteDocuments(contentIds.Select(ContentIdTerm).ToArray());
			Commit(index);
		}

		/// <summary>Replaces the whole index in one commit; searches see the previous index until then.</summary>
		public void ReplaceAll(IReadOnlyList<Document> documents)
		{
			var index = Open();
			index.Writer.DeleteAll();

			foreach (var document in documents)
			{
				index.Writer.AddDocument(document);
			}

			// The only place the version is written: a full build is what makes the index current
			index.Writer.SetCommitData(new Dictionary<string, string> { [LayoutVersionKey] = LayoutVersion.ToString(CultureInfo.InvariantCulture) });
			Commit(index);
		}

		/// <summary>Page <paramref name="page"/> (1-based) of the matches, best first. A page past the end gives the last page.</summary>
		public SearchIndexPage Search(Query query, Filter filter, int page, int pageSize)
		{
			var searcherManager = Open().SearcherManager;
			var searcher = searcherManager.Acquire();

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
				searcherManager.Release(searcher);
			}
		}

		public void Dispose()
		{
			if (_open is { } index)
			{
				index.SearcherManager.Dispose();

				// Every change commits as it's made, so this only drops one that shutdown interrupted halfway
				index.Writer.Rollback();
				index.Directory.Dispose();
			}

			Analyzer.Dispose();
		}

		private static Term ContentIdTerm(int contentId) => new(SearchFields.ContentId, contentId.ToString(CultureInfo.InvariantCulture));

		private static void Commit(OpenIndex index)
		{
			index.Writer.Commit();
			index.SearcherManager.MaybeRefresh();
		}

		private OpenIndex Open()
		{
			if (_open is { } open)
			{
				return open;
			}

			lock (_openLock)
			{
				if (_open is { } openedMeanwhile)
				{
					return openedMeanwhile;
				}

				FSDirectory? directory = null;
				IndexWriter? writer = null;
				bool isCurrent;

				try
				{
					directory = FSDirectory.Open(new DirectoryInfo(_path));
					isCurrent = ReadLayoutVersion(directory) == LayoutVersion;

					// A missing, outdated or unreadable index starts out empty, and the rebuild queued below fills it
					writer = new IndexWriter(directory, new IndexWriterConfig(Version, Analyzer) { OpenMode = isCurrent ? OpenMode.APPEND : OpenMode.CREATE });

					if (!isCurrent)
					{
						// CREATE keeps the old commit's version stamp; clear it, so a half-built index never passes as current
						writer.SetCommitData(new Dictionary<string, string>());
					}

					_open = new OpenIndex(directory, writer, new SearcherManager(writer, applyAllDeletes: true, searcherFactory: null));
				}
				catch
				{
					writer?.Rollback();
					directory?.Dispose();
					_openFailed = true;
					throw;
				}

				if (!isCurrent || _openFailed)
				{
					_logger.LogInformation("The search index is missing, outdated or may have missed changes; rebuilding it in the background");
					_ = _queue.RebuildAsync();
					_openFailed = false;
				}

				return _open;
			}
		}

		/// <summary>The layout version the index was built with; null when there is no index or it can't be read.</summary>
		private int? ReadLayoutVersion(FSDirectory directory)
		{
			try
			{
				if (!DirectoryReader.IndexExists(directory))
				{
					return null;
				}

				using var reader = DirectoryReader.Open(directory);

				return reader.IndexCommit.UserData.TryGetValue(LayoutVersionKey, out var value)
					&& int.TryParse(value, CultureInfo.InvariantCulture, out var version) ? version : null;
			}
			catch (Exception e) when (e is not LockObtainFailedException)
			{
				_logger.LogWarning(e, "The search index can't be read; it will be rebuilt");
				return null;
			}
		}
	}
}
