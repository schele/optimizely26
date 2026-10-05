# Site search ("find") with our own Lucene.NET index

Date: 2026-10-05
Status: Approved design, awaiting spec review

## Goal

Give `optimizely26` (CMS 13, .NET 10) a site search like the one in `umbraco26` (`Views/Find.cshtml`, `Blazor/FindSearchPage.razor`, `Business/Services/FindService.cs`), without Optimizely Graph or Search & Navigation.

umbraco26 queries Umbraco's built-in Examine (Lucene) index. CMS 13 has no built-in index, so we build and maintain our own index with Lucene.NET, the same engine Examine wraps.

- A find page where visitors search published pages in the current language (sv or en). Results show 10 per page, with numbered pagination, a hit count and empty states, as in umbraco26.
- Searches match the page name, the meta description and body text. A name match ranks highest.
- The query and page number are in the address bar (`?q=film&page=2`), so a results page can be linked and reloaded.
- A new article page type, so there is content to find.

**Done when:** an article published in sv and en can be found by words in its name, its meta description and its body, in each language; unpublishing it, moving it to the trash or restricting its access makes it disappear from the results; and the index rebuilds itself after `App_Data/SearchIndex` is deleted.

## Decisions

| Question | Decision |
|---|---|
| Search engine | Our own index with **Lucene.NET 4.8.0-beta00018** (`Lucene.Net`, `Lucene.Net.Analysis.Common`). No Graph, no Search & Navigation, no SQL full-text. |
| What is indexed | Page name, the meta description, and every other property Optimizely marks as searchable (string and XhtmlString are searchable by default; `[Searchable(false)]` opts out). |
| UI | A port of umbraco26's Blazor component, plus `?q=` and `page` kept in the address bar with a little JavaScript. |
| Hosting | One server. The index lives in `App_Data/SearchIndex` (already git-ignored). Multi-server support is out of scope. |
| Restricted pages | Never indexed: only pages that anonymous visitors can read are searchable, even for logged-in users. |
| Content to search | A new `ArticlePage` type with `MainBody`. No sample articles are seeded. |
| Naming | "Find" for the page, service and UI (as in umbraco26; no Optimizely Find package is installed). "SearchIndex" for the Lucene parts. |
| Tests | No test project; verified by a clean build and browser checks (see Verification). |

## Out of scope

- Several servers sharing or syncing an index.
- Highlighted excerpts, "did you mean", autocomplete, facets and sorting options.
- Stemming and language-specific stop words.
- Searching media, PDFs, blocks in content areas, or OMDb movies.
- Browser history per search (the back button leaves the find page).
- Prerendering the find page's results on the server.
- A search box in the header (the header gets a link to the find page instead).

## Design

### Packages

`Optimizely26.csproj` adds `Lucene.Net` and `Lucene.Net.Analysis.Common`, both `4.8.0-beta00018`. Queries are built in code, so `Lucene.Net.QueryParser` is not needed, and nothing a visitor types can cause a query syntax error.

### Article page

`Models/Pages/ArticlePage.cs`: inherits `SitePageData`, group `Specialized`, display name "Article Page". One property:

```csharp
[Display(GroupName = SystemTabNames.Content, Order = 10)]
[CultureSpecific]
public virtual XhtmlString? MainBody { get; set; }
```

`[AvailableContentTypes(Availability.Specific, Include = new[] { typeof(ArticlePage) })]`, so articles can be nested. `StartPage`'s include list adds `typeof(ArticlePage)` and `typeof(FindPage)`.

`Controllers/ArticlePageController.cs` is a `PageControllerBase<ArticlePage>` whose `Index` returns `View(PageViewModel.Create(currentPage))`. `Views/ArticlePage/Index.cshtml` renders the name as `<h1>` and `MainBody` with `@Html.PropertyFor(x => x.CurrentPage.MainBody)`, inside a Bootstrap container, so on-page editing works.

### Index layout

One Lucene document per page and language:

| Field | Lucene type | Contents |
|---|---|---|
| `key` | `StringField`, stored | `{contentId}:{lang}`, for example `42:sv` |
| `contentId` | `StringField`, stored | `ContentLink.ID`, used to delete every language of a page, including after the page is deleted |
| `lang` | `StringField`, stored | `sv` or `en` (the content language name, lowercased) |
| `name` | `TextField` | `PageName` |
| `description` | `TextField` | The `MetaDescription` property's own value (`GetPropertyValue`), not the getter's fallback to the name, so the name doesn't count twice |
| `content` | `TextField` | All other properties whose `PropertyDefinition.Searchable` is true and whose value is a `string` or `XhtmlString`, joined with spaces. HTML tags are removed and entities decoded. |
| `stopPublish` | `Int64Field` | `StopPublish` in UTC ticks, or `long.MaxValue` when there is none |

