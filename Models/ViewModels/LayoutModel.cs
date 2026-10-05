using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public class LayoutModel
    {
        public StartPage? StartPage { get; set; }

        public SettingsPage? SettingsPage { get; set; }

        /// <summary>The published find page in the current language, for the navbar; null when there is none.</summary>
        public string? FindPageUrl { get; set; }
    }
}