namespace ShoppingCart.Domain.Tests;

[Feature<CartFeature>]
public class CartTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);
    private static readonly Product Pen = new("PEN-1", "Fountain pen", 300m);

    [Fact]
    public void Adding_products_updates_the_total()
    {
        new Scenario("Add products to an empty cart")
            .Given("An empty shopping cart", () => new Cart())
            .When("A book and two pens are added", cart => cart.Add(Book).Add(Pen, 2))
            .Then("The cart contains two line items")
                .Equal("There are two line items", 2, cart => cart.Lines.Count)
                .Equal("The subtotal is 1200 + 2 × 300", 1800m, cart => cart.Subtotal, 2)
                .Equal("Without a coupon, the total equals the subtotal", 1800m, cart => cart.Total, 2)
            .Run();
    }

    [Fact]
    public void Adding_the_same_product_twice_merges_the_quantity()
    {
        new Scenario("Add the same product twice")
            .Given("The cart contains one pen", () => new Cart().Add(Pen))
            .When("Two more pens are added", cart => cart.Add(Pen, 2))
            .Then("The cart still contains one line item", cart => Assert.Single(cart.Lines))
            .Then("The quantities are merged into three", cart => Assert.Equal(3, cart.Lines[0].Quantity))
            .Run();
    }

    [Fact]
    public void Removing_a_product_drops_its_line()
    {
        new Scenario("Remove a product")
            .Given("The cart contains a book and a pen", () => new Cart().Add(Book).Add(Pen))
            .When("The pen is removed", cart => cart.Remove(Pen.Sku))
            .Then("Only the book remains")
                .Single("There is one line item", cart => cart.Lines)
                .Equal("The remaining product is the book", Book, cart => cart.Lines[0].Product)
            .Run();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Non_positive_quantity_is_rejected(int quantity)
    {
        new Scenario($"Add a product with a quantity of {quantity}")
            .Given("An empty shopping cart", () => new Cart())
            .When("A book is added with an invalid quantity", cart => cart.Add(Book, quantity))
            .ThenThrows<ArgumentOutOfRangeException>("The invalid quantity is rejected")
            .Then("The cart remains empty", t => Assert.Empty(t.State.Lines))
            .Run();
    }

    [Fact]
    public void Removing_a_product_that_is_not_in_the_cart_fails()
    {
        new Scenario("Remove a product that is not in the cart")
            .Given("The cart contains a book", () => new Cart().Add(Book))
            .When("The product with SKU NOPE is removed", cart => cart.Remove("NOPE"))
            .ThenThrows<KeyNotFoundException>("The missing product is reported")
            .Contains("The error message identifies the missing SKU", "NOPE", t => t.Exception.Message)
            .Run();
    }

    // A regular xUnit test does not appear in the Davish.JustBdd report; see the native report for its result.
    [Fact]
    public void Plain_xunit_test_is_not_in_the_report()
        => Assert.Equal(0m, new Cart().Total);
}
