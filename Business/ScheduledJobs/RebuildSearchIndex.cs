using EPiServer.Scheduler;
using Optimizely26.Business.Search;

namespace Optimizely26.Business.ScheduledJobs
{
	[ScheduledJob(
		GUID = "2A2A6665-FCA1-4D72-B4D2-4F1EEE6406D0",
		DisplayName = "Rebuild search index",
		Description = "Rebuilds the site search index from the published pages"
	)]
	public class RebuildSearchIndex(SearchIndexQueue queue) : ScheduledJobBase
	{
		private readonly SearchIndexQueue _queue = queue;

		public override string Execute()
		{
			// Through the queue, so the rebuild never runs at the same time as other index updates
			var count = _queue.RebuildAsync().GetAwaiter().GetResult();

			return $"Indexed {count} page languages.";
		}
	}
}
