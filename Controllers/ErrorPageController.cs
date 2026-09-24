using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Controllers
{
    public class ErrorPageController : PageControllerBase<ErrorPage>
    {
        public IActionResult Index(ErrorPage currentPage)
        {
            var viewModel = new ErrorPageViewModel(currentPage);

            return View(viewModel);
        }
    }
}