Nothing used for display is stored. Hits are loaded from `IContentLoader` at search time, so names, descriptions, URLs and dates are always current, and moving a page never leaves a stale URL in the index.

**Analyzer:** `StandardAnalyzer(LuceneVersion.LUCENE_48, CharArraySet.EMPTY_SET)` for every field and both languages: words are split and lowercased, there are no stop words and no stemming, and å/ä/ö are kept as they are.

**Layout version:** a constant `LayoutVersion = 1` is written to the commit user data (`layoutVersion`) on every commit. Change it whenever the fields or the analyzer change; the next startup then rebuilds the index.

### Which pages go in

`Business/Search/SearchDocumentFactory.cs` decides per page and language. A page language is indexed only if all of these are true:

1. It is a `SitePageData`, and it is the start page or a descendant of the start page.
2. It is not in the trash (`IsDeleted` is false).
3. It is published in that language: `IPublishedStateAssessor.IsPublished(page, PublishedStateCondition.None)`, which also rejects expired pages and pages whose start-publish date is in the future.
4. It has a page template: `ITemplateResolver.HasTemplate(page, TemplateTypeCategories.Request)` (the extension in `TemplateResolverExtensions`). This leaves out carousel and settings pages.
5. It is not an `ErrorPage`, `XmlSitemap`, `MoviePage` (it returns 404 without `?id=`) or `FindPage`.
6. Anonymous visitors can read it: `IContentAccessEvaluator.HasAccess(page, anonymousPrincipal, AccessLevel.Read)`. The anonymous principal is `PrincipalInfo.AnonymousPrincipal` if CMS 13 exposes it publicly; otherwise an unauthenticated `GenericPrincipal` with no roles. Check this during implementation.

Each language is loaded with `LanguageLoaderOption.Specific(culture)`, so a missing translation is never indexed as the master language.

### Keeping the index up to date

All writes go through one background worker, so an editor's publish never waits for Lucene, writes never overlap, and the worker always reads the page's current state (a rebuild that is running can never overwrite a newer publish with an older version).

- **`Business/Search/SearchIndex.cs`** (singleton, `IDisposable`): owns the `FSDirectory` at `{ContentRootPath}/App_Data/SearchIndex`, the single `IndexWriter` (`OpenMode.CREATE_OR_APPEND`) and a `SearcherManager`. Methods:
  - `ReplacePage(int contentId, IEnumerable<Document> documents)`: deletes every document for `contentId`, adds the given ones, then commits and refreshes the searcher.
  - `RemovePages(IEnumerable<int> contentIds)`: deletes them, then commits and refreshes.
  - `ReplaceAll(IEnumerable<Document> documents)`: `DeleteAll`, adds them all, then commits and refreshes. Searches keep using the previous commit until this one is done.
  - `IsCurrent`: true when the index exists and its `layoutVersion` equals `LayoutVersion`.
  - `Search(Query query, Filter filter, int count)`: acquires a searcher from the `SearcherManager`, runs the search, and releases it.
  
  It is the only class that touches the Lucene directory. `Dispose` disposes the `SearcherManager`, then the writer, then the directory, so the write lock is released when the site shuts down.
- **`Business/Search/SearchIndexQueue.cs`** (singleton): an unbounded `Channel<SearchIndexWork>` with three kinds of work:
  - `Reindex(ContentReference, includeDescendants)`
  - `Remove(IEnumerable<int> contentIds)`
  - `Rebuild`, which carries a `TaskCompletionSource<int>` so a caller can await the number of documents written.
- **`Business/Search/SearchIndexWorker.cs`** (`BackgroundService`): reads the queue one item at a time.
  - A reindex loads the page (and its descendants when asked) and calls `ReplacePage` for each one with the documents from `SearchDocumentFactory`. An empty list removes the page from the index.
  - A rebuild walks the start page and all its descendants in every existing language and calls `ReplaceAll`.
  - Every exception is logged and the worker moves on to the next item. A failed rebuild completes its `TaskCompletionSource` with the exception.
