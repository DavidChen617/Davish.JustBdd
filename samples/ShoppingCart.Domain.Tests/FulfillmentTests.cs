namespace ShoppingCart.Domain.Tests;

[Feature<CheckoutFeature>]
public class FulfillmentTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);
    private static readonly Product Pen = new("PEN-1", "Fountain pen", 300m);

    // A context carries several values through the steps instead of a tuple.
    private sealed class FulfillmentContext
    {
        public Warehouse Warehouse { get; } = new();
        public Cart Cart { get; } = new();
        public Order? Order { get; set; }
        public Reservation? Reservation { get; set; }
    }

    [Fact]
    public void Shopping_runs_through_several_steps()
    {
        // Several When/Then segments describe one shopping flow.
        new Scenario("Shop, apply a coupon, and check out")
            .Given("An empty shopping cart", () => new Cart())
            .When("A book and a pen are added", cart => { cart.Add(Book).Add(Pen); })
            .Then("The subtotal is 1500", cart => Assert.Equal(1500m, cart.Subtotal))
            .When("A 10% coupon is applied", cart => { cart.Apply(new Coupon("OFF10", 10)); })
            .Then("The total is 1350", cart => Assert.Equal(1350m, cart.Total))
            .When("The customer checks out", cart => cart.Checkout())
            .Then("The order total is 1350", order => Assert.Equal(1350m, order.Total))
            .And("The order has two lines", order => Assert.Equal(2, order.Lines.Count))
            .Run();
    }

    [Fact]
    public async Task Checkout_reserves_stock()
    {
        await new Scenario("Reserve stock for an order")
            .Given<FulfillmentContext>("Two books in stock and one in the cart", ctx =>
            {
                ctx.Warehouse.Stock(Book.Sku, 2);
                ctx.Cart.Add(Book);
            })
            .When("The customer checks out", ctx => { ctx.Order = ctx.Cart.Checkout(); })
            .Then("An order is created", ctx => Assert.NotNull(ctx.Order))
            .When("Stock is reserved", async ctx => { ctx.Reservation = await ctx.Warehouse.ReserveAsync(ctx.Order!); })
            .Then("One book remains in stock", ctx => Assert.Equal(1, ctx.Warehouse.InStock(Book.Sku)))
            .And("The reservation lists the book", ctx => Assert.Equal([Book.Sku], ctx.Reservation!.Skus))
            .RunAsync();
    }

    [Fact]
    public async Task Missing_stock_rejects_the_reservation()
    {
        await new Scenario("Reserve stock that is not available")
            .Given<FulfillmentContext>("A checked-out book that is not in stock", ctx =>
            {
                ctx.Cart.Add(Book);
                ctx.Order = ctx.Cart.Checkout();
            })
            .When("Stock is reserved", async ctx => { await ctx.Warehouse.ReserveAsync(ctx.Order!); })
            .ThenThrows<InvalidOperationException>("The reservation is rejected")
            .Contains("The message names the book", Book.Sku, t => t.Exception.Message)
            .RunAsync();
    }

    [Fact]
    public void Every_order_check_runs()
    {
        // ContinueOnFailure runs every check of the segment and reports all failures together.
        new Scenario("Check the order").ContinueOnFailure()
            .Given("The cart contains a book and a pen with a 10% coupon", () => new Cart().Add(Book).Add(Pen).Apply(new Coupon("OFF10", 10)))
            .When("The customer checks out", cart => cart.Checkout())
            .Then("The order has an ID", order => Assert.NotEqual(Guid.Empty, order.Id))
            .And("The order has two lines", order => Assert.Equal(2, order.Lines.Count))
            .And("The total is 1350", order => Assert.Equal(1350m, order.Total))
            .Run();
    }
}
