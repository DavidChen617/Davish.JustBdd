namespace ShoppingCart.Domain.Tests;

[Feature<CouponFeature>]
public class CouponTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);

    [Theory]
    [InlineData(10, 1080)]
    [InlineData(25, 900)]
    [InlineData(100, 0)]
    public async Task Coupon_discounts_the_subtotal(int percent, int expectedTotal)
    {
        await new Scenario($"Apply a {percent}% coupon")
            .Given("The cart contains a book priced at 1200", () => new Cart().Add(Book))
            .When($"A {percent}% coupon is applied", cart => cart.Apply(new Coupon($"OFF{percent}", percent)))
            .Then("The discount is deducted from the total")
                .Equal("The subtotal remains unchanged", 1200m, cart => cart.Subtotal, 2)
                .Equal($"The total is {expectedTotal}", (decimal)expectedTotal, cart => cart.Total, 2);
    }

    [Fact]
    public async Task Applying_a_second_coupon_replaces_the_first()
    {
        await new Scenario("Replace a coupon")
            .Given("The cart has a 10% coupon applied", () => new Cart().Add(Book).Apply(new Coupon("OFF10", 10)))
            .When("A 20% coupon is applied instead", cart => cart.Apply(new Coupon("OFF20", 20)))
            .Then("Only the 20% coupon is effective")
                .Equal("The current coupon is OFF20", "OFF20", cart => cart.Coupon!.Code)
                .Equal("The total is 960", 960m, cart => cart.Total, 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Percent_must_be_within_range(int percent)
    {
        await new Scenario($"Create a {percent}% coupon")
            .When("The coupon is created", () => new Coupon("BAD", percent))
            .ThenThrows<ArgumentOutOfRangeException>("The out-of-range percentage is rejected");
    }
}
