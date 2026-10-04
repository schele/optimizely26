using Optimizely26.Models.Omdb;
using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
	public class MoviePageViewModel : PageViewModel<MoviePage>
	{
		public MoviePageViewModel(MoviePage currentPage, OmdbMovieDetails? movie) : base(currentPage)
		{
			Movie = movie;
		}

		/// <summary>The title from OMDb; null only in edit or preview mode, where the page is opened without an id.</summary>
		public OmdbMovieDetails? Movie { get; }

		public override string PageTitle => Movie?.Title ?? base.PageTitle;

		public override string MetaDescription => Movie?.Plot ?? base.MetaDescription;
	}
}
