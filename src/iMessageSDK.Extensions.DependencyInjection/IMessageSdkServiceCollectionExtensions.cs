using System.Runtime.Versioning;
using iMessageSDK;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering iMessageSDK with a dependency injection container.
/// </summary>
[SupportedOSPlatform("macos")]
public static class IMessageSdkServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="IMessageClient"/> singleton with the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="configure">An optional callback to configure <see cref="MessageClientOptions"/>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <remarks>
    /// The client is created the first time it is resolved, which briefly blocks the resolving
    /// thread: <see cref="MessageClient.CreateAsync"/> only opens a local file and runs a couple
    /// of quick checks, not a network call, so this is the same tradeoff many other
    /// "expensive singleton" integrations make. Applications that want a non-blocking startup
    /// path should call <see cref="MessageClient.CreateAsync"/> directly instead of using this
    /// registration.
    /// </remarks>
    public static IServiceCollection AddIMessageSdk(this IServiceCollection services, Action<MessageClientOptions>? configure = null)
    {
        services.AddSingleton<IMessageClient>(_ =>
        {
            var options = new MessageClientOptions();
            configure?.Invoke(options);
            return MessageClient.CreateAsync(options).GetAwaiter().GetResult();
        });

        return services;
    }
}
