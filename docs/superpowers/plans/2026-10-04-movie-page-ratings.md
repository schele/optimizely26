# Movie Page and Ratings Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port umbraco26's OMDb movie detail page and visitor star ratings to optimizely26, storing ratings in Optimizely's Dynamic Data Store instead of EF Core.

**Architecture:** A new `MoviePage` page type reads `?id={imdbId}`, loads the title through `IOmdbService.GetByIdAsync`, and renders a server-side view that embeds a Blazor Server `MovieRatingBox`. Ratings are a typed Dynamic Data Store class behind `IMovieRatingService`. The start page finds the first published `MoviePage` and passes its URL to the existing Blazor search, so results link to it.

**Tech Stack:** Optimizely CMS 13.0.0 (EPiServer.CMS), .NET 10, ASP.NET Core MVC + Razor, Blazor Server (already set up: `AddServerSideBlazor`, `MapBlazorHub`), EPiServer.Data.Dynamic (Dynamic Data Store), System.Text.Json, Bootstrap 5.3.8, Font Awesome 6.5.

**Spec:** `docs/superpowers/specs/2026-10-04-movie-page-ratings-design.md`

**Source being ported:** `C:\src\umbraco26`, branch `origin/main` (read with `git -C C:/src/umbraco26 show origin/main:Umbraco26/<path>`; the local clone is behind, do not pull it).

## Global Constraints

- Target: `EPiServer.CMS` 13.0.0, `net10.0`, `<Nullable>enable</Nullable>`. Every build must end with **0 Warning(s), 0 Error(s)**.
- No EF Core, no `DbContext`, no migrations. Ratings live in the Dynamic Data Store.
- Movie URL shape: query string, `/movie/?id={imdbId}`. No partial router.
- Ratings: score 0.5–5 in steps of 0.5; name ≤ 100 chars; comment ≤ 2000 chars; 10 ratings per page, newest first; page-number window 2.
- The OMDb key is read from configuration `Omdb:ApiKey`. Never hard-code it. **Never stage or commit `appsettings.json`** (it holds the key locally).
- Style: match the file you're editing. New C# files use tabs and primary constructors with `private readonly` fields, like `Services/XmlSitemapService.cs`. New views use 4 spaces.
- Work on branch `local-wip`. Commit messages are imperative sentence case (e.g. "Add a movie page fed by OMDb") and end with the trailer `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>`.
- No test project exists and none is added (spec decision). Each task is verified by a build; the browser checks are in Task 5.

### Build command

The site is often running and locks `bin\Debug`, so build into a scratch folder:

```bash
cd C:/src/optimizely26
dotnet build --nologo --no-incremental -v q -p:OutDir="$TEMP/optimizely26-build/" 2>&1 | grep -E "warning|error|Warning\(s\)|Error\(s\)"
```

Expected output ends with:

```
    0 Warning(s)
    0 Error(s)
```

## Review Focus

