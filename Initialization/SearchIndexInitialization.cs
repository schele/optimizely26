using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Optimizely26.Business.Search;

namespace Optimizely26.Initialization
{
	/// <summary>
	/// Keeps the search index in step with the content. Each content event only queues work for <see cref="SearchIndexWorker"/>,
	/// so publishing never waits for the index, and a missing or outdated index is rebuilt at startup.
	/// </summary>
	/// <remarks>
	/// Expiry needs no event: nothing fires when stop publish passes, so the search filters on <see cref="SearchFields.StopPublish"/>.
	/// </remarks>
	[InitializableModule]
	[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
	public class SearchIndexInitialization : IInitializableModule
	{
		private SearchIndexQueue? _queue;
		private IContentEvents? _contentEvents;
		private IContentSecurityEvents? _contentSecurityEvents;

		public void Initialize(InitializationEngine context)
		{
			_queue = context.Services.GetRequiredService<SearchIndexQueue>();
			OpenIndex(context.Services);

			// After a rebuild is queued, so the worker handles it before any of these
			_contentEvents = context.Services.GetRequiredService<IContentEvents>();
			_contentEvents.PublishedContent += OnPublishedContent;
			_contentEvents.MovedContent += OnMovedContent;
			_contentEvents.DeletedContent += OnDeletedContent;
			_contentEvents.DeletedContentLanguage += OnDeletedContentLanguage;

			_contentSecurityEvents = context.Services.GetRequiredService<IContentSecurityEvents>();
			_contentSecurityEvents.ContentSecuritySaved += OnContentSecuritySaved;
		}

		public void Uninitialize(InitializationEngine context)
		{
			if (_contentEvents != null)
			{
				_contentEvents.PublishedContent -= OnPublishedContent;
				_contentEvents.MovedContent -= OnMovedContent;
				_contentEvents.DeletedContent -= OnDeletedContent;
				_contentEvents.DeletedContentLanguage -= OnDeletedContentLanguage;
			}

			if (_contentSecurityEvents != null)
			{
				_contentSecurityEvents.ContentSecuritySaved -= OnContentSecuritySaved;
			}
		}

		/// <summary>Opening queues a rebuild when the index is missing or outdated (see <see cref="SearchIndex.EnsureOpen"/>).</summary>
		private static void OpenIndex(IServiceProvider services)
		{
			try
			{
				services.GetRequiredService<SearchIndex>().EnsureOpen();
			}
			catch (Exception e)
			{
				// Most likely another process holds the index's write lock. The site still starts, searches show
				// "unavailable", and the first successful open later rebuilds the index.
				services.GetRequiredService<ILogger<SearchIndexInitialization>>().LogError(e, "Opening the search index failed");
			}
		}

		// Also covers unpublishing, and a page that was published again. Blocks and media are never indexed.
		private void OnPublishedContent(object? sender, ContentEventArgs e)
		{
			if (e.Content is null or PageData)
			{
				_queue?.Reindex(e.ContentLink);
			}
		}

		// Moving to the trash removes the pages and restoring adds them back, because the worker checks where they are now
		private void OnMovedContent(object? sender, ContentEventArgs e)
		{
			if (e.Content is null or PageData)
			{
				_queue?.Reindex(e.ContentLink, includeDescendants: true);
			}
		}

		// The pages can't be loaded any more, so they are removed by id
		private void OnDeletedContent(object? sender, DeleteContentEventArgs e) => _queue?.Remove(e.DeletedDescendents.Prepend(e.ContentLink));

		// The deleted language no longer loads, so reindexing drops its document
		private void OnDeletedContentLanguage(object? sender, ContentEventArgs e) => _queue?.Reindex(e.ContentLink);

		// Access rights are inherited, so descendants may have changed too
		private void OnContentSecuritySaved(object? sender, ContentSecurityEventArg e) => _queue?.Reindex(e.ContentLink, includeDescendants: true);
	}
}
