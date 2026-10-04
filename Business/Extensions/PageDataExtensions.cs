using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Optimizely26.Business.Extensions
{
	public static class PageDataExtensions
	{
		public static string? GetExternalUrl(this IContent content)
		{
			var internalUrl = UrlResolver.Current.GetUrl(content.ContentLink);

			if (internalUrl != null)
			{
				var url = new UrlBuilder(internalUrl);
				var friendlyUrl = ServiceLocator.Current.GetInstance<IUriSupport>().AbsoluteUrlBySettings(url.ToString());

				return friendlyUrl;
			}

			return null;
		}

		public static string Url(this string url)
		{
			return UrlResolver.Current.GetUrl(url);
		}
	}
}
