using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Optimizely26.Models.Setup
{
	/// <summary>Remembers that a one-time setup step has run, so content an editor deleted isn't created again on the next startup.</summary>
	/// <remarks>Stored in the database with the content, so a fresh database gets the setup again.</remarks>
	[EPiServerDataStore(StoreName = "SetupMarkers", AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
	public class SetupMarker : IDynamicData
	{
		public Identity Id { get; set; } = Identity.NewIdentity();

		[EPiServerDataIndex]
		public string Name { get; set; } = string.Empty;

		public DateTime CreatedUtc { get; set; }
	}
}
