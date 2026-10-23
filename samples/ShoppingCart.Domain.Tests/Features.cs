namespace ShoppingCart.Domain.Tests;

public sealed class CartFeature : IFeature
{
    public string Name => "Shopping cart";
    public string? Description => """
        As a customer,
        I want to add and remove products,
        so that the cart holds what I intend to buy.
        """;
}

public sealed class CheckoutFeature : IFeature
{
    public string Name => "Checkout";
    public string? Description => """
        As a customer,
        I want to check out my cart,
        so that an order is created with the discounted total.
        """;
    public IReadOnlyList<FeatureLink> Links => [new("Checkout spec (example)", "https://example.com/specs/checkout")];
    public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> Metadata =>
    [
        new("Owner", ["team-checkout"]),
        new("Priority", ["High"]),
    ];
}
