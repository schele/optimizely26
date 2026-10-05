using Optimizely26.Business;
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Blocks
{
	/// <summary>A contact form: name, email and comment. Submissions are listed under "Contact submissions" in the CMS menu.</summary>
	[ContentType(
		GUID = "563EDD1A-B8C6-484C-974B-A3C30F6C229B",
		DisplayName = "Contact Form Block",
		GroupName = Globals.GroupNames.Specialized,
		Description = "A contact form with name, email and comment. Submissions are listed under Contact submissions."
	)]
	public class ContactFormBlock : BlockData
	{
		[Display(
			GroupName = SystemTabNames.Content,
			Order = 10
		)]
		[CultureSpecific]
		public virtual string? Heading { get; set; }

		[Display(
			GroupName = SystemTabNames.Content,
			Order = 20
		)]
		[CultureSpecific]
		[UIHint(EPiServer.Web.UIHint.Textarea)]
		public virtual string? Intro { get; set; }
	}
}
