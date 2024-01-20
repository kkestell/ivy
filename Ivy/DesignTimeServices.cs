using Microsoft.Extensions.DependencyInjection;

namespace Ivy;

public static class DesignTimeServices
{
    private static IServiceProvider? _rawServices;

    private static readonly Lazy<IServiceScope> Scope = new(() =>
    {
        var serviceCollection = new ServiceCollection();
        App.ConfigureServices(serviceCollection);
        _rawServices = serviceCollection.BuildServiceProvider();
        return _rawServices.CreateScope();
    });

    public static IServiceProvider Services => Scope.Value.ServiceProvider;
}