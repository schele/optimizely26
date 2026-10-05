using System.Threading.Channels;

namespace Optimizely26.Business.Search
{
	/// <summary>Work for <see cref="SearchIndexWorker"/>. Adding work never blocks, so content events stay fast.</summary>
	public class SearchIndexQueue
	{
		private readonly Channel<SearchIndexWork> _channel = Channel.CreateUnbounded<SearchIndexWork>(new UnboundedChannelOptions { SingleReader = true });

		public ChannelReader<SearchIndexWork> Reader => _channel.Reader;

		public void Reindex(ContentReference contentLink, bool includeDescendants = false)
		{
			_channel.Writer.TryWrite(new SearchIndexWork.Reindex(contentLink.ToReferenceWithoutVersion(), includeDescendants));
		}

		public void Remove(IEnumerable<ContentReference> contentLinks)
		{
			_channel.Writer.TryWrite(new SearchIndexWork.Remove(contentLinks.Select(contentLink => contentLink.ID).Distinct().ToList()));
		}

		/// <summary>Rebuilds the whole index; the task gives the number of documents written.</summary>
		public Task<int> RebuildAsync()
		{
			var work = new SearchIndexWork.Rebuild(new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously));
			_channel.Writer.TryWrite(work);

			return work.Completion.Task;
		}
	}
}
