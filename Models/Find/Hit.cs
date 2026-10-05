namespace Optimizely26.Models.Find
{
	public class Hit
	{
		public string Name { get; init; } = string.Empty;

		public string Description { get; init; } = string.Empty;

		public string Url { get; init; } = string.Empty;

		/// <summary>When an editor last marked the page as changed.</summary>
		public DateTime Changed { get; init; }
	}
}
