namespace Inventory.Domain.Stock;

public enum ReservationStatus
{
    Reserved = 1,
    Released = 2
}

public sealed class StockReservation
{
    private StockReservation() { }

    public StockReservation(Guid orderId, string sku, int quantity)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Sku = sku;
        Quantity = quantity;
        Status = ReservationStatus.Reserved;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }

    public void MarkReleased() => Status = ReservationStatus.Released;
}
