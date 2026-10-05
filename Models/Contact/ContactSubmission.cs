using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Optimizely26.Models.Contact
{
	/// <summary>A message sent with the contact form, stored in the Dynamic Data Store.</summary>
	/// <remarks>The fixed store name keeps existing submissions reachable if this class moves namespace.</remarks>
	[EPiServerDataStore(StoreName = "ContactSubmissions", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
	public class ContactSubmission : IDynamicData
	{
		public const int NameMaxLength = 100;
		public const int EmailMaxLength = 254;
		public const int CommentMaxLength = 2000;

		public Identity Id { get; set; } = Identity.NewIdentity();

		public string Name { get; set; } = string.Empty;

		/// <summary>The email, encrypted with ASP.NET Core Data Protection. Read it through <c>IContactSubmissionService</c>.</summary>
		public string EmailProtected { get; set; } = string.Empty;

		public string Comment { get; set; } = string.Empty;

		/// <summary>The page the form was on.</summary>
		public int PageId { get; set; }

		/// <summary>The page language the visitor used, e.g. <c>sv</c>.</summary>
		public string Language { get; set; } = string.Empty;

		/// <summary>Indexed: the admin page sorts on it.</summary>
		[EPiServerDataIndex]
		public DateTime CreatedUtc { get; set; }
	}
}
