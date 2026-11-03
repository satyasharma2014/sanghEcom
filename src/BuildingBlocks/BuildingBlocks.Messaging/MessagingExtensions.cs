using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddSanghMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
                      ?? new RabbitMqOptions();

        services.AddSingleton(options);
        services.AddSingleton<RabbitMqPublisher>();
        return services;
    }
}
