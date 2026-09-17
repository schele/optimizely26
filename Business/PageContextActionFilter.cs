using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Business
{
    public class PageContextActionFilter : IActionFilter
    {
        private readonly PageViewContextFactory _pageViewContextFactory;

        public PageContextActionFilter(PageViewContextFactory pageViewContextFactory)
        {
            _pageViewContextFactory = pageViewContextFactory;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var controller = context.Controller as Controller;
            var viewModel = controller?.ViewData.Model;

            if (viewModel is IPageViewModel<SitePageData> model)
            {
                var currentContentLink = context.HttpContext.GetContentLink();
                var layoutModel = model.Layout ?? _pageViewContextFactory.GetLayoutModel(currentContentLink, context.HttpContext);
                
                if (context.Controller is IModifyLayout layoutController)
                {
                    layoutController.ModifyLayout(layoutModel);
                }

                model.Layout = layoutModel;
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}
