namespace ShoppingCart.Domain.Tests;

// Event checks: ThenRaises, ThenPropertyChanged, and ThenNotRaises observe the When input while When runs.
[Feature<CartFeature>]
public class CartEventTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);

    [Fact]
    public void Adding_a_product_raises_Changed()
    {
        new Scenario("Adding a product notifies listeners")
            .Given("An empty shopping cart", () => new Cart())
            .When("A book is added", cart => { cart.Add(Book); })
            .ThenRaises<CartChangedEventArgs>("Changed is raised", (cart, h) => cart.Changed += h, (cart, h) => cart.Changed -= h)
            .Then("The event names the book", raised => Assert.Equal(Book.Sku, raised.Args.Sku))
            .Run();
    }

    [Fact]
    public void Applying_a_coupon_reports_a_new_total()
    {
        new Scenario("Applying a coupon changes the total")
            .Given("The cart contains a book", () => new Cart().Add(Book))
            .When("A 10% coupon is applied", cart => { cart.Apply(new Coupon("OFF10", 10)); })
            .ThenPropertyChanged("Total changes", cart => cart, nameof(Cart.Total))
            .Then("The new total is 1080", raised => Assert.Equal(1080m, raised.State.Total))
            .Run();
    }

    [Fact]
    public void Checking_out_changes_no_line()
    {
        new Scenario("Checking out leaves the lines unchanged")
            .Given("The cart contains a book", () => new Cart().Add(Book))
            .When("The customer checks out", cart => cart.Checkout())
            .ThenNotRaises<CartChangedEventArgs>("No line changes", (cart, h) => cart.Changed += h, (cart, h) => cart.Changed -= h)
            .Then("The order holds the book", order => Assert.Equal(Book, Assert.Single(order.Lines).Product))
            .Run();
    }
}
