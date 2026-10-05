using Optimizely26.Models.Pages;

namespace Optimizely26.Business.Search
{
	/// <summary>
	/// The only writer of the search index. It takes work from <see cref="SearchIndexQueue"/> one item at a time and reads each
	/// page as it is now, so updates never overlap and a rebuild that is running can't put back an older version of a page.
	/// </summary>
	public class SearchIndexWorker(SearchIndexQueue queue, IServiceScopeFactory scopeFactory, ILogger<SearchIndexWorker> logger) : BackgroundService
	{
		private readonly SearchIndexQueue _queue = queue;
		private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
		private readonly ILogger<SearchIndexWorker> _logger = logger;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await foreach (var work in _queue.Reader.ReadAllAsync(stoppingToken))
			{
				try
				{
					// Resolved per item, so an index that can't be opened (say, locked by another process) fails one item, not the site
					using var scope = _scopeFactory.CreateScope();
					Handle(work, scope.ServiceProvider);
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Updating the search index failed: {Work}", work);
					(work as SearchIndexWork.Rebuild)?.Completion.TrySetException(e);
				}
			}
		}

		private void Handle(SearchIndexWork work, IServiceProvider services)
		{
			var index = services.GetRequiredService<SearchIndex>();
			var documentFactory = services.GetRequiredService<SearchDocumentFactory>();
			var contentLoader = services.GetRequiredService<IContentLoader>();

			switch (work)
			{
				case SearchIndexWork.Reindex reindex:
				{
					var contentLinks = reindex.IncludeDescendants
						? contentLoader.GetDescendents(reindex.ContentLink).Prepend(reindex.ContentLink)
						: [reindex.ContentLink];

					var pages = contentLinks
						.Select(contentLink => (contentLink.ID, documentFactory.Create(contentLink)))
						.ToList();

					index.ReplacePages(pages);
					break;
				}
				case SearchIndexWork.Remove remove:
				{
					index.RemovePages(remove.ContentIds);
					break;
				}
				case SearchIndexWork.Rebuild rebuild:
				{
					// Every document is built before the index is touched, so a failure leaves the previous index as it was
					var startPages = contentLoader.GetChildren<StartPage>(ContentReference.RootPage, new LoaderOptions { LanguageLoaderOption.FallbackWithMaster() });
					var documents = startPages
						.SelectMany(startPage => contentLoader.GetDescendents(startPage.ContentLink).Prepend(startPage.ContentLink))
						.SelectMany(documentFactory.Create)
						.ToList();

					index.ReplaceAll(documents);
					rebuild.Completion.TrySetResult(documents.Count);
					_logger.LogInformation("Rebuilt the search index with {Count} page languages", documents.Count);
					break;
				}
			}
		}
	}
}
