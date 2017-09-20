using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Search.Configuration;
using EPiServer.ServiceLocation;

namespace EPiServer.Search.Initialization.Internal
{
    [ModuleDependency(typeof(Web.InitializationModule))]
    internal class AspNetSearchInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<EPiServer.Configuration.Transform.Internal.IConfigurationTransform>(s => 
                new EPiServer.Configuration.Transform.Internal.SearchOptionsTransform(s.GetInstance<SearchOptions>(), SearchSection.Instance));
        }

        public void Initialize(InitializationEngine context) { }

        public void Uninitialize(InitializationEngine context) { }
    }
}
