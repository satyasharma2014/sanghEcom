using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Abstractions;
using Payment.Application.Payments;
using Payment.Infrastructure.Gateway;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<PaymentDbContext>(o => o.UseNpgsql(connectionString));
        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<PaymentService>();
        return services;
    }
}
