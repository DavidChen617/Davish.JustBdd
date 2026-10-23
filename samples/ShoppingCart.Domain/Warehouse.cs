namespace ShoppingCart.Domain;

public sealed record Reservation(Guid OrderId, IReadOnlyList<string> Skus);

/// <summary>Reserves stock for an order. Asynchronous, like a call to an inventory service.</summary>
public sealed class Warehouse
{
    private readonly Dictionary<string, int> _stock = [];

    public Warehouse Stock(string sku, int quantity)
    {
        _stock[sku] = _stock.TryGetValue(sku, out var current) ? current + quantity : quantity;
        return this;
    }

    public int InStock(string sku) => _stock.TryGetValue(sku, out var quantity) ? quantity : 0;

    public async Task<Reservation> ReserveAsync(Order order)
    {
        await Task.Yield();   // Stands in for a network call.
        foreach (var line in order.Lines)
        {
            if (InStock(line.Product.Sku) < line.Quantity)
                throw new InvalidOperationException($"Not enough stock for '{line.Product.Sku}'.");
        }
        foreach (var line in order.Lines) _stock[line.Product.Sku] -= line.Quantity;
        return new Reservation(order.Id, order.Lines.Select(l => l.Product.Sku).ToArray());
    }
}
