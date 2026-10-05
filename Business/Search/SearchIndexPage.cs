using Lucene.Net.Documents;

namespace Optimizely26.Business.Search
{
	/// <summary>One page of documents from <see cref="SearchIndex.Search"/>; <see cref="Page"/> is the page actually returned.</summary>
	public record SearchIndexPage(IReadOnlyList<Document> Documents, int TotalHits, int Page);
}
