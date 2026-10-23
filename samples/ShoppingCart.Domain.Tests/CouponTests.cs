namespace ShoppingCart.Domain.Tests;

// A feature declared inline, without a feature class.
[Feature("Coupons", Description = "A cart holds one percentage coupon; applying another replaces it.")]
[FeatureLinks("Coupon rules (example)", "https://example.com/specs/coupons")]
[FeatureMetadata("Owner", "team-pricing")]
[FeatureMetadata("Tags", "pricing", "coupon")]
public class CouponTests
{
    private static readonly Product Book = new("BOOK-1", "Domain-Driven Design", 1200m);

    [Theory]
    [InlineData(10, 1080)]
    [InlineData(25, 900)]
    [InlineData(100, 0)]
    public void Coupon_discounts_the_subtotal(int percent, int expectedTotal)
    {
        new Scenario($"Apply a {percent}% coupon")
            .Given("The cart contains a book priced at 1200", () => new Cart().Add(Book))
            .When($"A {percent}% coupon is applied", cart => cart.Apply(new Coupon($"OFF{percent}", percent)))
            .Then("The discount is deducted from the total")
                .Equal("The subtotal remains unchanged", 1200m, cart => cart.Subtotal, 2)
                .Equal($"The total is {expectedTotal}", (decimal)expectedTotal, cart => cart.Total, 2)
            .Run();
    }

    [Fact]
    public void Applying_a_second_coupon_replaces_the_first()
    {
        new Scenario("Replace a coupon")
            .Given("The cart has a 10% coupon applied", () => new Cart().Add(Book).Apply(new Coupon("OFF10", 10)))
            .When("A 20% coupon is applied instead", cart => cart.Apply(new Coupon("OFF20", 20)))
            .Then("Only the 20% coupon is effective")
                .Equal("The current coupon is OFF20", "OFF20", cart => cart.Coupon!.Code)
                .Equal("The total is 960", 960m, cart => cart.Total, 2)
            .Run();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Percent_must_be_within_range(int percent)
    {
        new Scenario($"Create a {percent}% coupon")
            .When("The coupon is created", () => new Coupon("BAD", percent))
            .ThenThrows<ArgumentOutOfRangeException>("The out-of-range percentage is rejected")
            .Run();
    }
}
