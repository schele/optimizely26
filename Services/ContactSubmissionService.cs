using System.Globalization;
using System.Security.Cryptography;
using EPiServer.Data.Dynamic;
using Microsoft.AspNetCore.DataProtection;
using Optimizely26.Models.Contact;

namespace Optimizely26.Services
{
	/// <remarks>A Dynamic Data Store instance isn't thread-safe, so every call gets its own.</remarks>
	public class ContactSubmissionService(
		DynamicDataStoreFactory dataStoreFactory,
		IDataProtectionProvider dataProtectionProvider,
		IContentLoader contentLoader,
		ILogger<ContactSubmissionService> logger) : IContactSubmissionService
	{
		private readonly DynamicDataStoreFactory _dataStoreFactory = dataStoreFactory;
		private readonly IDataProtector _emailProtector = dataProtectionProvider.CreateProtector("Optimizely26.ContactSubmission.Email");
		private readonly IContentLoader _contentLoader = contentLoader;
		private readonly ILogger<ContactSubmissionService> _logger = logger;

		public void Add(string name, string email, string comment, int pageId, string language)
		{
			GetStore().Save(new ContactSubmission
			{
				Name = name,
				EmailProtected = _emailProtector.Protect(email),
				Comment = comment,
				PageId = pageId,
				Language = language,
				CreatedUtc = DateTime.UtcNow,
			});
		}

		public ContactSubmissionPage GetPage(int page, int pageSize)
		{
			var store = GetStore();
			var totalCount = store.Items<ContactSubmission>().Count();
			var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
			page = Math.Clamp(page, 1, totalPages);

			var items = store.Items<ContactSubmission>()
				.OrderByDescending(x => x.CreatedUtc)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList()
				.Select(x => new ContactSubmissionView(x.CreatedUtc, x.Name, Decrypt(x), x.Comment, GetPageName(x.PageId, x.Language), x.Language))
				.ToList();

			return new ContactSubmissionPage(items, totalCount, page, totalPages);
		}

		private DynamicDataStore GetStore() => _dataStoreFactory.GetStore(typeof(ContactSubmission)) ?? _dataStoreFactory.CreateStore(typeof(ContactSubmission));

		private string? Decrypt(ContactSubmission submission)
		{
			try
			{
				return _emailProtector.Unprotect(submission.EmailProtected);
			}
			catch (CryptographicException e)
			{
				// The key it was encrypted with is gone, for example after App_Data/DataProtection-Keys was deleted
				_logger.LogWarning(e, "The email of contact submission {Id} can't be decrypted", submission.Id);
				return null;
			}
		}

		private string GetPageName(int pageId, string language)
		{
			var culture = CultureInfo.GetCultureInfo(language);

			return _contentLoader.TryGet<IContent>(new ContentReference(pageId), new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(culture) }, out var content)
				? content.Name
				: $"#{pageId}";
		}
	}
}
