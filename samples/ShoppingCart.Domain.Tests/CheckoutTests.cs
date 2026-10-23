namespace ShoppingCart.Domain.Tests;

[Feature<CheckoutFeature>]
public class CheckoutTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);
    private static readonly Product Pen = new("PEN-1", "Fountain pen", 300m);

    [Fact]
    [Trait("Category", "Smoke")]
    public void Checkout_creates_an_order_with_the_discounted_total()
    {
        new Scenario("Check out with a coupon")
            .Given("The cart contains a book and a pen with a 10% coupon applied", () => new Cart().Add(Book).Add(Pen).Apply(new Coupon("OFF10", 10)))
            .When("The customer checks out", cart => (Cart: cart, Order: cart.Checkout()))
            .Then("An order is created")
                .NotEqual("The order has an ID", Guid.Empty, x => x.Order.Id)
                .Equal("The order contains two line items", 2, x => x.Order.Lines.Count)
                .Equal("The total is (1200 + 300) × 90%", 1350m, x => x.Order.Total, 2)
            .And("The cart is checked out", x => Assert.True(x.Cart.IsCheckedOut))
            .Run();
    }

    [Fact]
    [Trait("Category", "Smoke")]
    [Trait("Owner", "team-a")]
    public void Empty_cart_cannot_be_checked_out()
    {
        new Scenario("Check out an empty cart")
            .Given("An empty shopping cart", () => new Cart())
            .When("The customer checks out", cart => cart.Checkout())
            .ThenThrows<InvalidOperationException>("Checkout is rejected")
            .Then("The cart remains open", t => Assert.False(t.State.IsCheckedOut))
            .Run();
    }

    [Fact]
    public void Checked_out_cart_cannot_be_changed()
    {
        // Nested scenario: another scenario prepares the cart and runs as the Given step.
        new Scenario("A checked-out cart cannot be changed")
            .Given("A checked-out cart",
                new Scenario("Prepare a checked-out cart")
                    .Given("The cart contains a book", () => new Cart().Add(Book))
                    .When("The customer checks out", cart => { cart.Checkout(); })   // Preserve the cart state instead of returning the order.
                    .Then("The cart is checked out", cart => Assert.True(cart.IsCheckedOut)))
            .When("A pen is added after checkout", cart => cart.Add(Pen))
            .ThenThrows<InvalidOperationException>("The change is rejected")
            .Contains("The error message explains that the cart is already checked out", "already been checked out", t => t.Exception.Message)
            .Run();
    }
}
