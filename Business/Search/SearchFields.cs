namespace Optimizely26.Business.Search
{
	/// <summary>The field names in the search index, shared by the code that writes documents and the code that queries them.</summary>
	public static class SearchFields
	{
		/// <summary><c>{contentId}:{language}</c>; one document per page and language.</summary>
		public const string Key = "key";

		/// <summary><c>ContentLink.ID</c>, without version. Every language of a page shares it.</summary>
		public const string ContentId = "contentId";

		/// <summary>The content language, lowercased: <c>sv</c> or <c>en</c>.</summary>
		public const string Language = "lang";

		public const string Name = "name";

		/// <summary>The meta description as the editor wrote it, without the fallback to the name.</summary>
		public const string Description = "description";

		/// <summary>The other searchable text properties, without HTML.</summary>
		public const string Content = "content";

		/// <summary>Stop publish in UTC ticks; <see cref="long.MaxValue"/> when the page never expires.</summary>
		public const string StopPublish = "stopPublish";
	}
}
