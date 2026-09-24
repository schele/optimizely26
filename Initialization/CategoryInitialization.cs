using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Initialization;
using EPiServer.ServiceLocation;

namespace Optimizely26.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(CmsCoreInitialization))]
    public class CategoryInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            CreateCategories();
        }

        private void CreateCategories()
        {
            var categoryRepository = ServiceLocator.Current.GetInstance<CategoryRepository>();
            var root = categoryRepository.GetRoot();

            if (categoryRepository.Get("Film genres") == null)
            {
                var systemCategory = new Category(root, "Film genres")
                {
                    Available = true,
                    Description = "Film genres",
                    Selectable = false
                };

                categoryRepository.Save(systemCategory);
                var heading = categoryRepository.Get("Film genres");
                var genres = new List<string>()
                {
                    "Horror", "Comedy", "Drama", "Thriller"
                };

                foreach (var genre in genres)
                {
                    var category = new Category(heading, genre)
                    {
                        Available = true,
                        Description = genre,
                        Selectable = true
                    };

                    categoryRepository.Save(category);
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}