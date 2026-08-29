using GenomeTrack.Application.Services.Implementation;
using GenomeTrack.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GenomeTrack.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISampleService, SampleService>();
        services.AddScoped<ICustodyService, CustodyService>();
        services.AddScoped<ISequencingRunService, SequencingRunService>();
        services.AddScoped<IVariantService, VariantService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
