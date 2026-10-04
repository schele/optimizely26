using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public class StartPageViewModel : PageViewModel<StartPage>
    {
        public StartPageViewModel(StartPage currentPage) : base(currentPage)
        {
        }

        /// <summary>The first published movie page; null when there is none, and results link to IMDb.</summary>
        public string? MoviePageUrl { get; init; }
    }
}
