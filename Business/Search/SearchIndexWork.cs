namespace Optimizely26.Business.Search
{
	/// <summary>A change for <see cref="SearchIndexWorker"/> to make to the search index.</summary>
	public abstract record SearchIndexWork
	{
		/// <summary>Reads the page (and its descendants) as it is now and replaces its documents.</summary>
		public sealed record Reindex(ContentReference ContentLink, bool IncludeDescendants) : SearchIndexWork;

		/// <summary>Removes the pages' documents in every language.</summary>
		public sealed record Remove(IReadOnlyList<int> ContentIds) : SearchIndexWork;

		/// <summary>Rebuilds the whole index; <see cref="Completion"/> gives the number of documents written.</summary>
		public sealed record Rebuild(TaskCompletionSource<int> Completion) : SearchIndexWork;
	}
}
