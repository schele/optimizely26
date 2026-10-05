using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
	public class FindPageViewModel : PageViewModel<FindPage>
	{
		public FindPageViewModel(FindPage currentPage) : base(currentPage)
		{
		}

		/// <summary>The search from the address bar (<c>?q=</c>); empty when the page is opened without one.</summary>
		public string Query { get; init; } = string.Empty;

		/// <summary>The result page from the address bar (<c>?page=</c>), 1 or more.</summary>
		public int Page { get; init; } = 1;
	}
}
