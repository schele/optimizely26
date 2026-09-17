using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Optimizely26.Models.Pages;

namespace Optimizely26.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class RootPageInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var contentTypeRepository = context.Services.GetRequiredService<IContentTypeRepository>();
            var sysRoot = contentTypeRepository.Load("SysRoot") as PageType;
            var setting = new AvailableSetting { Availability = Availability.Specific };

            setting.AllowedContentTypeNames.Add(contentTypeRepository.Load<StartPage>().Name);

            var availableSettingsRepository = context.Services.GetRequiredService<IAvailableSettingsRepository>();
            availableSettingsRepository.RegisterSetting(sysRoot, setting);
        }

        public void Uninitialize(InitializationEngine context)
        {            
        }
    }
}