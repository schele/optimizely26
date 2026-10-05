using EPiServer.Authorization;
using EPiServer.Shell.Navigation;
using Optimizely26.Controllers;

namespace Optimizely26.Business.ContactForm
{
	/// <summary>Adds "Contact submissions" to the CMS top menu, for CMS admins.</summary>
	[MenuProvider]
	public class ContactSubmissionsMenuProvider : IMenuProvider
	{
		public IEnumerable<MenuItem> GetMenuItems()
		{
			return
			[
				new UrlMenuItem("Contact submissions", MenuPaths.Global + "/cms/contactsubmissions", "/" + ContactSubmissionsController.Path)
				{
					SortIndex = 500,
					AuthorizationPolicy = CmsPolicyNames.CmsAdmin,
				},
			];
		}
	}
}
