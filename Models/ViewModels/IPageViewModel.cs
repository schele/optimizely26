using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public interface IPageViewModel<out T> where T : SitePageData
    {
        T CurrentPage { get; }

        LayoutModel? Layout { get; set; }

        /// <summary>The page's &lt;title&gt;.</summary>
        string PageTitle { get; }

        /// <summary>The page's meta description.</summary>
        string MetaDescription { get; }
    }
}