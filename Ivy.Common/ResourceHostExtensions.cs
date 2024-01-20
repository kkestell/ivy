using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;

using Microsoft.Extensions.DependencyInjection;

namespace Ivy.Common;

public static class ResourceHostExtensions
{
    public static IServiceProvider GetServiceProvider()
    {
        Debug.Assert(Application.Current is not null);

        if (Application.Current.Resources.TryGetValue(typeof(IServiceProvider), out var result))
        {
            Debug.Assert(result is not null);

            return (IServiceProvider)result;
        }

        throw new InvalidOperationException("No IServiceProvider found");
    }

    public static T CreateInstance<T>(this IResourceHost control)
    {
        return ActivatorUtilities.CreateInstance<T>(GetServiceProvider());
    }
}
