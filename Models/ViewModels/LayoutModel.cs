using Optimizely26.Models.Pages;

namespace Optimizely26.Models.ViewModels
{
    public class LayoutModel
    {
        public StartPage? StartPage { get; set; }

        public SettingsPage? SettingsPage { get; set; }

        /// <summary>The published find page in the current language, for the navbar; null when there is none.</summary>
        public string? FindPageUrl { get; set; }

        /// <summary>The pages picked on the settings page that the visitor can open, in the order editors picked them.</summary>
        public IReadOnlyList<MenuItem> MenuItems { get; set; } = [];
    }
}