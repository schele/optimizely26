namespace Optimizely26.Models.ViewModels
{
    /// <summary>One link in the top menu.</summary>
    /// <param name="IsCurrent">The visitor is on this page or on a page below it.</param>
    public record MenuItem(string Name, string Url, bool IsCurrent);
}
