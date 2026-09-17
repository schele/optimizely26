using System.Globalization;
using EPiServer.Applications;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
    /// <summary>
    /// Creates the application on an empty database and binds its hosts to Swedish.
    /// Optimizely omits the language URL segment for the language a host is bound to, and derives
    /// the segment from the language id for every other enabled language. That gives "/" for
    /// Swedish and "/en" for English.
    /// </summary>
    /// <remarks>
    /// The application needs one host with a real authority: <see cref="ApplicationHost.Url"/> is
    /// derived from the authority, so a wildcard-only host list leaves the application without a
    /// URL and content URL generation throws. The authority comes from "Site:PrimaryHost" when set,
    /// otherwise from the first address in "urls" (ASPNETCORE_URLS).
    /// </remarks>
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    [ModuleDependency(typeof(LanguageSetupInitialization))]
    public class SiteSetupInitialization : IInitializableModule
    {
        private const string WildcardAuthority = "*";
        private const string DefaultLanguageCode = "sv";
        private const string ApplicationName = "Optimizely26";
        private const string PrimaryHostConfigurationKey = "Site:PrimaryHost";

        public void Initialize(InitializationEngine context)
        {
            var applicationRepository = context.Services.GetRequiredService<IApplicationRepository>();
            var configuration = context.Services.GetRequiredService<IConfiguration>();
            var locale = CultureInfo.GetCultureInfo(DefaultLanguageCode);
            var primaryHost = ResolvePrimaryHost(configuration, locale);
            var application = applicationRepository.List().FirstOrDefault(candidate => candidate is IRoutableApplication);

            if (application == null)
            {
                CreateApplication(context, applicationRepository, locale, primaryHost);

                return;
            }

            EnsureHosts(applicationRepository, application, locale, primaryHost);
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        /// <summary>
        /// Builds the host that gives the application its URL, or null when no usable authority is
        /// configured. Wildcard bindings such as "http://+:80" cannot be used as an authority.
        /// </summary>
        private static ApplicationHost? ResolvePrimaryHost(IConfiguration configuration, CultureInfo locale)
        {
            var configured = configuration[PrimaryHostConfigurationKey];

            var address = string.IsNullOrWhiteSpace(configured)
                ? configuration["urls"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
                : configured;

            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            // The value may be a full address ("https://localhost:5000") or a bare authority
            // ("www.example.se"); only the former carries a scheme.
            if (!Uri.TryCreate(address, UriKind.Absolute, out var parsedAddress))
            {
                return IsUsableAuthority(address)
                    ? new ApplicationHost(address) { Type = ApplicationHostType.Primary, Locale = locale }
                    : null;
            }

            if (!IsUsableAuthority(parsedAddress.Authority))
            {
                return null;
            }

            return new ApplicationHost(parsedAddress.Authority)
            {
                Type = ApplicationHostType.Primary,
                Locale = locale,
                PreferredUrlScheme = parsedAddress.Scheme == Uri.UriSchemeHttps ? UrlScheme.Https : UrlScheme.Http
            };
        }

        private static void CreateApplication(
            InitializationEngine context,
            IApplicationRepository applicationRepository,
            CultureInfo locale,
            ApplicationHost? primaryHost)
        {
            if (primaryHost == null)
            {
                // Without a real authority the application would have no URL, so leave it uncreated
                // rather than store one that breaks URL generation.
                return;
            }

            var contentRepository = context.Services.GetRequiredService<IContentRepository>();
            var loaderOptions = new LoaderOptions { LanguageLoaderOption.FallbackWithMaster(locale) };
            var startPage = contentRepository.GetChildren<StartPage>(ContentReference.RootPage, loaderOptions).FirstOrDefault();

            if (startPage == null)
            {
                // Nothing to serve yet; the application is created on a later startup.
                return;
            }

            var application = new InProcessWebsite(ApplicationName, startPage.ContentLink)
            {
                DisplayName = ApplicationName
            };

            ApplyHosts(application, locale, primaryHost);

            applicationRepository.SaveAsync(application, CancellationToken.None).GetAwaiter().GetResult();
            applicationRepository.MakeDefaultAsync(application, true, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void EnsureHosts(
            IApplicationRepository applicationRepository,
            Application application,
            CultureInfo locale,
            ApplicationHost? primaryHost)
        {
            if (application is not IRoutableApplication routableApplication || HasExpectedHosts(routableApplication, locale, primaryHost))
            {
                return;
            }

            var writableApplication = application.CreateWritableClone();

            if (writableApplication is not IRoutableApplication writableRoutableApplication)
            {
                return;
            }

            ApplyHosts(writableRoutableApplication, locale, primaryHost);

            applicationRepository.SaveAsync(writableApplication, CancellationToken.None).GetAwaiter().GetResult();
        }

        private static void ApplyHosts(IRoutableApplication application, CultureInfo locale, ApplicationHost? primaryHost)
        {
            var existingWildcard = FindHost(application, WildcardAuthority);

            if (existingWildcard != null)
            {
                application.Hosts.Remove(existingWildcard);
            }

            application.Hosts.Add(new ApplicationHost(WildcardAuthority)
            {
                Locale = locale,
                Type = existingWildcard?.Type ?? ApplicationHostType.Default
            });

            if (primaryHost == null)
            {
                return;
            }

            var existingPrimary = FindHost(application, primaryHost.Authority);

            if (existingPrimary != null)
            {
                application.Hosts.Remove(existingPrimary);
            }

            application.Hosts.Add(primaryHost);
        }

        private static bool HasExpectedHosts(IRoutableApplication application, CultureInfo locale, ApplicationHost? primaryHost)
        {
            if (!IsBoundTo(FindHost(application, WildcardAuthority), locale))
            {
                return false;
            }

            if (primaryHost == null)
            {
                return true;
            }

            var existingPrimary = FindHost(application, primaryHost.Authority);

            return IsBoundTo(existingPrimary, locale) && existingPrimary!.Type == ApplicationHostType.Primary;
        }

        /// <summary>
        /// Kestrel binds wildcards such as "http://+:80" and "http://*:5000"; those are not usable
        /// as a host authority because no absolute URL can be built from them.
        /// </summary>
        private static bool IsUsableAuthority(string authority) =>
            !authority.Contains('*') && !authority.Contains('+');

        private static ApplicationHost? FindHost(IRoutableApplication application, string authority) =>
            application.Hosts.FirstOrDefault(host => string.Equals(host.Authority, authority, StringComparison.OrdinalIgnoreCase));

        private static bool IsBoundTo(ApplicationHost? host, CultureInfo locale) =>
            host != null && string.Equals(host.Locale?.Name, locale.Name, StringComparison.OrdinalIgnoreCase);
    }
}
