using EPiServer.Data.Dynamic;
using Optimizely26.Models.Setup;

namespace Optimizely26.Business
{
	/// <summary>Reads and writes <see cref="SetupMarker"/>s for the initialization modules that create content once.</summary>
	public class SetupMarkers(DynamicDataStoreFactory dataStoreFactory)
	{
		private readonly DynamicDataStoreFactory _dataStoreFactory = dataStoreFactory;

		// The Dynamic Data Store LINQ provider has no Any(predicate); Where + Count translates to SQL
		public bool Exists(string name) => GetStore().Items<SetupMarker>().Where(marker => marker.Name == name).Count() > 0;

		public void Add(string name)
		{
			GetStore().Save(new SetupMarker { Name = name, CreatedUtc = DateTime.UtcNow });
		}

		private DynamicDataStore GetStore() => _dataStoreFactory.GetStore(typeof(SetupMarker)) ?? _dataStoreFactory.CreateStore(typeof(SetupMarker));
	}
}