- **`Initialization/SearchIndexInitialization.cs`** (`IInitializableModule`, depends on `EPiServer.Web.InitializationModule`) subscribes to these events and only enqueues:

  | Event | Work |
  |---|---|
  | `IContentEvents.PublishedContent` | `Reindex(page)`. This also covers unpublishing. |
  | `IContentEvents.MovedContent` | `Reindex(page, includeDescendants: true)`. Moving to the trash removes them through rule 2; restoring adds them back. |
  | `IContentEvents.DeletedContent` | `Remove(page and DeletedDescendents)` |
  | `IContentEvents.DeletedContentLanguage` | `Reindex(page)`. The deleted language no longer loads, so its document goes. |
  | `IContentSecurityEvents.ContentSecuritySaved` | `Reindex(page, includeDescendants: true)`, because access rights are inherited |

  At startup it enqueues `Rebuild` when `SearchIndex.IsCurrent` is false. Searches return no hits until that first build is done. `Uninitialize` unsubscribes.

  Expiry needs no event: when `StopPublish` passes, nothing fires, so searches filter on `stopPublish` instead.
- **`Business/ScheduledJobs/RebuildSearchIndex.cs`** ("Rebuild search index"): enqueues `Rebuild`, awaits it, and returns "Indexed N page languages." or the error message. The scheduler is turned off in Development; check during implementation whether the job can still be started manually there. Either way, startup covers a missing index.

`Startup.ConfigureServices` registers `SearchIndex`, `SearchIndexQueue` and `SearchDocumentFactory` as singletons and `SearchIndexWorker` with `AddHostedService`.

### Searching

`Services/Interfaces/IFindService.cs` and `Services/FindService.cs` (scoped):

```csharp
/// <summary>Searches published pages in <paramref name="culture"/>; <paramref name="page"/> is 1-based.</summary>
FindResult FindContent(string query, CultureInfo culture, int page, int pageSize);
```

`Models/Find/FindResult.cs` is `record FindResult(IReadOnlyList<Hit> Hits, int TotalCount, int Page)` with `static FindResult Empty`. `Page` is the page actually returned after clamping. `Models/Find/Hit.cs` has `Name`, `Description`, `Url` and `Changed` (DateTime).

**Query:**
1. Split the input with the same analyzer. Keep the first 10 words. No words gives `FindResult.Empty`.
2. Every word is a `MUST` clause. Each word matches if any of these `SHOULD` clauses does, for each field with its weight (name 3, description 2, content 1):
   - a `TermQuery`, boosted by 2 × the weight;
   - a `PrefixQuery`, boosted by the weight, only for words of 3 or more letters.
3. With 2 or more words, a `PhraseQuery` on `name` (boost 6) and one on `content` (boost 2) are added as top-level `SHOULD` clauses, so the exact phrase ranks higher.
4. The filter, which doesn't affect scores, is `lang` equals the culture's language name, and `stopPublish` is later than `DateTime.UtcNow`.

The weights and boosts are constants at the top of `FindService`.

**Paging:** search with `count = page × pageSize`. If the page is beyond the last page and there are hits, use the last page (its hits are already in the results). Then take that page's slice. `TotalCount` is Lucene's total hit count.

**Hits:** each document is loaded with `IContentLoader.TryGet<SitePageData>` for `contentId` in that language. The hit gets `Name`, `MetaDescription` (here the fallback to the name is fine), `Url` from `IUrlResolver.GetUrl(contentLink, lang)`, and `Changed`. A document whose page no longer loads is skipped; the count can then be off by one until the index catches up.

**Errors:** if the index can't be read, `FindService` logs the exception and rethrows it. The component shows an error message (see below).

### Find page

`Models/Pages/FindPage.cs`: inherits `SitePageData`, no own properties, group `Specialized`, display name "Find Page".

`Initialization/FindPageInitialization.cs` works like `MoviePageInitialization`: after `SiteSetupInitialization`, if there is a start page and no `FindPage` among its descendants (published or not), it creates and publishes one under the start page, named "Sök" in sv (the master language) and "Search" in en.

`Controllers/FindPageController.cs`: `Index(FindPage currentPage, string? q, int page = 1)` returns a `FindPageViewModel` (derives from `PageViewModel<FindPage>`) with `Query` and `Page`.

`Views/FindPage/Index.cshtml` loads `~/js/find-page.js` and renders:

```cshtml
<component type="typeof(FindSearchPage)" render-mode="Server"
           param-CultureName="@Model.CurrentPage.Language.Name"
           param-InitialQuery="@Model.Query" param-InitialPage="@Model.Page" />
```

### Blazor component

`Blazor/FindSearchPage.razor` ports umbraco26's component (hero with `EditForm role="search"`, spinner, "Results for", hit count, `<article>` list with date, title, description and "Read more", pagination with a window of 2, the two empty states, focus on the results heading after paging). The changes:

