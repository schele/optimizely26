using EPiServer.Scheduler;
using EPiServer.Web;
using Optimizely26.Models.Pages;

namespace Optimizely26.Business.ScheduledJobs
{
    [ScheduledJob(
        GUID = "30C22477-367B-474E-888F-B15377A9D6DC",
        DisplayName = "Delete Unpublished Carousel Pages",
        Description = "Deletes all unpublished carousel pages"
    )]
    public class DeleteUnpublishedCarouselPages : ScheduledJobBase
    {
        private readonly IContentLoader _contentLoader;
        private readonly IContentRepository _contentRepository;
        private bool _stopSignaled;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;

        public DeleteUnpublishedCarouselPages(IContentLoader contentLoader, IContentRepository contentRepository, ISiteDefinitionRepository siteDefinitionRepository)
        {
            _contentLoader = contentLoader;
            _contentRepository = contentRepository;
            _siteDefinitionRepository = siteDefinitionRepository;
            IsStoppable = true;
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        public override string Execute()
        {
            _stopSignaled = false;

            var carouselPages = GetCarouselPages();
            var deleted = 0;

            foreach (var item in carouselPages)
            {
                if (_stopSignaled)
                {
                    return $"Job stopped. Deleted {deleted} unpublished carousel pages.";
                }

                if (!item.CheckPublishedStatus(PagePublishedStatus.Published))
                {
                    _contentRepository.Delete(item.ContentLink, true, EPiServer.Security.AccessLevel.NoAccess);

                    deleted++;
                }
            }

            return $"Job completed. Deleted {deleted} unpublished carousel pages.";
        }

        private List<CarouselPage> GetCarouselPages()
        {
            var refs = _siteDefinitionRepository.List()
                .SelectMany(site => _contentLoader.GetDescendents(site.StartPage))
                .Distinct();

            return _contentLoader
                .GetItems(refs, [LanguageLoaderOption.MasterLanguage()])
                .OfType<CarouselPage>()
                .ToList();
        }

    }
}
