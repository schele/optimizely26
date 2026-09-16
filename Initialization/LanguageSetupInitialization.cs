using System.Globalization;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace Optimizely26.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class LanguageSetupInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var languageBranchRepository = context.Services.GetRequiredService<ILanguageBranchRepository>();

            EnsureLanguage(languageBranchRepository, "sv", string.Empty);
            EnsureLanguage(languageBranchRepository, "en", "en");
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        private static void EnsureLanguage(ILanguageBranchRepository languageBranchRepository, string cultureCode, string urlSegment)
        {
            var culture = CultureInfo.GetCultureInfo(cultureCode);
            var existingLanguage = languageBranchRepository.Load(cultureCode);

            if (existingLanguage == null)
            {
                var languageBranch = new LanguageBranch(culture)
                {
                    Enabled = true,
                    URLSegment = urlSegment
                };

                languageBranchRepository.Save(languageBranch);

                return;
            }

            var writableLanguage = existingLanguage.CreateWritableClone();
            var hasChanges = false;

            if (!writableLanguage.Enabled)
            {
                writableLanguage.Enabled = true;
                hasChanges = true;
            }

            if (!string.Equals(writableLanguage.URLSegment, urlSegment, StringComparison.OrdinalIgnoreCase))
            {
                writableLanguage.URLSegment = urlSegment;
                hasChanges = true;
            }

            if (hasChanges)
            {
                languageBranchRepository.Save(writableLanguage);
            }
        }
    }
}