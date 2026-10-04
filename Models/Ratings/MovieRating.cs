using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Optimizely26.Models.Ratings
{
	/// <summary>A visitor's MovieFinder rating of a title, stored in the Dynamic Data Store.</summary>
	/// <remarks>The fixed store name keeps existing ratings reachable if this class moves namespace.</remarks>
	[EPiServerDataStore(StoreName = "MovieFinderRatings", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
	public class MovieRating : IDynamicData
	{
		public const double MinScore = 0.5;
		public const double MaxScore = 5;
		public const int NameMaxLength = 100;
		public const int CommentMaxLength = 2000;

		public Identity Id { get; set; } = Identity.NewIdentity();

		/// <summary>The OMDb/IMDb id, e.g. <c>tt0111161</c>. Indexed: every query filters on it.</summary>
		[EPiServerDataIndex]
		public string ImdbId { get; set; } = string.Empty;

		/// <summary>0.5–5 stars in steps of 0.5.</summary>
		public double Score { get; set; }

		public string? Name { get; set; }

		public string? Comment { get; set; }

		public DateTime CreatedUtc { get; set; }
	}
}
