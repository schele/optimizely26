namespace Optimizely26.Models.Find
{
	/// <summary>One page of search hits. <see cref="Page"/> is the page actually returned: a page past the end becomes the last page.</summary>
	public record FindResult(IReadOnlyList<Hit> Hits, int TotalCount, int Page)
	{
		public static FindResult Empty { get; } = new([], 0, 1);
	}
}
