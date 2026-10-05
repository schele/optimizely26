using System.Security.Principal;
using EPiServer.Security;

namespace Optimizely26.Business
{
	/// <summary>
	/// What a visitor who isn't signed in may see: published content that Everyone or Anonymous may read. Used wherever the site
	/// shows or reveals content outside normal page routing (search results, the contact form's way back).
	/// </summary>
	public class VisitorAccess(IPublishedStateAssessor publishedStateAssessor, IContentAccessEvaluator contentAccessEvaluator)
	{
		/// <summary>No name and no roles, so only Optimizely's virtual roles Everyone and Anonymous apply.</summary>
		private static readonly IPrincipal AnonymousVisitor = new GenericPrincipal(new GenericIdentity(string.Empty), []);

		private readonly IPublishedStateAssessor _publishedStateAssessor = publishedStateAssessor;
		private readonly IContentAccessEvaluator _contentAccessEvaluator = contentAccessEvaluator;

		public bool CanSee(IContent content)
			=> _publishedStateAssessor.IsPublished(content, PublishedStateCondition.None)
				&& _contentAccessEvaluator.HasAccess(content, AnonymousVisitor, AccessLevel.Read);
	}
}
