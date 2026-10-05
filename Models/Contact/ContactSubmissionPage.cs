namespace Optimizely26.Models.Contact
{
	/// <summary>A submission as the admin page shows it. <see cref="Email"/> is null when it can't be decrypted.</summary>
	public record ContactSubmissionView(DateTime CreatedUtc, string Name, string? Email, string Comment, string PageName, string Language);

	/// <summary>One page of submissions, newest first. <see cref="Page"/> is the page actually returned.</summary>
	public record ContactSubmissionPage(IReadOnlyList<ContactSubmissionView> Items, int TotalCount, int Page, int TotalPages);
}
