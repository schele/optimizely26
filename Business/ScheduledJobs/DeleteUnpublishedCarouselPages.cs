using EPiServer.Globalization;
using EPiServer.Scheduler;
using Optimizely26.Models.Pages;

namespace Optimizely26.Business.ScheduledJobs
{


	[ScheduledJob(
		GUID = "C281D755-A9FD-4756-9A4B-85EB0AB760BD",
		DisplayName = "Delete Unpublished Carousel Pages",
		Description = "Deletes all unpublished carousel pages"
	)]
	public class DeleteUnpublishedCarouselPages : ScheduledJobBase
	{
		private readonly IContentLoader _contentLoader;
		private readonly IContentRepository _contentRepository;
		private bool _stopSignaled;

		public DeleteUnpublishedCarouselPages(IContentLoader contentLoader, IContentRepository contentRepository)
		{
			_contentLoader = contentLoader;			
			_contentRepository = contentRepository;
			IsStoppable = true;
		}

		public override void Stop()
		{
			_stopSignaled = true;
		}

		public override string Execute()
		{
			_stopSignaled = false;

			var carouselPages = GetCarouselPages().ToList();
			var deleted = 0;

			foreach (var item in carouselPages)
			{
				if (_stopSignaled)
				{
					return $"Scheduled job stopped after deleting {deleted} carousel page(s).";
				}

				if (!item.CheckPublishedStatus(PagePublishedStatus.Published))
				{
					_contentRepository.Delete(item.ContentLink, true, EPiServer.Security.AccessLevel.NoAccess);

					deleted++;
				}
			}

			return $"Scheduled job completed successfully. Deleted {deleted} unpublished carousel page(s).";
		}

		private List<CarouselPage> GetCarouselPages()
		{
			var startPage = _contentLoader.Get<StartPage>(ContentReference.StartPage);
			var refs = _contentLoader.GetDescendents(startPage.ContentLink);
			var carouselPages = _contentLoader
				.GetItems(refs, [LanguageLoaderOption.MasterLanguage()])
				.OfType<CarouselPage>()
				.ToList();

			return carouselPages;
		}
	}
}
