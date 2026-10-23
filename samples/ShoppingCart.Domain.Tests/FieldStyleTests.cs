namespace ShoppingCart.Domain.Tests;

[Feature<CartFeature>]
public class FieldStyleTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);
    private static readonly Product Pen = new("PEN-1", "Fountain pen", 300m);

    // xUnit creates a new instance for every test, so fields are not shared between tests.
    private readonly Cart _cart = new();
    private Order? _order;

    [Fact]
    public void Steps_can_share_state_through_fields()
    {
        // Steps without parameters: the state lives in the test class, like BDDfy.
        new Scenario("Steps share state through test fields")
            .Given("The cart contains a book", () => { _cart.Add(Book); })
            .When("The customer checks out", () => { _order = _cart.Checkout(); })
            .Then("The cart is closed", () => Assert.True(_cart.IsCheckedOut))
            .And("The order holds the book", () => Assert.Equal(Book, _order!.Lines[0].Product))
            .Run();
    }

    [Fact]
    [Feature("Pricing", Description = "How line subtotals are computed.")]   // A method feature overrides the class feature.
    public void Descriptions_are_optional()
    {
        // Without descriptions, the scenario takes the method name and each step shows its code in the report.
        new Scenario()
            .Given(() => new Cart().Add(Pen, 3))
            .Then(cart => Assert.Equal(900m, cart.Subtotal))
            .Run();
    }
}
