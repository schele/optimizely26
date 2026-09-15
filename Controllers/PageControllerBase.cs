using EPiServer.Web.Mvc;
using Optimizely26.Models.Pages;

namespace Optimizely26.Controllers
{
    public abstract class PageControllerBase<T> : PageController<T>  where T : SitePageData
    {
    }
}