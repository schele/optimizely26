using Optimizely26.Models.Contact;

namespace Optimizely26.Services
{
	public interface IContactSubmissionService
	{
		/// <summary>Stores a submission; the email is encrypted before it's saved.</summary>
		void Add(string name, string email, string comment, int pageId, string language);

		/// <summary>Page <paramref name="page"/> (1-based) of the submissions, newest first, with the emails decrypted.</summary>
		ContactSubmissionPage GetPage(int page, int pageSize);
	}
}
