using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Common.Results;
using BuildingBlocks.Contracts;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Clients;

internal static class Failures
{
    public static Error Down(string service) =>
        new(ErrorCodes.DependencyUnavailable, $"{service} abhi available nahi hai.");

    public static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or TimeoutException;
}

public sealed class CatalogClient(HttpClient http) : ICatalogClient
{
    public async Task<Result<decimal>> GetPriceAsync(string sku, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync($"api/v1/products/sku/{Uri.EscapeDataString(sku)}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return Result.Failure<decimal>(new Error("ORDER_PRODUCT_NOT_FOUND", $"SKU '{sku}' catalog mein nahi mila."));
            }

            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<decimal>(Failures.Down("Catalog"));
            }

            var product = await response.Content.ReadFromJsonAsync<ProductPriceDto>(ct);
            return product is null
                ? Result.Failure<decimal>(Failures.Down("Catalog"))
                : Result.Success(product.Price);
        }
        catch (Exception ex) when (Failures.IsTransient(ex))
        {
            return Result.Failure<decimal>(Failures.Down("Catalog"));
        }
    }

    private sealed record ProductPriceDto(decimal Price);
}

public sealed class InventoryClient(HttpClient http) : IInventoryClient
{
    public async Task<Result> ReserveAsync(Guid orderId, IReadOnlyList<(string Sku, int Quantity)> lines, CancellationToken ct)
    {
        var body = new
        {
            orderId,
            items = lines.Select(l => new { sku = l.Sku, quantity = l.Quantity }).ToList()
        };

        try
        {
            using var response = await http.PostAsJsonAsync("api/v1/inventory/reservations", body, ct);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            return response.StatusCode >= HttpStatusCode.InternalServerError
                ? Result.Failure(Failures.Down("Inventory"))
                : Result.Failure(new Error("STOCK_INSUFFICIENT", "Stock kam hai."));
        }
        catch (Exception ex) when (Failures.IsTransient(ex))
        {
            return Result.Failure(Failures.Down("Inventory"));
        }
    }

    public async Task<Result> ReleaseAsync(Guid orderId, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsync($"api/v1/inventory/reservations/{orderId}/release", null, ct);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : Result.Failure(Failures.Down("Inventory"));
        }
        catch (Exception ex) when (Failures.IsTransient(ex))
        {
            return Result.Failure(Failures.Down("Inventory"));
        }
    }
}

public sealed class PaymentClient(HttpClient http) : IPaymentClient
{
    public async Task<Result> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("api/v1/payments", new { orderId, amount }, ct);
            if (response.IsSuccessStatusCode)
            {
                return Result.Success();
            }

            return response.StatusCode == HttpStatusCode.PaymentRequired
                ? Result.Failure(new Error("PAYMENT_DECLINED", "Payment decline hui."))
                : Result.Failure(Failures.Down("Payment"));
        }
        catch (Exception ex) when (Failures.IsTransient(ex))
        {
            return Result.Failure(Failures.Down("Payment"));
        }
    }

    public async Task<Result> RefundAsync(Guid orderId, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsync($"api/v1/payments/orders/{orderId}/refund", null, ct);
            // 404 = koi payment nahi, refund ki zarurat nahi
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound
                ? Result.Success()
                : Result.Failure(Failures.Down("Payment"));
        }
        catch (Exception ex) when (Failures.IsTransient(ex))
        {
            return Result.Failure(Failures.Down("Payment"));
        }
    }
}
