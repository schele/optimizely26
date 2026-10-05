using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Contact
{
	/// <summary>What the contact form posts.</summary>
	public class ContactFormPost
	{
		[Required]
		[StringLength(ContactSubmission.NameMaxLength)]
		public string? Name { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(ContactSubmission.EmailMaxLength)]
		public string? Email { get; set; }

		[Required]
		[StringLength(ContactSubmission.CommentMaxLength)]
		public string? Comment { get; set; }

		/// <summary>The honeypot: hidden from people, so anything in it was typed by a bot.</summary>
		public string? Website { get; set; }

		/// <summary>The signed token from <c>ContactFormToken</c>.</summary>
		public string? FormToken { get; set; }

		/// <summary>The page the form is on; only used to find the way back, and checked against the token.</summary>
		public int PageId { get; set; }

		public string Language { get; set; } = string.Empty;
	}
}
