namespace UndercutF1.Console;

public static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddInputHandlers(this IServiceCollection services)
    {
        services.AddInputHandlersCore();
        return services;
    }

    public static IServiceCollection AddDisplays(this IServiceCollection services)
    {
        services.AddDisplaysCore();

        services.AddSingleton<CommonDisplayComponents>();
        services.AddSingleton<LogDisplayOptions>();
        services.AddSingleton<StartSimulatedSessionOptions>();

        return services;
    }
}
