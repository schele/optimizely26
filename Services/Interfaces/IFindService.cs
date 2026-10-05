using System.Globalization;
using Optimizely26.Models.Find;

namespace Optimizely26.Services
{
	public interface IFindService
	{
		/// <summary>Searches published pages in <paramref name="culture"/>; <paramref name="page"/> is 1-based.</summary>
		/// <exception cref="Exception">The search index can't be read, for example while it is locked by another process.</exception>
		FindResult FindContent(string query, CultureInfo culture, int page, int pageSize);
	}
}
