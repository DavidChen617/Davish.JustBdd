namespace ShoppingCart.Domain;

/// <summary>A percentage discount coupon; for example, 10 takes 10% off.</summary>
public sealed record Coupon
{
    public Coupon(string code, decimal percent)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Coupon code is required.", nameof(code));
        if (percent is <= 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percent), percent, "Percent must be between 0 (exclusive) and 100.");
        Code = code;
        Percent = percent;
    }

    public string Code { get; }
    public decimal Percent { get; }
}
