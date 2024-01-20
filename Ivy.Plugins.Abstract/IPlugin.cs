using Microsoft.Extensions.DependencyInjection;

namespace Ivy.Plugins.Abstract;

public interface IPlugin
{
    void Initialize(IPluginHost host, IServiceCollection serviceCollection);
}