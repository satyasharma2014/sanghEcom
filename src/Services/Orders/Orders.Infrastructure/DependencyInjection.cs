//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Orders.Application.Abstractions;
//using Orders.Infrastructure.Persistence;

//namespace Orders.Infrastructure;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, string connectionString)
//    {
//        services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(connectionString));
//        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
//        services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();
//        return services;
//    }
//}
using BuildingBlocks.Resilience;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Clients;
using Orders.Infrastructure.Outbox;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrdersDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("OrdersDb")!));

        services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
        services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();
        services.AddScoped<IOutboxWriter, OrdersOutboxWriter>();

        services.AddScoped<Orders.Application.Recovery.CompensationRecoveryService>();
        services.AddHostedService<Orders.Infrastructure.Recovery.CompensationRecoveryWorker>();

        var resilience = config.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>()
                         ?? new ResilienceOptions();
        var urls = config.GetSection("Services");

        services.AddHttpClient<ICatalogClient, CatalogClient>(c => c.BaseAddress = new Uri(urls["Catalog"]!))
            .AddSanghResilience(resilience);
        services.AddHttpClient<IInventoryClient, InventoryClient>(c => c.BaseAddress = new Uri(urls["Inventory"]!))
            .AddSanghResilience(resilience);
        services.AddHttpClient<IPaymentClient, PaymentClient>(c => c.BaseAddress = new Uri(urls["Payment"]!))
            .AddSanghResilience(resilience);

        return services;
    }
}