1. **Movie page opened without `?id`, or with an unknown id** — visitors end on `/error`; editors in edit/preview mode see "Open a movie from the search to see it here." (Task 3 controller; checked in Task 5.)
2. **Rating comment containing HTML or line breaks** (e.g. `<b>great</b>` on two lines) — shown as literal text with the line break kept, never rendered as HTML. (Blazor encodes; `.movie-rating-comment { white-space: pre-line }` from Task 3's CSS; checked in Task 5.)
3. **Movie page unpublished or deleted** — search results fall back to IMDb links instead of linking visitors to a 404. (Task 4 `IsPublished` filter; checked in Task 5.)
4. **Poster URL that fails to load on the movie page** — the "No poster" placeholder appears instead of a broken image. (Task 3 view; checked in Task 5.)
5. **Paging boundary** — exactly 10 ratings shows no pager; the 11th rating adds page 2, and the new rating appears first on page 1. (Task 2 service + component; checked in Task 5.)

---

### Task 0: Commit the pending work as a baseline

Everything since the last push to `local-wip` is uncommitted: build-warning fixes, the umbraco26 markup and movie-search port, the failed-poster fallback, and these docs. Commit it in three logical commits so the feature commits that follow are reviewable on their own.

**Files:** no edits; commits only. `appsettings.json` stays modified and unstaged.

- [ ] **Step 1: Confirm the branch and that the tree builds**

```bash
cd C:/src/optimizely26
git branch --show-current
```

Expected: `local-wip`. Then run the [build command](#build-command). Expected: 0 warnings, 0 errors.

- [ ] **Step 2: Commit the warning fixes**

```bash
git add Business/PageViewContextFactory.cs Models/Blocks/CarouselBlock.cs Models/Pages/CarouselPage.cs Models/Pages/SettingsPage.cs Models/Pages/StartPage.cs Models/ViewModels/IPageViewModel.cs Models/ViewModels/PageViewModel.cs Models/ViewModels/XmlSitemapViewModel.cs Views/XmlSitemap/Index.cshtml
git commit -F - <<'EOF'
Fix build warnings

Use IApplicationResolver instead of the obsolete SiteDefinition.Current,
ContentReference instead of PageReference, nullable content properties,
and the view's own HttpContext in the XML sitemap.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
```

- [ ] **Step 3: Commit the umbraco26 markup and movie search**

```bash
git add Blazor Models/Omdb Models/ViewModels/CarouselViewModel.cs Resources/Translations/Views.xml Services/Interfaces/IOmdbService.cs Services/OmdbService.cs Startup.cs Views/ErrorPage/Index.cshtml Views/Shared/Layouts/_Root.cshtml Views/Shared/carousel.cshtml Views/StartPage/Index.cshtml Views/_ViewImports.cshtml wwwroot/css
git commit -F - <<'EOF'
Port the umbraco26 markup and Blazor movie search

Movie Finder layout, carousel partial and styles, error page text, and
the OMDb search component with its service. The OMDb key is read from
configuration (Omdb:ApiKey). Posters that fail to load fall back to the
no-poster placeholder.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
```

- [ ] **Step 4: Commit the spec and this plan**

```bash
git add docs
git commit -F - <<'EOF'
Add the movie page and ratings spec and plan

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
git status --short
```

Expected `git status` output: only ` M appsettings.json`.

---

### Task 1: Look up a single OMDb title

**Files:**
- Create: `Models/Omdb/OmdbRating.cs`
- Create: `Models/Omdb/OmdbMovieDetails.cs`
- Modify: `Services/Interfaces/IOmdbService.cs`
- Modify: `Services/OmdbService.cs` (full replacement below)

**Interfaces:**
- Consumes: existing `OmdbService(HttpClient, IConfiguration, ILogger<OmdbService>)` registered via `services.AddHttpClient<IOmdbService, OmdbService>()`; existing `OmdbSearchResponse`, `OmdbSearchModel`, `OmdbMovie` in `Optimizely26.Models.Omdb`.
- Produces: `Task<OmdbMovieDetails?> IOmdbService.GetByIdAsync(string imdbId)` (null when unknown or failed); `Optimizely26.Models.Omdb.OmdbMovieDetails` with string properties `Title, Year, Rated, Released, Runtime, Genre, Director, Writer, Actors, Plot, Language, Country, Awards, Poster, Metascore, ImdbRating, ImdbVotes, ImdbID, Type, BoxOffice, Response` and `List<OmdbRating> Ratings`; `OmdbRating` with `Source`, `Value`.

- [ ] **Step 1: Create `Models/Omdb/OmdbRating.cs`**

```csharp
namespace Optimizely26.Models.Omdb
{
	/// <summary>One of the outside ratings OMDb lists for a title, e.g. Rotten Tomatoes "91%".</summary>
	public class OmdbRating
	{
		public string Source { get; set; } = string.Empty;

		public string Value { get; set; } = string.Empty;
	}
}
```

- [ ] **Step 2: Create `Models/Omdb/OmdbMovieDetails.cs`**

```csharp
namespace Optimizely26.Models.Omdb
{
	/// <summary>A single title from the OMDb API (<c>?i={imdbId}</c>). OMDb uses "N/A" for missing values.</summary>
	public class OmdbMovieDetails
	{
		public string Title { get; set; } = string.Empty;

		public string Year { get; set; } = string.Empty;

		public string Rated { get; set; } = string.Empty;

		public string Released { get; set; } = string.Empty;

		public string Runtime { get; set; } = string.Empty;

		public string Genre { get; set; } = string.Empty;

		public string Director { get; set; } = string.Empty;

		public string Writer { get; set; } = string.Empty;

		public string Actors { get; set; } = string.Empty;

		public string Plot { get; set; } = string.Empty;

		public string Language { get; set; } = string.Empty;

		public string Country { get; set; } = string.Empty;

		public string Awards { get; set; } = string.Empty;

		public string Poster { get; set; } = string.Empty;

		public List<OmdbRating> Ratings { get; set; } = [];

		public string Metascore { get; set; } = string.Empty;

		public string ImdbRating { get; set; } = string.Empty;

		public string ImdbVotes { get; set; } = string.Empty;

		public string ImdbID { get; set; } = string.Empty;

		public string Type { get; set; } = string.Empty;

		public string BoxOffice { get; set; } = string.Empty;

		/// <summary>"True", or "False" when OMDb doesn't know the id.</summary>
		public string Response { get; set; } = string.Empty;
	}
}
```

OMDb's JSON names (`imdbID`, `imdbRating`, …) match these properties case-insensitively, which is the default for `GetFromJsonAsync`, so no attributes are needed.

- [ ] **Step 3: Add `GetByIdAsync` to `Services/Interfaces/IOmdbService.cs`**

Replace the file with:

```csharp
using Optimizely26.Models.Omdb;

namespace Optimizely26.Services
{
	public interface IOmdbService
	{
		Task<List<OmdbMovie>> SearchAsync(OmdbSearchModel search);

		/// <summary>The full details of one title, or null when OMDb doesn't know the id or can't be reached.</summary>
		Task<OmdbMovieDetails?> GetByIdAsync(string imdbId);
	}
}
```

- [ ] **Step 4: Implement it in `Services/OmdbService.cs`**

Replace the file with (the URL building moves into `ApiUrl`, shared by both calls):

```csharp
using Optimizely26.Models.Omdb;

namespace Optimizely26.Services
{
	public class OmdbService(HttpClient httpClient, IConfiguration configuration, ILogger<OmdbService> logger) : IOmdbService
	{
		private readonly HttpClient _httpClient = httpClient;
		private readonly IConfiguration _configuration = configuration;
		private readonly ILogger<OmdbService> _logger = logger;

		public async Task<List<OmdbMovie>> SearchAsync(OmdbSearchModel search)
		{
			try
			{
				var result = await _httpClient.GetFromJsonAsync<OmdbSearchResponse>(ApiUrl($"s={Uri.EscapeDataString(search.Query)}"));

				if (result != null)
				{
					return result.Search;
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "OMDb search for {Query} failed", search.Query);
			}

			return [];
		}

		public async Task<OmdbMovieDetails?> GetByIdAsync(string imdbId)
		{
			try
			{
				var movie = await _httpClient.GetFromJsonAsync<OmdbMovieDetails>(ApiUrl($"i={Uri.EscapeDataString(imdbId)}&plot=full"));

				// OMDb answers unknown ids with 200 and Response "False"
				if (movie != null && movie.Response == "True")
				{
					return movie;
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "OMDb lookup of {ImdbId} failed", imdbId);
			}

			return null;
		}

		private string ApiUrl(string query) => $"https://www.omdbapi.com/?{query}&apikey={_configuration["Omdb:ApiKey"]}";
	}
}
```

- [ ] **Step 5: Build**

Run the [build command](#build-command). Expected: 0 warnings, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Models/Omdb/OmdbRating.cs Models/Omdb/OmdbMovieDetails.cs Services/Interfaces/IOmdbService.cs Services/OmdbService.cs
git commit -F - <<'EOF'
Look up a single title from OMDb by IMDb id

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 2: Store ratings in the Dynamic Data Store

**Files:**
- Create: `Models/Ratings/MovieRating.cs`
- Create: `Models/Ratings/MovieRatingFormModel.cs`
- Create: `Models/Ratings/MovieRatingSummary.cs`
- Create: `Services/Interfaces/IMovieRatingService.cs`
- Create: `Services/MovieRatingService.cs`
- Create: `Blazor/MovieRatingBox.razor`
- Modify: `Startup.cs` (register the service)

**Interfaces:**
- Consumes: `EPiServer.Data.Dynamic.DynamicDataStoreFactory` (registered by `AddCms()`).
- Produces: `Optimizely26.Models.Ratings.MovieRating` (constants `MinScore = 0.5`, `MaxScore = 5`, `NameMaxLength = 100`, `CommentMaxLength = 2000`; properties `Identity Id`, `string ImdbId`, `double Score`, `string? Name`, `string? Comment`, `DateTime CreatedUtc`); `MovieRatingSummary` (`double? Average`, `int Count`, `List<MovieRating> Ratings`); `IMovieRatingService` with `Task<MovieRatingSummary> GetSummaryAsync(string imdbId, int page, int pageSize)` and `Task<MovieRating> AddAsync(string imdbId, double score, string? name, string? comment)`; Blazor component `Optimizely26.Blazor.MovieRatingBox` with parameters `string ImdbId` (required) and `string CultureName`.

- [ ] **Step 1: Create `Models/Ratings/MovieRating.cs`**

```csharp
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Optimizely26.Models.Ratings
{
	/// <summary>A visitor's MovieFinder rating of a title, stored in the Dynamic Data Store.</summary>
	/// <remarks>The fixed store name keeps existing ratings reachable if this class moves namespace.</remarks>
	[EPiServerDataStore(StoreName = "MovieFinderRatings", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
	public class MovieRating : IDynamicData
	{
		public const double MinScore = 0.5;
		public const double MaxScore = 5;
		public const int NameMaxLength = 100;
		public const int CommentMaxLength = 2000;

		public Identity Id { get; set; } = Identity.NewIdentity();

		/// <summary>The OMDb/IMDb id, e.g. <c>tt0111161</c>. Indexed: every query filters on it.</summary>
		[EPiServerDataIndex]
		public string ImdbId { get; set; } = string.Empty;

		/// <summary>0.5–5 stars in steps of 0.5.</summary>
		public double Score { get; set; }

		public string? Name { get; set; }

		public string? Comment { get; set; }

		public DateTime CreatedUtc { get; set; }
	}
}
```

- [ ] **Step 2: Create `Models/Ratings/MovieRatingFormModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace Optimizely26.Models.Ratings
{
	public class MovieRatingFormModel
	{
		/// <summary>0 until the visitor has picked a rating.</summary>
		[Range(MovieRating.MinScore, MovieRating.MaxScore, ErrorMessage = "Slide over the stars to pick a rating.")]
		public double Score { get; set; }

		[StringLength(MovieRating.NameMaxLength)]
		public string? Name { get; set; }

		[StringLength(MovieRating.CommentMaxLength)]
		public string? Comment { get; set; }
	}
}
```

- [ ] **Step 3: Create `Models/Ratings/MovieRatingSummary.cs`**

```csharp
namespace Optimizely26.Models.Ratings
{
	/// <summary>The MovieFinder rating of a title: the average score and one page of ratings, newest first.</summary>
	public class MovieRatingSummary
	{
		public double? Average { get; init; }

		/// <summary>The number of ratings in total, across all pages.</summary>
		public int Count { get; init; }

		public List<MovieRating> Ratings { get; init; } = [];
	}
}
```

- [ ] **Step 4: Create `Services/Interfaces/IMovieRatingService.cs`**

```csharp
using Optimizely26.Models.Ratings;

namespace Optimizely26.Services
{
	public interface IMovieRatingService
	{
		Task<MovieRatingSummary> GetSummaryAsync(string imdbId, int page, int pageSize);

		Task<MovieRating> AddAsync(string imdbId, double score, string? name, string? comment);
	}
}
```

- [ ] **Step 5: Create `Services/MovieRatingService.cs`**

```csharp
using EPiServer.Data.Dynamic;
using Optimizely26.Models.Ratings;

namespace Optimizely26.Services
{
	/// <remarks>
	/// The Dynamic Data Store has no async API, so these run synchronously behind the Task-based interface.
	/// A store instance isn't thread-safe, so every call gets its own.
	/// </remarks>
	public class MovieRatingService(DynamicDataStoreFactory dataStoreFactory) : IMovieRatingService
	{
		private readonly DynamicDataStoreFactory _dataStoreFactory = dataStoreFactory;

		public Task<MovieRatingSummary> GetSummaryAsync(string imdbId, int page, int pageSize)
		{
			var ratings = GetStore().Items<MovieRating>().Where(x => x.ImdbId == imdbId);

			// Averaged in memory, like umbraco26, so rounding is the same
			var scores = ratings.Select(x => x.Score).ToList();
			var pageRatings = ratings
				.OrderByDescending(x => x.CreatedUtc)
				.Skip((Math.Max(page, 1) - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			return Task.FromResult(new MovieRatingSummary
			{
				Average = scores.Count > 0 ? Math.Round(scores.Average(), 1) : null,
				Count = scores.Count,
				Ratings = pageRatings,
			});
		}

		public Task<MovieRating> AddAsync(string imdbId, double score, string? name, string? comment)
		{
			if (string.IsNullOrWhiteSpace(imdbId))
			{
				throw new ArgumentException("An IMDb id is required.", nameof(imdbId));
			}

			// NaN or infinity would survive Math.Clamp and be stored as is
			if (!double.IsFinite(score))
			{
				throw new ArgumentOutOfRangeException(nameof(score), score, "The score must be a number.");
			}

			// Snap to half stars within range, whatever the client sent
			score = Math.Clamp(Math.Round(score * 2, MidpointRounding.AwayFromZero) / 2, MovieRating.MinScore, MovieRating.MaxScore);

			var rating = new MovieRating
			{
				ImdbId = imdbId.Trim(),
				Score = score,
				Name = Truncate(name, MovieRating.NameMaxLength),
				Comment = Truncate(comment, MovieRating.CommentMaxLength),
				CreatedUtc = DateTime.UtcNow,
			};

			GetStore().Save(rating);

			return Task.FromResult(rating);
		}

		private DynamicDataStore GetStore()
			=> _dataStoreFactory.GetStore(typeof(MovieRating)) ?? _dataStoreFactory.CreateStore(typeof(MovieRating));

		private static string? Truncate(string? value, int maxLength)
		{
			value = value?.Trim();

			if (string.IsNullOrEmpty(value))
			{
				return null;
			}

			return value.Length > maxLength ? value[..maxLength] : value;
		}
	}
}
```

- [ ] **Step 6: Register the service in `Startup.cs`**

In `ConfigureServices`, after `services.AddHttpClient<IOmdbService, OmdbService>();`, add:

```csharp
            services.AddScoped<IMovieRatingService, MovieRatingService>();
```

(`Optimizely26.Services` is already imported.)

- [ ] **Step 7: Create `Blazor/MovieRatingBox.razor`**

Ported from `umbraco26` `Blazor/MovieRatingBox.razor`; only the `@using` lines and the injection changed.

```razor
@using System.Globalization
@using Optimizely26.Models.Ratings
@using Optimizely26.Services
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Web

@inject IMovieRatingService _movieRatingService

@code {
    private const int PageSize = 10;

    /// <summary>How many page numbers to show on each side of the current page.</summary>
    private const int PageWindow = 2;

    [Parameter, EditorRequired]
    public string ImdbId { get; set; } = string.Empty;

    [Parameter]
    public string CultureName { get; set; } = string.Empty;

    private CultureInfo CultureInfo { get; set; } = CultureInfo.InvariantCulture;
    private MovieRatingSummary? Summary;
    private MovieRatingFormModel RatingModel = new MovieRatingFormModel();
    private bool IsSaving = false;
    private bool HasRated = false;
    private int CurrentPage = 1;
    private int TotalPages => Summary == null ? 0 : (int)Math.Ceiling(Summary.Count / (double)PageSize);

    protected override async Task OnInitializedAsync()
    {
        CultureInfo = new CultureInfo(CultureName);
        await LoadPageAsync(1);
    }

    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > TotalPages || page == CurrentPage)
        {
            return;
        }

        await LoadPageAsync(page);
    }

    private async Task LoadPageAsync(int page)
    {
        CurrentPage = page;
        Summary = await _movieRatingService.GetSummaryAsync(ImdbId, CurrentPage, PageSize);
    }

    /// <summary>The page numbers to show, where null marks a gap: 1 … 4 5 [6] 7 8 … 20.</summary>
    private IEnumerable<int?> PageNumbers()
    {
        var previous = 0;

        for (var page = 1; page <= TotalPages; page++)
        {
            if (page == 1 || page == TotalPages || Math.Abs(page - CurrentPage) <= PageWindow)
            {
                if (page - previous > 1)
                {
                    yield return null;
                }

                yield return page;
                previous = page;
            }
        }
    }

    private async Task SaveRatingAsync()
    {
        IsSaving = true;

        try
        {
            await _movieRatingService.AddAsync(ImdbId, RatingModel.Score, RatingModel.Name, RatingModel.Comment);
            // Back to the first page, where the new rating is
            await LoadPageAsync(1);
            HasRated = true;
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>How much of star <paramref name="index"/> (0–4) to fill, as a CSS width.</summary>
    private static string FillWidth(double score, int index)
        => (Math.Clamp(score - index, 0, 1) * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%";

    private string Format(double score) => score.ToString("0.#", CultureInfo);

    private RenderFragment Stars(double score, string? sizeClass = null) =>
        @<span class="movie-stars @sizeClass" aria-hidden="true">@StarIcons(score)</span>;

    /// <summary>
    /// Five equally wide stars, each with its own gold layer, so a half score fills exactly half a star
    /// and the range input on top maps its value straight onto them.
    /// </summary>
    private static RenderFragment StarIcons(double score) =>
        @<text>@for (var i = 0; i < 5; i++)
        {
            <span class="movie-star">
                <i class="fas fa-star"></i>
                <span class="movie-star-fill" style="width: @FillWidth(score, i)"><i class="fas fa-star"></i></span>
            </span>
        }</text>;
}

<section class="card border-0 shadow-sm">
    <div class="card-body p-4">
        <h2 class="h5 mb-3">MovieFinder rating</h2>

        @if (Summary == null)
        {
            <div class="placeholder-glow" aria-hidden="true">
                <span class="placeholder col-8 mb-2"></span>
                <span class="placeholder col-5"></span>
            </div>
        }
        else
        {
            <div class="d-flex align-items-center gap-3 mb-4">
                @if (Summary.Average is double average)
                {
                    @Stars(average, "fs-4")
                    <div class="lh-sm">
                        <div><span class="fs-4 fw-bold">@Format(average)</span><span class="text-body-secondary">/5</span></div>
                        <small class="text-body-secondary">@Summary.Count @(Summary.Count == 1 ? "rating" : "ratings")</small>
                    </div>
                    <span class="visually-hidden">Rated @Format(average) out of 5 by @Summary.Count visitors</span>
                }
                else
                {
                    <p class="text-body-secondary mb-0">No ratings yet. Be the first!</p>
                }
            </div>

            @if (HasRated)
            {
                <div class="alert alert-success d-flex align-items-center mb-4" role="status">
                    <i class="fas fa-circle-check me-2" aria-hidden="true"></i>Thanks for your rating!
                </div>
            }
            else
            {
                <EditForm Model="@RatingModel" OnValidSubmit="SaveRatingAsync" class="mb-4">
                    <DataAnnotationsValidator />

                    <label for="ratingScore" class="form-label fw-semibold">Your rating</label>
                    <div class="d-flex align-items-center gap-3 mb-1">
                        <span class="movie-stars movie-stars-input fs-2">
                            <span aria-hidden="true">@StarIcons(RatingModel.Score)</span>
                            @* Invisible range on top of the stars: drag, tap or use the arrow keys to slide in half stars *@
                            <input id="ratingScore" type="range" min="0" max="@MovieRating.MaxScore" step="0.5"
                                   @bind="RatingModel.Score" @bind:event="oninput" @bind:culture="CultureInfo.InvariantCulture"
                                   aria-valuetext="@($"{Format(RatingModel.Score)} of 5 stars")" disabled="@IsSaving" />
                        </span>
                        <span class="fw-semibold">
                            @if (RatingModel.Score > 0)
                            {
                                @Format(RatingModel.Score)<span class="text-body-secondary fw-normal">/5</span>
                            }
                        </span>
                    </div>
                    <ValidationMessage For="@(() => RatingModel.Score)" class="validation-message small text-danger" />

                    <div class="mt-3 mb-3">
                        <label for="ratingName" class="form-label small text-body-secondary">Name (optional)</label>
                        <InputText id="ratingName" class="form-control" @bind-Value="RatingModel.Name"
                                   maxlength="@MovieRating.NameMaxLength" disabled="@IsSaving" />
                    </div>

                    <div class="mb-3">
                        <label for="ratingComment" class="form-label small text-body-secondary">Comment (optional)</label>
                        <InputTextArea id="ratingComment" class="form-control" rows="3" @bind-Value="RatingModel.Comment"
                                       maxlength="@MovieRating.CommentMaxLength" placeholder="What did you think?" disabled="@IsSaving" />
                    </div>

                    <button type="submit" class="btn btn-warning fw-semibold w-100" disabled="@IsSaving">
                        @if (IsSaving)
                        {
                            <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
                            <span role="status">Saving…</span>
                        }
                        else
                        {
                            <text>Submit rating</text>
                        }
                    </button>
                </EditForm>
            }

            @if (Summary.Ratings.Count > 0)
            {
                <ul class="list-group list-group-flush border-top">
                    @foreach (var rating in Summary.Ratings)
                    {
                        <li class="list-group-item px-0 py-3">
                            <div class="d-flex justify-content-between align-items-center mb-1">
                                <span class="fw-semibold">@(rating.Name ?? "Anonymous")</span>
                                <small class="text-body-secondary">@rating.CreatedUtc.ToLocalTime().ToString("d", CultureInfo)</small>
                            </div>
                            <div class="d-flex align-items-center gap-2">
                                @Stars(rating.Score, "small")
                                <small class="text-body-secondary">@Format(rating.Score)/5</small>
                            </div>
                            @if (rating.Comment != null)
                            {
                                <p class="mb-0 mt-2 movie-rating-comment">@rating.Comment</p>
                            }
                        </li>
                    }
                </ul>

                @if (TotalPages > 1)
                {
                    <nav class="mt-3" aria-label="Rating pages">
                        <ul class="pagination pagination-sm justify-content-center mb-0">
                            <li class="page-item @(CurrentPage == 1 ? "disabled" : null)">
                                <button type="button" class="page-link" disabled="@(CurrentPage == 1)" aria-label="Previous"
                                        @onclick="() => GoToPageAsync(CurrentPage - 1)">
                                    <span aria-hidden="true">&laquo;</span>
                                </button>
                            </li>
                            @foreach (var page in PageNumbers())
                            {
                                if (page is int number)
                                {
                                    <li class="page-item @(number == CurrentPage ? "active" : null)">
                                        <button type="button" class="page-link" aria-current="@(number == CurrentPage ? "page" : null)"
                                                @onclick="() => GoToPageAsync(number)">@number</button>
                                    </li>
                                }
                                else
                                {
                                    <li class="page-item disabled" aria-hidden="true"><span class="page-link">…</span></li>
                                }
                            }
                            <li class="page-item @(CurrentPage == TotalPages ? "disabled" : null)">
                                <button type="button" class="page-link" disabled="@(CurrentPage == TotalPages)" aria-label="Next"
                                        @onclick="() => GoToPageAsync(CurrentPage + 1)">
                                    <span aria-hidden="true">&raquo;</span>
                                </button>
                            </li>
                        </ul>
                    </nav>
                }
            }
        }
    </div>
</section>
```

`CreatedUtc` comes back from the store with `DateTimeKind.Unspecified`; `ToLocalTime()` treats Unspecified as UTC, so the shown date is right.

- [ ] **Step 8: Build**

Run the [build command](#build-command). Expected: 0 warnings, 0 errors. (Nothing renders the component yet; Task 3 does.)

- [ ] **Step 9: Commit**

```bash
git add Models/Ratings Services/Interfaces/IMovieRatingService.cs Services/MovieRatingService.cs Blazor/MovieRatingBox.razor Startup.cs
git commit -F - <<'EOF'
Store movie ratings in the Dynamic Data Store

Port the umbraco26 MovieFinder rating box: half-star scores with an
optional name and comment, an average, and paged ratings newest first.
Ratings live in the MovieFinderRatings store instead of an EF Core table.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 3: Add the movie page

**Files:**
- Create: `Models/Pages/MoviePage.cs`
- Modify: `Models/Pages/StartPage.cs` (allowed child types)
- Modify: `Models/ViewModels/IPageViewModel.cs`, `Models/ViewModels/PageViewModel.cs` (`PageTitle`, `MetaDescription`)
- Create: `Models/ViewModels/MoviePageViewModel.cs`
- Create: `Controllers/MoviePageController.cs`
- Create: `Views/MoviePage/Index.cshtml`
- Create: `wwwroot/css/movie.css` (copied from umbraco26)
- Modify: `Views/Shared/Layouts/_Root.cshtml` (title, description, `movie.css`)

**Interfaces:**
- Consumes: `IOmdbService.GetByIdAsync(string) : Task<OmdbMovieDetails?>` (Task 1); `OmdbMovieDetails`, `OmdbRating` (Task 1); `Optimizely26.Blazor.MovieRatingBox` with `ImdbId`, `CultureName` (Task 2); `EPiServer.Web.IContextModeResolver.CurrentMode : ContextMode` (`Undefined, Default, Edit, Preview`).
- Produces: page type `Optimizely26.Models.Pages.MoviePage : SitePageData`; `IPageViewModel<T>.PageTitle` and `.MetaDescription` (string); `MoviePageViewModel(MoviePage currentPage, OmdbMovieDetails? movie)` with `OmdbMovieDetails? Movie`.

- [ ] **Step 1: Create `Models/Pages/MoviePage.cs`**

```csharp
using Optimizely26.Business;

namespace Optimizely26.Models.Pages
{
	/// <summary>Shows one OMDb title, picked by <c>?id={imdbId}</c>, with visitor ratings.</summary>
	[ContentType(
		GUID = "6E2B7C1A-4F3D-4B8E-9C2A-7D5E1F3A9B64",
		GroupName = Globals.GroupNames.Specialized,
		DisplayName = "Movie Page",
		Description = "Shows one movie from OMDb, picked by ?id={imdbId}, with visitor ratings."
	)]
	public class MoviePage : SitePageData
	{
	}
}
```

- [ ] **Step 2: Allow it under the start page**

In `Models/Pages/StartPage.cs`, change the `Include` list of `[AvailableContentTypes]` to:

```csharp
        Include = new[] { 
            typeof(SettingsPage), 
            typeof(ContainerPage),
			typeof(ErrorPage),
			typeof(XmlSitemap),
			typeof(MoviePage)
		}
```

- [ ] **Step 3: Make the page title and description overridable**

Replace `Models/ViewModels/IPageViewModel.cs` with:

```csharp
using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public interface IPageViewModel<out T> where T : SitePageData
    {
        T CurrentPage { get; }

        LayoutModel? Layout { get; set; }

        /// <summary>The page's &lt;title&gt;.</summary>
        string PageTitle { get; }

        /// <summary>The page's meta description.</summary>
        string MetaDescription { get; }
    }
}
```

In `Models/ViewModels/PageViewModel.cs`, add to `PageViewModel<T>` after the `Layout` property:

```csharp

        public virtual string PageTitle => CurrentPage.Name;

        public virtual string MetaDescription => CurrentPage.MetaDescription;
```

In `Views/Shared/Layouts/_Root.cshtml`, change the `<title>` and description lines to:

```cshtml
    <title>@Model.PageTitle</title>
    <meta name="description" content="@Model.MetaDescription" />
```

and add the movie styles after the `carousel.css` link:

```cshtml
    <link href="~/css/movie.css" rel="stylesheet" />
```

- [ ] **Step 4: Create `Models/ViewModels/MoviePageViewModel.cs`**

```csharp
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
```

- [ ] **Step 5: Create `Controllers/MoviePageController.cs`**

```csharp
using EPiServer.Web;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;
using Optimizely26.Services;

namespace Optimizely26.Controllers
{
	public class MoviePageController(IOmdbService omdbService, IContextModeResolver contextModeResolver) : PageControllerBase<MoviePage>
	{
		private readonly IOmdbService _omdbService = omdbService;
		private readonly IContextModeResolver _contextModeResolver = contextModeResolver;

		/// <summary>The title comes from <c>?id={imdbId}</c>.</summary>
		public async Task<IActionResult> Index(MoviePage currentPage, string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				// Editors open the page without an id; show them a note instead of the error page
				if (_contextModeResolver.CurrentMode is ContextMode.Edit or ContextMode.Preview)
				{
					return View(new MoviePageViewModel(currentPage, null));
				}

				return NotFound();
			}

			var movie = await _omdbService.GetByIdAsync(id);

			if (movie == null)
			{
				return NotFound();
			}

			return View(new MoviePageViewModel(currentPage, movie));
		}
	}
}
```

`NotFound()` becomes a 404, which `Startup`'s `UseStatusCodePages` redirects to `/error`.

- [ ] **Step 6: Copy the movie styles**

```bash
git -C C:/src/umbraco26 show origin/main:Umbraco26/wwwroot/css/movie.css > C:/src/optimizely26/wwwroot/css/movie.css
head -3 C:/src/optimizely26/wwwroot/css/movie.css
```

Expected first line: `/* Movie detail page */`.

- [ ] **Step 7: Create `Views/MoviePage/Index.cshtml`**

Ported from umbraco26 `Views/Movie.cshtml`. Differences: `Model.CurrentPage` instead of `Model.Content`, the culture from the page language, the editor note when `Movie` is null, and the poster fallback.

```cshtml
@using Optimizely26.Blazor
@using Optimizely26.Models.ViewModels

@model MoviePageViewModel

@{
    if (Model.Movie == null)
    {
        @* Only reached in edit or preview mode, where the page is opened without ?id= *@
        <div class="container py-5">
            <div class="alert alert-info d-flex align-items-center mb-0" role="status">
                <i class="fas fa-circle-info me-2" aria-hidden="true"></i>Open a movie from the search to see it here.
            </div>
        </div>

        return;
    }

    var movie = Model.Movie;

    @* OMDb uses "N/A" for missing values *@
    string? Value(string? value) => string.IsNullOrWhiteSpace(value) || value == "N/A" ? null : value;
    string[] List(string? value) => Value(value)?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

    var title = Value(movie.Title) ?? Model.CurrentPage.Name;
    var year = Value(movie.Year);
    var rated = Value(movie.Rated);
    var runtime = Value(movie.Runtime);
    var released = Value(movie.Released);
    var type = Value(movie.Type);
    var genres = List(movie.Genre);
    var plot = Value(movie.Plot);
    var poster = Value(movie.Poster);
    var imdbId = Value(movie.ImdbID);
    var imdbRating = Value(movie.ImdbRating);
    var imdbVotes = Value(movie.ImdbVotes);
    var metascore = Value(movie.Metascore);
    var director = Value(movie.Director);
    var writers = List(movie.Writer);
    var actors = List(movie.Actors);
    var language = Value(movie.Language);
    var country = Value(movie.Country);
    var awards = Value(movie.Awards);
    var boxOffice = Value(movie.BoxOffice);
    var ratings = movie.Ratings;
    var cultureName = Model.CurrentPage.Language.Name;

    var details = new (string Label, string? Value)[]
    {
        ("Released", released),
        ("Language", language),
        ("Country", country),
        ("Box office", boxOffice),
    }.Where(detail => detail.Value != null).ToList();
}

<section class="movie-hero bg-dark text-white py-5 mb-5" data-bs-theme="dark">
    @if (poster != null)
    {
        <div class="movie-hero-backdrop" style="background-image: url('@poster')" aria-hidden="true"></div>
    }

    <div class="container position-relative py-lg-4">
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb small mb-4">
                <li class="breadcrumb-item"><a href="/" class="link-secondary">Movie Finder</a></li>
                <li class="breadcrumb-item active" aria-current="page">@title</li>
            </ol>
        </nav>

        <div class="row g-4 g-lg-5 align-items-center">
            <div class="col-8 col-sm-5 col-md-4 col-lg-3 mx-auto mx-md-0">
                <div class="movie-poster rounded-3 shadow-lg bg-secondary-subtle">
                    @if (poster != null)
                    {
                        @* A broken poster URL hides itself and reveals the placeholder below *@
                        <img src="@poster" alt="Poster for @title" onerror="this.hidden = true; this.nextElementSibling.hidden = false;" />
                    }
                    @* [hidden] sits on a plain wrapper because Bootstrap's d-flex would override it *@
                    <div class="h-100" hidden="@(poster != null)">
                        <div class="d-flex flex-column align-items-center justify-content-center h-100 text-body-secondary">
                            <i class="fas fa-film fa-3x mb-2"></i>
                            <small>No poster</small>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-md-8 col-lg-9">
                @if (type != null)
                {
                    <span class="badge text-bg-warning text-capitalize mb-2">@type</span>
                }
                <h1 class="display-5 fw-bold mb-2">
                    @title
                    @if (year != null)
                    {
                        <span class="fw-light text-body-secondary">(@year)</span>
                    }
                </h1>

                <ul class="list-inline text-body-secondary mb-3">
                    @if (rated != null)
                    {
                        <li class="list-inline-item"><span class="badge border border-secondary text-body-secondary">@rated</span></li>
                    }
                    @if (runtime != null)
                    {
                        <li class="list-inline-item"><i class="far fa-clock me-1"></i>@runtime</li>
                    }
                    @if (released != null)
                    {
                        <li class="list-inline-item"><i class="far fa-calendar me-1"></i>@released</li>
                    }
                </ul>

                @if (genres.Length > 0)
                {
                    <div class="d-flex flex-wrap gap-2 mb-4">
                        @foreach (var genre in genres)
                        {
                            <span class="badge rounded-pill text-bg-secondary fw-normal px-3 py-2">@genre</span>
                        }
                    </div>
                }

                @if (plot != null)
                {
                    <p class="lead mb-4">@plot</p>
                }

                <div class="d-flex flex-wrap align-items-center gap-4">
                    @if (imdbRating != null)
                    {
                        <div class="d-flex align-items-center">
                            <i class="fas fa-star fa-2x text-warning me-2" aria-hidden="true"></i>
                            <div class="lh-sm">
                                <div><span class="fs-4 fw-bold">@imdbRating</span><span class="text-body-secondary">/10</span></div>
                                @if (imdbVotes != null)
                                {
                                    <small class="text-body-secondary">@imdbVotes votes</small>
                                }
                            </div>
                        </div>
                    }

                    @if (metascore != null)
                    {
                        <div class="d-flex align-items-center">
                            <span class="movie-metascore rounded-2 fw-bold me-2">@metascore</span>
                            <small class="text-body-secondary lh-sm">Metascore</small>
                        </div>
                    }

                    @if (imdbId != null)
                    {
                        <a href="https://www.imdb.com/title/@imdbId/" class="btn btn-warning fw-semibold" target="_blank" rel="noopener">
                            <i class="fab fa-imdb me-2"></i>View on IMDb
                        </a>
                    }
                </div>
            </div>
        </div>
    </div>
</section>

<div class="container pb-5">
    <div class="row g-4">
        <div class="col-lg-8">
            <article class="card border-0 shadow-sm h-100">
                <div class="card-body p-4">
                    <h2 class="h5 mb-4">Cast &amp; crew</h2>

                    <dl class="row mb-0">
                        @if (director != null)
                        {
                            <dt class="col-sm-3 text-body-secondary fw-normal">Director</dt>
                            <dd class="col-sm-9">@director</dd>
                        }

                        @if (writers.Length > 0)
                        {
                            <dt class="col-sm-3 text-body-secondary fw-normal">Writers</dt>
                            <dd class="col-sm-9">@string.Join(", ", writers)</dd>
                        }

                        @if (actors.Length > 0)
                        {
                            <dt class="col-sm-3 text-body-secondary fw-normal">Stars</dt>
                            <dd class="col-sm-9 mb-0">
                                <div class="d-flex flex-wrap gap-3">
                                    @foreach (var actor in actors)
                                    {
                                        <div class="d-flex align-items-center">
                                            <span class="movie-avatar rounded-circle bg-secondary-subtle text-body-secondary me-2" aria-hidden="true">
                                                <i class="fas fa-user"></i>
                                            </span>
                                            @actor
                                        </div>
                                    }
                                </div>
                            </dd>
                        }
                    </dl>

                    @if (details.Count > 0)
                    {
                        <hr class="my-4" />

                        <h2 class="h5 mb-4">Details</h2>

                        <dl class="row mb-0">
                            @foreach (var detail in details)
                            {
                                <dt class="col-sm-3 text-body-secondary fw-normal">@detail.Label</dt>
                                <dd class="col-sm-9">@detail.Value</dd>
                            }
                        </dl>
                    }
                </div>
            </article>
        </div>

        <div class="col-lg-4">
            <aside class="d-flex flex-column gap-4 h-100">
                @if (ratings.Count > 0)
                {
                    <section class="card border-0 shadow-sm">
                        <div class="card-body p-4">
                            <h2 class="h5 mb-3">Ratings</h2>
                            <ul class="list-group list-group-flush">
                                @foreach (var rating in ratings)
                                {
                                    <li class="list-group-item d-flex justify-content-between align-items-center px-0">
                                        <span class="text-body-secondary">@rating.Source</span>
                                        <span class="fw-semibold">@rating.Value</span>
                                    </li>
                                }
                            </ul>
                        </div>
                    </section>
                }

                @if (imdbId != null)
                {
                    <component type="typeof(MovieRatingBox)" render-mode="Server" param-ImdbId="@imdbId" param-CultureName="@cultureName" />
                }

                @if (awards != null)
                {
                    <section class="card border-0 shadow-sm">
                        <div class="card-body p-4 d-flex">
                            <i class="fas fa-trophy fa-2x text-warning me-3" aria-hidden="true"></i>
                            <div>
                                <h2 class="h5 mb-1">Awards</h2>
                                <p class="text-body-secondary mb-0">@awards</p>
                            </div>
                        </div>
                    </section>
                }
            </aside>
        </div>
    </div>

    <div class="mt-5">
        <a href="/" class="btn btn-outline-secondary"><i class="fas fa-arrow-left me-2"></i>Back to search</a>
    </div>
</div>
```

- [ ] **Step 8: Build**

Run the [build command](#build-command). Expected: 0 warnings, 0 errors.

- [ ] **Step 9: Commit**

```bash
git add Models/Pages/MoviePage.cs Models/Pages/StartPage.cs Models/ViewModels/IPageViewModel.cs Models/ViewModels/PageViewModel.cs Models/ViewModels/MoviePageViewModel.cs Controllers/MoviePageController.cs Views/MoviePage/Index.cshtml Views/Shared/Layouts/_Root.cshtml wwwroot/css/movie.css
git commit -F - <<'EOF'
Add a movie page fed by OMDb

The page takes the title from ?id={imdbId}, shows its details with the
MovieFinder rating box, and uses the movie as the page title and meta
description. Visitors get the error page for a missing or unknown id;
editors see a note instead.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 4: Link search results to the movie page

**Files:**
- Modify: `Models/ViewModels/StartPageViewModel.cs`
- Modify: `Controllers/StartPageController.cs` (full replacement below)
- Modify: `Views/StartPage/Index.cshtml`
- Modify: `Blazor/OmdbSearchPage.razor`

**Interfaces:**
- Consumes: `MoviePage` (Task 3); existing `ContentLoaderExtensions.GetDescendantsAndSelf(this IContentLoader, ContentReference) : IEnumerable<SitePageData>` (lazy; skips `XmlSitemap`); `IPublishedStateAssessor.IsPublished(IContent, PublishedStateCondition)`; `IUrlResolver.GetUrl(ContentReference)` extension from `EPiServer.Web.Routing`.
- Produces: `StartPageViewModel.MoviePageUrl : string?`; `OmdbSearchPage` parameter `string? MoviePageUrl`.

- [ ] **Step 1: Add the URL to `Models/ViewModels/StartPageViewModel.cs`**

Replace the file with:

```csharp
using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public class StartPageViewModel : PageViewModel<StartPage>
    {
        public StartPageViewModel(StartPage currentPage) : base(currentPage)
        {
        }

        /// <summary>The first published movie page; null when there is none, and results link to IMDb.</summary>
        public string? MoviePageUrl { get; init; }
    }
}
```

- [ ] **Step 2: Find the movie page in `Controllers/StartPageController.cs`**

Replace the file with:

```csharp
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;
using Optimizely26.Business.Extensions;
using Optimizely26.Models.Pages;
using Optimizely26.Models.ViewModels;

namespace Optimizely26.Controllers
{
    public class StartPageController(IContentLoader contentLoader, IPublishedStateAssessor publishedStateAssessor, IUrlResolver urlResolver) : PageControllerBase<StartPage>
    {
        private readonly IContentLoader _contentLoader = contentLoader;
        private readonly IPublishedStateAssessor _publishedStateAssessor = publishedStateAssessor;
        private readonly IUrlResolver _urlResolver = urlResolver;

        public IActionResult Index(StartPage currentPage)
        {
            var model = new StartPageViewModel(currentPage)
            {
                MoviePageUrl = GetMoviePageUrl(currentPage)
            };

            return View(model);
        }

        /// <summary>The URL of the first published movie page under the start page, so visitors are never sent to a 404.</summary>
        private string? GetMoviePageUrl(StartPage startPage)
        {
            var moviePage = _contentLoader.GetDescendantsAndSelf(startPage.ContentLink)
                .OfType<MoviePage>()
                .FirstOrDefault(page => _publishedStateAssessor.IsPublished(page, PublishedStateCondition.None));

            return moviePage == null ? null : _urlResolver.GetUrl(moviePage.ContentLink);
        }
    }
}
```

- [ ] **Step 3: Pass it to the search in `Views/StartPage/Index.cshtml`**

Change the `<component>` line to:

```cshtml
<component type="typeof(OmdbSearchPage)" render-mode="Server" param-CultureName="@cultureName" param-MoviePageUrl="@Model.MoviePageUrl" />
```

- [ ] **Step 4: Link results to it in `Blazor/OmdbSearchPage.razor`**

After the `CultureName` parameter, add:

```razor

    /// <summary>The movie page; when missing, results link to IMDb instead.</summary>
    [Parameter]
    public string? MoviePageUrl { get; set; }
```

Replace the result title link:

```razor
                            <h3 class="card-title h6 mb-1">
                                <a href="https://www.imdb.com/title/@item.ImdbID/" class="stretched-link clean-link" target="_blank" rel="noopener">@item.Title</a>
                            </h3>
```

with:

```razor
                            <h3 class="card-title h6 mb-1">
                                @if (MoviePageUrl != null)
                                {
                                    <a href="@($"{MoviePageUrl}?id={Uri.EscapeDataString(item.ImdbID)}")" class="stretched-link clean-link">@item.Title</a>
                                }
                                else
                                {
                                    <a href="https://www.imdb.com/title/@item.ImdbID/" class="stretched-link clean-link" target="_blank" rel="noopener">@item.Title</a>
                                }
                            </h3>
```

Keep the existing `FailedPosters` fallback as it is.

- [ ] **Step 5: Build**

Run the [build command](#build-command). Expected: 0 warnings, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Models/ViewModels/StartPageViewModel.cs Controllers/StartPageController.cs Views/StartPage/Index.cshtml Blazor/OmdbSearchPage.razor
git commit -F - <<'EOF'
Link search results to the movie page

The start page finds the first published movie page and passes its URL
to the search, which links each result to it with ?id={imdbId}. Without
a movie page, results still link to IMDb.

Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>
EOF
```

---

### Task 5: Check it in the browser

**Files:** none (verification only). Needs the site restarted with the new build. **Ask the user before stopping the running site** (it was started outside this session).

- [ ] **Step 1: Restart the site on the new build**

With the user's go-ahead, stop the running `Optimizely26` process, then start it from the project:

```bash
cd C:/src/optimizely26
dotnet run --launch-profile Optimizely26
```

Expected: it listens on `https://localhost:5000/`. On start the CMS adds the `MoviePage` content type and the `MovieFinderRatings` store.

- [ ] **Step 2: Create the movie page**

In the CMS (`/episerver/cms`), create a **Movie Page** named "Movie" under the start page and publish it.

- [ ] **Step 3: Movie page from the search**

On `/`, search "batman" and click "Batman Begins". Expected: `/movie/?id=tt0372784` (path follows the page name) shows the poster, plot, cast and crew, details, OMDb ratings and awards; the browser tab title is "Batman Begins".

- [ ] **Step 4: Missing and unknown ids (Review Focus 1)**

Open `/movie/` and `/movie/?id=tt0000000`. Expected: both end on `/error` ("SIDAN FINNS INTE"). In the CMS, open the Movie page in edit mode and in preview. Expected: the note "Open a movie from the search to see it here.", not the error page.

- [ ] **Step 5: Rate, including HTML in the comment (Review Focus 2)**

On the Batman Begins page, slide to 3.5 stars, name "Test", comment `<b>great</b>` then Enter then `second line`, and submit. Expected: "Thanks for your rating!", average 3.5, "1 rating", the rating at the top with today's date; the comment shows the literal text `<b>great</b>` with `second line` on its own line.

- [ ] **Step 6: Ratings survive a restart**

Stop and start the site again (Step 1 command) and reopen the Batman Begins page. Expected: the rating is still there.

- [ ] **Step 7: Paging boundary (Review Focus 5)**

On one title, add ratings until there are exactly 10 (reload the page between ratings; the form hides after each submit). Expected: no pager. Add an 11th. Expected: a pager with pages 1 and 2, and the newest rating first on page 1.

- [ ] **Step 8: Broken poster on the movie page (Review Focus 4)**

Open the movie page for "Batman v Superman: Dawn of Justice (Ultimate Edition)" (its poster URL fails, as seen in the search). Expected: the "No poster" placeholder, not a broken image.

- [ ] **Step 9: Unpublished movie page (Review Focus 3)**

In the CMS, unpublish the Movie page, then search again on `/`. Expected: results link to IMDb in a new tab. Republish it afterwards. Expected: results link to the movie page again.

- [ ] **Step 10: Report**

Tell the user which checks passed, quoting anything that didn't, before pushing `local-wip`.
