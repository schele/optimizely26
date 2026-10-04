# Movie page and visitor ratings (port from umbraco26)

Date: 2026-10-04
Status: Approved design, awaiting spec review

## Goal

Port the movie detail page and visitor ratings from `umbraco26` (`origin/main`, commits `07bf47d` and `37ab4f6`) to `optimizely26` (CMS 13, .NET 10).

- A movie page shows one OMDb title, looked up by IMDb id from the query string: poster, plot, cast and crew, details, OMDb ratings and awards.
- Search results on the start page link to the movie page instead of to IMDb.
- Visitors can rate a title: 0.5–5 stars in half steps, an optional name and an optional comment. The page shows the average, the number of ratings, and the ratings themselves, newest first, 10 per page.
- Ratings are stored in Optimizely's **Dynamic Data Store**. No EF Core.

**Done when:** clicking a search result opens the movie page; a submitted rating survives a site restart and appears in the average and the list.

## Decisions

| Question | Decision |
|---|---|
| How the search finds the movie page | Automatically: the first `MoviePage` under the start page (as umbraco26 does). `SettingsPage.LinkToMovies` stays unused. |
| Movie URL shape | Query string: `/movie/?id=tt0372784`. No partial router. |
| Ratings storage | Typed Dynamic Data Store class with `[EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]`. |
| Tests | No test project; verified by a clean build and a browser check (see Verification). |

## Out of scope

- Moderating, editing or deleting ratings.
- Limiting a visitor to one rating per title (umbraco26 has no such limit).
- Translating the rating box texts (they are hard-coded English in umbraco26 too).
- Pretty `/movie/{id}/` URLs.

## Design

### Movie page type

`Models/Pages/MoviePage.cs`: a new page type inheriting `SitePageData`, with no extra properties, group `Specialized`, display name "Movie Page". `StartPage`'s `[AvailableContentTypes]` include list adds `typeof(MoviePage)`, so editors can create one under the start page.

### OMDb lookup

`IOmdbService` gains `Task<OmdbMovieDetails?> GetByIdAsync(string imdbId)`:

- Calls `https://www.omdbapi.com/?i={Uri.EscapeDataString(imdbId)}&plot=full&apikey={Omdb:ApiKey}` using the existing typed `HttpClient` and System.Text.Json.
- Returns `null` on HTTP failure or exception (logged), or when OMDb answers `Response: "False"` (unknown id).

New models in `Models/Omdb/`:
- `OmdbMovieDetails`: Title, Year, Rated, Released, Runtime, Genre, Director, Writer, Actors, Plot, Language, Country, Awards, Poster, Ratings, Metascore, ImdbRating, ImdbVotes, ImdbID, Type, BoxOffice, Response.
- `OmdbRating`: Source, Value.

All string properties default to `string.Empty`, and `Ratings` defaults to `[]`. OMDb's JSON names match case-insensitively, which is System.Text.Json's web default, so no attributes are needed.

### Controller

`Controllers/MoviePageController.cs`: `PageControllerBase<MoviePage>` with `Index(MoviePage currentPage, string? id)`:

1. No id, and the request is in CMS edit or preview mode (`IContextModeResolver.CurrentMode` is `ContextMode.Edit` or `ContextMode.Preview`): render the view with `Movie = null`. The view shows a short note ("Open a movie from the search to see it here.") so editors don't land on the error page.
2. No id otherwise, or `GetByIdAsync` returns null: `NotFound()`. The existing status-code handling in `Startup` redirects to `/error`.
3. Otherwise render the view with the details.

### View model and page title

`IPageViewModel<T>` and `PageViewModel<T>` gain `PageTitle` and `MetaDescription`, defaulting to `CurrentPage.Name` and `CurrentPage.MetaDescription` (virtual on the class). `_Root.cshtml` uses `Model.PageTitle` for `<title>` and `Model.MetaDescription` for the meta description.

`Models/ViewModels/MoviePageViewModel.cs` derives from `PageViewModel<MoviePage>`, adds `OmdbMovieDetails? Movie`, and overrides `PageTitle` with `Movie?.Title` and `MetaDescription` with `Movie?.Plot`, falling back to the base values.

### View and styles

`Views/MoviePage/Index.cshtml` ports `Views/Movie.cshtml` as it is, except:
- `Model.Content.Name` becomes `Model.CurrentPage.Name`.
- The culture for the rating box is `Model.CurrentPage.Language.Name`.
- The edit-mode note when `Movie` is null.
- The hero poster falls back to the "No poster" placeholder when the image fails to load, as the search cards do. The view is server-rendered, so this is an inline `onerror` that swaps in the placeholder markup instead of a Blazor handler.

`wwwroot/css/movie.css` is copied from umbraco26 unchanged and linked from `_Root.cshtml`.

### Search wiring

- `StartPageViewModel` gains `string? MoviePageUrl`.
- `StartPageController` loads the first **published** `MoviePage` among the start page's descendants (`IPublishedStateAssessor.IsPublished`, so visitors are never linked to a page that would 404) and resolves its URL with `IUrlResolver`. The value is null when there is no such page.
- `Views/StartPage/Index.cshtml` passes `param-MoviePageUrl` to the search component.
- `OmdbSearchPage.razor` gains `[Parameter] string? MoviePageUrl`. When it is set, result links go to `{MoviePageUrl}?id={Uri.EscapeDataString(imdbId)}` in the same tab; otherwise they go to IMDb in a new tab, as now. The existing failed-poster fallback stays.

