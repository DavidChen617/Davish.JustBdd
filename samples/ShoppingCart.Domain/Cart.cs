using System.ComponentModel;

namespace ShoppingCart.Domain;

public sealed record CartLine(Product Product, int Quantity)
{
    public decimal Subtotal => Product.Price * Quantity;
}

public sealed record Order(Guid Id, IReadOnlyList<CartLine> Lines, decimal Total);

/// <param name="Sku">The product whose line changed; null when a coupon changed.</param>
public sealed class CartChangedEventArgs(string? sku) : EventArgs
{
    public string? Sku { get; } = sku;
}

public sealed class Cart : INotifyPropertyChanged
{
    /// <summary>Raised after a line or the coupon changes.</summary>
    public event EventHandler<CartChangedEventArgs>? Changed;

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly List<CartLine> _lines = [];

    public IReadOnlyList<CartLine> Lines => _lines;
    public Coupon? Coupon { get; private set; }
    public bool IsCheckedOut { get; private set; }

    public decimal Subtotal => _lines.Sum(l => l.Subtotal);
    public decimal Discount => Coupon is null ? 0 : Math.Round(Subtotal * Coupon.Percent / 100, 2);
    public decimal Total => Subtotal - Discount;

    /// <summary>Adding a product that is already in the cart merges the quantities.</summary>
    public Cart Add(Product product, int quantity = 1)
    {
        EnsureOpen();
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");

        var index = _lines.FindIndex(l => l.Product.Sku == product.Sku);
        if (index < 0) _lines.Add(new CartLine(product, quantity));
        else _lines[index] = _lines[index] with { Quantity = _lines[index].Quantity + quantity };
        OnChanged(product.Sku);
        return this;
    }

    public Cart Remove(string sku)
    {
        EnsureOpen();
        if (_lines.RemoveAll(l => l.Product.Sku == sku) == 0)
            throw new KeyNotFoundException($"Product '{sku}' is not in the cart.");
        OnChanged(sku);
        return this;
    }

    /// <summary>A cart holds one coupon; applying another replaces the previous one.</summary>
    public Cart Apply(Coupon coupon)
    {
        EnsureOpen();
        Coupon = coupon;
        OnChanged(null);
        return this;
    }

    public Order Checkout()
    {
        EnsureOpen();
        if (_lines.Count == 0) throw new InvalidOperationException("Cannot check out an empty cart.");
        IsCheckedOut = true;
        return new Order(Guid.NewGuid(), _lines.ToArray(), Total);
    }

    private void OnChanged(string? sku)
    {
        Changed?.Invoke(this, new CartChangedEventArgs(sku));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
    }

    private void EnsureOpen()
    {
        if (IsCheckedOut) throw new InvalidOperationException("The cart has already been checked out.");
    }
}
