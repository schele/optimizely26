using EPiServer.Web.Routing;

namespace Optimizely26.Business.Extensions
{
    public static class PageDataExtensions
    {
        public static string GetExternalUrl(this IContent content)
        {
            return UrlResolver.Current.GetUrl(
                content.ContentLink,
                null,
                new VirtualPathArguments { ForceAbsolute = true });
        }
        
        public static string Url(this string url)
        {
            return UrlResolver.Current.GetUrl(url);
        }
    }
}