### Ratings: storage model

`Models/Ratings/MovieRating.cs`:

```csharp
[EPiServerDataStore(StoreName = "MovieFinderRatings", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
public class MovieRating : IDynamicData
{
    public const double MinScore = 0.5;
    public const double MaxScore = 5;
    public const int NameMaxLength = 100;
    public const int CommentMaxLength = 2000;

    public Identity Id { get; set; } = Identity.NewIdentity();  // EPiServer.Data is nullable-annotated
    [EPiServerDataIndex]
    public string ImdbId { get; set; } = string.Empty;
    public double Score { get; set; }               // 0.5–5 in steps of 0.5
    public string? Name { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedUtc { get; set; }
}
```

The store is created on first use and remapped automatically if the class changes. There is no migration code. The fixed `StoreName` keeps existing ratings reachable if the class is later moved to another namespace.

`MovieRatingFormModel` and `MovieRatingSummary` are ported unchanged, apart from namespaces, into `Models/Ratings/`.

### Ratings: service

`Services/Interfaces/IMovieRatingService.cs` keeps the umbraco26 signatures:

```csharp
Task<MovieRatingSummary> GetSummaryAsync(string imdbId, int page, int pageSize);
Task<MovieRating> AddAsync(string imdbId, double score, string? name, string? comment);
```

`Services/MovieRatingService.cs` takes `DynamicDataStoreFactory` and gets the store on every call (`GetStore(typeof(MovieRating)) ?? CreateStore(typeof(MovieRating))`), because `DynamicDataStore` instances are not thread-safe and Blazor calls can overlap. The Dynamic Data Store has no async API, so the methods run synchronously and return completed tasks.

- **GetSummaryAsync:** `Items<MovieRating>().Where(x => x.ImdbId == imdbId)`. It loads the scores with `Select(x => x.Score)` and averages them in memory, rounded to 1 decimal (null when there are none). It loads one page with `OrderByDescending(x => x.CreatedUtc).Skip((max(page, 1) - 1) * pageSize).Take(pageSize)`. Count is the number of scores.
- **AddAsync:** throws `ArgumentException` on an empty id. It snaps the score to half stars and clamps it to `MinScore`–`MaxScore`, trims and cuts name and comment to their limits (empty becomes null), trims the id, sets `CreatedUtc = DateTime.UtcNow`, calls `Save`, and returns the rating.

Registered in `Startup.ConfigureServices` as `services.AddScoped<IMovieRatingService, MovieRatingService>()`.

**Dates:** the store returns `CreatedUtc` with `DateTimeKind.Unspecified`. `ToLocalTime()` treats Unspecified as UTC, so the displayed date is correct, matching umbraco26.

### Ratings: Blazor component

`Blazor/MovieRatingBox.razor` ports `MovieRatingBox.razor` unchanged apart from namespaces and `@inject` style: the star slider in half steps, the optional name and comment, validation, the thank-you message, paging with a page-number window of 2, and newest first. The movie view renders it with `<component type="typeof(MovieRatingBox)" render-mode="Server" param-ImdbId=… param-CultureName=… />` when the movie has an IMDb id.

### Files

| New | Changed |
|---|---|
| `Models/Pages/MoviePage.cs` | `Models/Pages/StartPage.cs` (allowed child types) |
| `Models/Omdb/OmdbMovieDetails.cs`, `OmdbRating.cs` | `Services/Interfaces/IOmdbService.cs`, `Services/OmdbService.cs` (`GetByIdAsync`) |
| `Models/Ratings/MovieRating.cs`, `MovieRatingFormModel.cs`, `MovieRatingSummary.cs` | `Models/ViewModels/IPageViewModel.cs`, `PageViewModel.cs` (`PageTitle`, `MetaDescription`) |
| `Models/ViewModels/MoviePageViewModel.cs` | `Models/ViewModels/StartPageViewModel.cs`, `Controllers/StartPageController.cs` (`MoviePageUrl`) |
| `Controllers/MoviePageController.cs` | `Views/Shared/Layouts/_Root.cshtml` (title, description, `movie.css`) |
| `Views/MoviePage/Index.cshtml` | `Views/StartPage/Index.cshtml`, `Blazor/OmdbSearchPage.razor` (`MoviePageUrl`) |
| `Services/Interfaces/IMovieRatingService.cs`, `Services/MovieRatingService.cs` | `Startup.cs` (register `IMovieRatingService`) |
| `Blazor/MovieRatingBox.razor`, `wwwroot/css/movie.css` | |

## Verification

1. A clean rebuild (`--no-incremental`) with 0 warnings and 0 errors.
2. Browser, after a site restart:
   - Create a Movie Page under the start page and publish it.
   - Search "batman" and click a result. The movie page shows the details, and `<title>` is the movie title.
   - Open `/movie/?id=tt0000000` (unknown id) and `/movie/` (no id); both end on `/error`.
   - Submit a rating with a name and comment. It appears with the average and count, and the form is replaced by the thank-you message.
   - Restart the site. The rating is still there.
   - More than 10 ratings on one title: paging appears and works.
   - Edit mode: open the Movie Page in the CMS editor. It shows the note, not the error page.
