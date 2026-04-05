using KBlazor.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KBlazor.Extensions;

/// <summary>
/// Extension methods for registering KBlazor services.
/// </summary>
public static class KBlazorServiceExtensions
{
    /// <summary>
    /// Registers the KBlazor machine-locked license provider as a singleton.
    /// The provider looks for a <c>kblazor.lic</c> file in the application directory
    /// and validates its RSA signature against the embedded public key.
    /// </summary>
    public static IServiceCollection AddKBlazorLicensing(this IServiceCollection services)
    {
        services.AddSingleton<ILicenseProvider, KBlazorLicenseProvider>();
        return services;
    }
}
