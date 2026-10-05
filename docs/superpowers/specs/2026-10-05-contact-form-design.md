# Contact form block

Date: 2026-10-05
Status: Decided by the implementer (the user asked for the recommended choices without check-ins)

## Goal

A "Contact" article page with a form block where visitors send their name, email and a comment. Everything is created in code at startup. Submissions are stored in the Dynamic Data Store with the email encrypted, and admins see them on a page in the CMS menu. umbraco26 gets the same feature with EF Core (separate work in that repo).

**Done when:** the Contact page exists in sv and en, a valid submission is stored with an encrypted email and shows up on the admin page, and bots that fill the honeypot, post too fast or tamper with the form get a normal-looking response but store nothing.

## Decisions

| Question | Decision |
|---|---|
| Where the form lives | `ContactFormBlock` (heading, intro) in a new `MainContentArea` on `ArticlePage`. The area is not culture-specific; the block's texts are. |
| How the page is created | `ContactPageInitialization`: an article "Kontakt" (sv) / "Contact" (en) with the block in its "For this page" folder. Skipped when any article under the start page already holds a contact form block. |
| Posting | A classic form POST (works without JavaScript) to `/contact-form/submit`, then Post/Redirect/Get back to the page with `?contact=sent|invalid|expired#contact-form`. The redirect target is resolved from the posted page id with `IUrlResolver`, so it can only ever be a page on this site. |
| Anti-forgery | ASP.NET Core anti-forgery token, validated with `IAntiforgery` so a failure redirects with `contact=expired` instead of a bare 400. |
| Honeypot | A `Website` text field moved off-screen (not `display:none`), `tabindex="-1"`, `autocomplete="off"`, `aria-hidden`. Any value: log, store nothing, answer "sent". |
| "Hash security" | A signed form token: `ITimeLimitedDataProtector` (HMAC-authenticated encryption) over `{pageId}|{language}|{issuedUtcTicks}`, valid for 2 hours. Tampered or expired → `contact=expired`. Posted less than 3 seconds after the form was rendered → treated as a bot like the honeypot. The page id in the token must match the posted page id. |
| Email at rest | Encrypted with `IDataProtector` (purpose `Optimizely26.ContactSubmission.Email`) and decrypted only for the admin page. |
| Data protection keys | Persisted to `App_Data/DataProtection-Keys` with application name `Optimizely26`, so encrypted emails survive restarts and deployments on the same server. Keys aren't DPAPI-protected (they would break if the app pool identity changes). Existing login cookies become invalid once; editors log in again. |
| Storage | Dynamic Data Store class `ContactSubmission` (store `ContactSubmissions`): name, encrypted email, comment, page id, language, created (UTC, indexed). |
| Admin add-on | `/contact-submissions`, `CmsAdmin` policy, newest first, 20 per page, linked from the CMS top menu ("Contact submissions") through an `IMenuProvider`. CMS 13 has no platform-navigation HTML helpers, so it's a standalone page with a link back to the CMS. |
| Validation | Name required ≤ 100, email required and valid ≤ 254, comment required ≤ 2000, enforced in HTML and again on the server. |
| Texts | `Views.xml` under `/contactform/`, sv and en. |
| Tests | No test project (as for the rest of the site); verified with real POSTs against the running site and a check of the stored data. |

## Out of scope

Email notifications, rate limiting per IP, deleting or exporting submissions, CAPTCHA, Optimizely Forms.