- **Texts** come from `LocalizationService.GetStringByCulture("/find/{key}", fallback, CultureInfo)` instead of Umbraco dictionary items.
- **Starting from the URL:** when `InitialQuery` is set, `OnInitialized` fills the search box and loads `InitialPage` right away, without the 750 ms pause. The pause remains for searches the visitor submits.
- **The address bar:** after every search and page change, `OnAfterRenderAsync` calls `findPage.replaceUrl(query, page)` through `IJSRuntime`. `wwwroot/js/find-page.js` sets `q` and `page` on the current URL (removing `page` when it is 1) with `history.replaceState`.
- **Errors:** `LoadPage` catches exceptions from `IFindService`, logs them, and sets `HasError`. The results area then shows "Search is unavailable right now. Please try again later." in an alert, as `MovieRatingBox` does. The next search clears `HasError`.
- **Lead text:** "Find pages by their title, description or content."
- **Dates:** `hit.Changed` formatted `d MMMM yyyy` in the page culture, with `datetime="yyyy-MM-dd"`.

### Texts

`Resources/Translations/Views.xml` gets a `<find>` element in both languages with: `heading`, `lead`, `placeholder`, `submit`, `searching`, `resultsfor`, `hit`, `hits`, `readmore`, `pagination`, `previous`, `next`, `starttyping`, `noresultsfound`, `unavailable` and `navlink`. The Swedish texts are taken from umbraco26's `AddFindDictionaryItems` migration where they exist.

### Layout

- `LayoutModel` gets `string? FindPageUrl`. `PageViewContextFactory.GetLayoutModel` sets it to the URL of the first published `FindPage` among the start page's children (`IPublishedStateAssessor`, `IUrlResolver`), or null.
- `_Root.cshtml` shows a link with a magnifying glass and the `/find/navlink` text ("Sök" / "Search") at the right of the navbar when `FindPageUrl` is set, and links `~/css/find-page.css`.
- `wwwroot/css/find-page.css` holds the find-specific rules from umbraco26's `find.css` (`.find-results-heading:focus`, `.find-hit-title a:hover`, `.find-hit-description` 3-line clamp). The existing `find.css` holds the movie-card styles and stays as it is; any rule the component needs that is only in umbraco26's `find.css` moves to `find-page.css`.

### Files

| New | Changed |
|---|---|
| `Models/Pages/ArticlePage.cs`, `Controllers/ArticlePageController.cs`, `Views/ArticlePage/Index.cshtml` | `Optimizely26.csproj` (Lucene packages) |
| `Models/Pages/FindPage.cs`, `Controllers/FindPageController.cs`, `Views/FindPage/Index.cshtml`, `Models/ViewModels/FindPageViewModel.cs` | `Models/Pages/StartPage.cs` (allowed child types) |
| `Initialization/FindPageInitialization.cs`, `Initialization/SearchIndexInitialization.cs` | `Startup.cs` (registrations) |
| `Business/Search/SearchIndex.cs`, `SearchDocumentFactory.cs`, `SearchIndexQueue.cs`, `SearchIndexWorker.cs` | `Models/ViewModels/LayoutModel.cs`, `Business/PageViewContextFactory.cs` (`FindPageUrl`) |
| `Business/ScheduledJobs/RebuildSearchIndex.cs` | `Views/Shared/Layouts/_Root.cshtml` (navbar link, `find-page.css`) |
| `Services/Interfaces/IFindService.cs`, `Services/FindService.cs`, `Models/Find/FindResult.cs`, `Models/Find/Hit.cs` | `Resources/Translations/Views.xml` (`<find>` texts) |
| `Blazor/FindSearchPage.razor`, `wwwroot/js/find-page.js`, `wwwroot/css/find-page.css` | |

## Verification

1. A clean rebuild (`--no-incremental`) with 0 warnings and 0 errors.
2. Startup: the find page exists in sv ("Sök") and en ("Search"), the navbar link points to it in each language, and `App_Data/SearchIndex` has been created.
3. Browser, in both sv and en:
   - Create two articles with different words in the name, the meta description and `MainBody`, and publish them in both languages. Each word finds the right article, only in its own language, and a name match ranks above a body match.
   - A two-word search where only one word occurs gives no hit. A three-letter prefix ("art") finds "article".
   - Input like `"`, `*`, `AND` or `title:x` gives results or the empty state, never an error.
   - Unpublish an article: it disappears. Publish it again: it comes back.
   - Move an article to the trash: it and its child articles disappear. Restore it: they come back.
   - Remove Everyone's read access from an article: it disappears.
   - Set an article's stop-publish to a minute ahead: after that minute it is no longer found.
   - More than 10 hits: pagination works, the address bar shows `?q=…&page=…`, and reloading or opening that URL shows the same page. `?page=99` shows the last page.
4. Stop the site, delete `App_Data/SearchIndex`, start it again: the index is rebuilt and searches work.
5. Run the "Rebuild search index" job from admin (if the scheduler setting allows it in Development): it reports the number of indexed page languages.
