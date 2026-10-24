<div align="center">

# Davish.JustBdd

**Given / When / Then tests on xUnit v3, with a single-file HTML report.**

[Overview](#overview) • [Getting started](#getting-started) • [Writing scenarios](#writing-scenarios) • [Checking results](#checking-results) • [Features and traits](#features-and-traits) • [HTML report](#html-report) • [Diagnostics](#diagnostics)

</div>

In 2006, Dan North published [Introducing BDD](https://dannorth.net/introducing-bdd/), arguing that tests should describe behavior rather than just verify code; Given / When / Then is the sentence pattern for describing behavior. On .NET, SpecFlow turned that pattern into a tool: scenarios live in Gherkin `.feature` files and bind to C# methods through regular expressions, so every scenario is maintained in two places. In early 2024 the community forked SpecFlow into Reqnroll; at the end of that year SpecFlow reached end of life, and Reqnroll carries the same approach forward.

JustBdd takes another route: Given / When / Then are plain C# lambdas, with no `.feature` files and no bindings. Step descriptions and source code are still recorded, so the report still reads like a specification.

## Overview

A test is a chain of steps that runs only when you call `Run()` or `RunAsync()` at the end. Each step's description, source code, status, and duration are recorded, so a failure shows which step broke, and the HTML report lists every scenario step by step.

```csharp
[Fact]
public void Adding_products_updates_the_total()
{
    new Scenario("Add products to an empty cart")
        .Given("An empty shopping cart", () => new Cart())
        .When("A book and two pens are added", cart => cart.Add(Book).Add(Pen, 2))
        .Then("The cart contains two line items")
            .Equal("There are two line items", 2, cart => cart.Lines.Count)
            .Equal("The subtotal is 1200 + 2 × 300", 1800m, cart => cart.Subtotal, 2)
        .Run();
}
```

- Steps are plain C# lambdas: sync, `Task`, or `ValueTask`.
- xUnit assertions chain directly after `Then`.
- Expected exceptions, events, and `PropertyChanged` have ready-made checks.
- Features carry descriptions, links, and metadata, and the report groups tests by feature.
- Results of several test projects merge into one report.
- A Roslyn analyzer warns when a scenario is never run.

### Packages

| Package | Contents |
| --- | --- |
| `Davish.JustBdd` | Core: `Scenario`, the Given / When / Then stages, the result model, features, and the JBDD001 analyzer |
| `Davish.JustBdd.XunitV3` | xUnit assertions, the HTML and CTRF reports, the Microsoft.Testing.Platform hook, and the xunit.v3 version check. Depends on `Davish.JustBdd` |
| `Davish.JustBdd.Tool` | The `justbdd` dotnet tool, which merges the CTRF files of several test projects into one HTML report |

The first two packages target `netstandard2.1`, and an xUnit test project only needs `Davish.JustBdd.XunitV3`. `Davish.JustBdd.Tool` needs the .NET 10 runtime or later.

## Getting started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 9 or later
- A test project on [xunit.v3](https://xunit.net/docs/getting-started/v3/whats-new) **4.x** (Microsoft.Testing.Platform v2)

> [!IMPORTANT]
> xunit.v3 3.2.x uses Microsoft.Testing.Platform v1 and fails the build with [`JUSTBDD0001`](#diagnostics).

### Install

```shell
dotnet add package Davish.JustBdd.XunitV3
```

To run tests through Microsoft.Testing.Platform with `dotnet test`, add this to `global.json`:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### Run the sample

`samples/ShoppingCart.Domain.Tests` uses a shopping cart to show every pattern in this README:

```shell
dotnet test --project samples/ShoppingCart.Domain.Tests --report-xunit-html
```

## Writing scenarios

### The basic shape

```csharp
new Scenario("Check out an empty cart")          // Defaults to the test method name.
    .Given("An empty shopping cart", () => new Cart())
    .When("The customer checks out", cart => cart.Checkout())
    .ThenThrows<InvalidOperationException>("Checkout is rejected")
    .Then("The cart remains open", t => Assert.False(t.State.IsCheckedOut))
    .Run();
```

- Step descriptions are optional; without one, the report shows the lambda's source code.
- End a `void` test with `Run()` and an async test with `await ….RunAsync()`. Both return a `ScenarioResult` and throw the original exception on failure.
- Every call to `Run()` or `RunAsync()` runs the whole chain again from `Given`.

> [!WARNING]
> Without `Run()` or `RunAsync()`, the chain never executes and the test still passes. The bundled analyzer reports [`JBDD001`](#diagnostics).

### How state flows

- `Given` returns the initial state.
- A `When` that returns a value replaces the state with it; a `When` without a return value leaves the state unchanged.
- `Then` and `And` only check; they never change the state.

```csharp
.When("The customer checks out", cart => cart.Checkout())      // State becomes the Order.
.When("The customer checks out", cart => { cart.Checkout(); }) // State stays the Cart.
```

### Multiple When / Then segments

A `When` can follow a `Then`, so one scenario can describe a whole flow:

```csharp
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
```

### Async steps and context objects

When a scenario needs to carry several values, use `Given<TContext>`. Each run creates a new `TContext` (it needs a parameterless constructor), and every later step receives the same instance. The `When` below also shows an async lambda:

```csharp
private sealed class FulfillmentContext
{
    public Warehouse Warehouse { get; } = new();
    public Cart Cart { get; } = new();
    public Order? Order { get; set; }
    public Reservation? Reservation { get; set; }
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
```

### Steps without state

When `Given` returns nothing, the scenario has no state and later steps take no parameters. Keep data in test fields instead:

```csharp
private readonly Cart _cart = new();
private Order? _order;

[Fact]
public void Steps_can_share_state_through_fields()
{
    new Scenario("Steps share state through test fields")
        .Given("The cart contains a book", () => { _cart.Add(Book); })
        .When("The customer checks out", () => { _order = _cart.Checkout(); })
        .Then("The cart is closed", () => Assert.True(_cart.IsCheckedOut))
        .And("The order holds the book", () => Assert.Equal(Book, _order!.Lines[0].Product))
        .Run();
}
```

### Reusing a scenario as setup

Pass an unrun scenario straight to `Given`. The inner scenario runs when that step runs, and its final state becomes the outer state; the report nests the inner steps under that step.

```csharp
new Scenario("A checked-out cart cannot be changed")
    .Given("A checked-out cart",
        new Scenario("Prepare a checked-out cart")
            .Given("The cart contains a book", () => new Cart().Add(Book))
            .When("The customer checks out", cart => { cart.Checkout(); })
            .Then("The cart is checked out", cart => Assert.True(cart.IsCheckedOut)))
    .When("A pen is added after checkout", cart => cart.Add(Pen))
    .ThenThrows<InvalidOperationException>("The change is rejected")
    .Run();
```

> [!CAUTION]
> Don't wrap the inner scenario in a lambda (`() => new Scenario(…)`). The lambda only returns the chain, so not a single inner step runs. JBDD001 catches this.

## Checking results

### Then, And, and Assert

```csharp
.Then("The cart still contains one line item", cart => Assert.Single(cart.Lines))
.And("The quantities are merged into three", cart => Assert.Equal(3, cart.Lines[0].Quantity))
```

`And` works like `Then` but is recorded as `And` in the report. `Assert(lambda)` is recorded as an `Assert` step.

### xUnit assertions

`Davish.JustBdd.XunitV3` wraps xUnit assertions as methods that chain after `Then` or `And`, each with or without a description. The report records the full call:

```csharp
.Then("Only the 20% coupon is effective")
    .Equal("The current coupon is OFF20", "OFF20", cart => cart.Coupon!.Code)
    .Equal("The total is 960", 960m, cart => cart.Total, 2)
```

<details>
<summary>Available assertions</summary>

`True`, `False`, `Null`, `NotNull`, `Equal`, `NotEqual`, `StrictEqual`, `NotStrictEqual`, `Same`, `NotSame`, `Equivalent`, `EquivalentWithExclusions`, `IsType`, `IsNotType`, `IsAssignableFrom`, `IsNotAssignableFrom`, `InRange`, `NotInRange`, `StartsWith`, `EndsWith`, `Contains`, `DoesNotContain`, `Matches`, `DoesNotMatch`, `Empty`, `NotEmpty`, `Single`, `Distinct`, `All`, `AllAsync`, `Collection`, `CollectionAsync`, `Subset`, `Superset`, `ProperSubset`, `ProperSuperset`.

Type assertions take a `Type`, for example `IsType(typeof(Admin), u => u.Account)`. Assertions don't return values; to get the cast object, use `Assert(lambda)`.

</details>

### Expected exceptions

```csharp
.When("The product with SKU NOPE is removed", cart => cart.Remove("NOPE"))
.ThenThrows<KeyNotFoundException>("The missing product is reported")
.Contains("The error message identifies the missing SKU", "NOPE", t => t.Exception.Message)
```

- The state becomes `Thrown<TState, TEx>`: `t.State` is the `When` input and `t.Exception` is the thrown exception.
- Derived types count. `ThenThrows()` equals `ThenThrows<Exception>()`.
- If the `When` doesn't throw, the step fails with `ScenarioExpectationException`.

### Events

`ThenRaises` subscribes to the `When` input while the `When` runs:

```csharp
.When("A book is added", cart => { cart.Add(Book); })
.ThenRaises<CartChangedEventArgs>("Changed is raised", (cart, h) => cart.Changed += h, (cart, h) => cart.Changed -= h)
.Then("The event names the book", raised => Assert.Equal(Book.Sku, raised.Args.Sku))
```

| Method | For |
| --- | --- |
| `ThenRaises<TArgs>(attach, detach)` | `EventHandler<TArgs>` |
| `ThenRaises(attach, detach)` | `EventHandler` |
| `ThenRaises<THandler, TArgs>(handler, attach, detach)` | Custom delegates; `handler` wraps the recorder in the event's delegate type |
| `ThenPropertyChanged(source, propertyName)` | `INotifyPropertyChanged`, matching the property name only |
| `ThenNotRaises…`, `ThenPropertyNotChanged` | The opposite check; the state stays the `When` result |

After `ThenRaises`, the state is a `Raised<TState, TArgs>` holding `State`, `Events` in raise order, and the first event's `Sender` and `Args`. The subscribed object must exist before the `When` runs.

### Reporting several failures at once

```csharp
new Scenario("Check the order").ContinueOnFailure()
    .Given("The cart contains a book and a pen with a 10% coupon", () => new Cart().Add(Book).Add(Pen).Apply(new Coupon("OFF10", 10)))
    .When("The customer checks out", cart => cart.Checkout())
    .Then("The order has an ID", order => Assert.NotEqual(Guid.Empty, order.Id))
    .And("The order has two lines", order => Assert.Equal(2, order.Lines.Count))
    .And("The total is 1350", order => Assert.Equal(1350m, order.Total))
    .Run();
```

- Consecutive `Then`, `And`, and `Assert` checks in a segment keep running after one fails. A failing `Given`, `When`, or a step like `ThenThrows` still stops the scenario.
- One failure rethrows the original exception; two or more throw `ScenarioFailuresException` (an `AggregateException`) whose message lists each failed step.
- It's off by default. Once an early check fails, later ones often fail in a cascade, which makes the report harder to read.

### Exceptions

A failing step throws its original exception, such as xUnit's `EqualException`, without wrapping it. The exception's `Data` carries two extra values:

- `Scenario.StepDataKey`: the path of the failing step, for example `Scenario 'S' > Then 'Equals 2'`.
- `Scenario.ResultDataKey`: the run's `ScenarioResult`.

## Features and traits

The report groups tests by feature by default, and each group header shows the feature's description, metadata, and links.

The simplest form is a set of attributes:

```csharp
[Feature("Coupons", Description = "A cart holds one percentage coupon; applying another replaces it.")]
[FeatureLinks("Coupon rules (example)", "https://example.com/specs/coupons")]
[FeatureMetadata("Owner", "team-pricing")]
[FeatureMetadata("Tags", "pricing", "coupon")]
public class CouponTests { }
```

To share a feature across test classes, write it as a class:

```csharp
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

[Feature<CheckoutFeature>]
public class CheckoutTests { }
```

- Only `Name` is required; `Description`, `Links`, and `Metadata` have defaults.
- Feature attributes go on a class or a method; when both have one, the method wins.
- `[FeatureLinks]` and `[FeatureMetadata]` on the same target are appended to the feature's own links and metadata, for both forms.
- A feature is identified by its name. If two declarations share a name but differ in content, the first one read wins.
- Only `http` and `https` links are shown.

xUnit traits (`[Trait("Category", "Smoke")]`) appear as tags on each test row and can also be used for grouping.

## HTML report

### One project

```shell
dotnet test --report-xunit-html
```

Davish.JustBdd registers no command-line options of its own; it writes its reports next to xUnit's. A solution can therefore pass the same options at the solution level even when some test projects don't reference Davish.JustBdd.

| Command | Davish.JustBdd writes |
| --- | --- |
| `--report-xunit-html` | `justbdd-<project>.html` |
| `--report-xunit-ctrf` | `justbdd-<project>.ctrf.json` ([CTRF](#ctrf)) |
| With `--report-xunit-html-filename x.html` | `x.justbdd.html` |
| With `--report-xunit-ctrf-filename x.json` | `x.justbdd.ctrf.json` |

- Files go to Microsoft.Testing.Platform's results folder, `TestResults/` under the current directory by default; change it with `--results-directory <dir>`.
- Each project gets its own file name, so projects don't overwrite each other. The files are listed under "In process file artifacts produced" in the `dotnet test` output.

> [!NOTE]
> `--report-justbdd-html` and `--report-justbdd-html-filename` from 0.1.0-preview.1 have been removed; use the xUnit options above.

### Several projects in one report

Each test project runs on its own and writes its own files. To get one report, have every project write CTRF, then merge the files with the `justbdd` dotnet tool:

```shell
dotnet new tool-manifest            # if there is no tool manifest (dotnet-tools.json) yet
dotnet tool install Davish.JustBdd.Tool

dotnet test --report-xunit-ctrf
dotnet justbdd html                 # writes TestResults/justbdd.html
```

- Without inputs, the tool reads `TestResults`. Inputs can be files, folders, or paths with file name wildcards, such as `dotnet justbdd html TestResults/justbdd-*.ctrf.json`; a folder contributes its `*.ctrf.json` and `*.ctrf` files, without subfolders.
- The report goes to `justbdd.html` in the first input's folder; `-o <file>` writes it elsewhere.
- With `--report-xunit-ctrf`, xUnit also writes its own CTRF (`.ctrf`). When a project has both, the Davish.JustBdd one is used; a project without Davish.JustBdd contributes xUnit's, so its tests still appear (without scenarios).
- If any file isn't valid CTRF, the tool reports the file and the reason, exits with code 1, and writes no report.

### What the report shows

The report is a single HTML file that opens offline.

- Every test is listed. When the report has tests both with and without scenarios, a "BDD only" toggle shows just the ones with scenarios.
- Filter by outcome; group by feature, project, class, duration, or trait. A report that merges two or more projects groups by "Project › Feature" by default, and a project without features lists its tests directly.
- Expand a scenario to see each step's status, duration, source code, and exception; nested scenarios sit under their step.
- Failed and skipped tests show the error message (or skip reason), stack trace, and output. Output covers what tests write to `ITestOutputHelper`; `Console` output doesn't belong to a single test and only appears in the `dotnet test` console output.
- Search test names, classes, error messages, scenario and step text, and feature names, descriptions, link titles, metadata, and traits.

When projects define a feature of the same name differently, "Project › Feature" shows each project's own description; grouping by feature alone merges them and uses the first one read.

Test outcomes come from Microsoft.Testing.Platform.

### CTRF

`justbdd-<project>.ctrf.json` from `--report-xunit-ctrf` follows [CTRF](https://github.com/ctrf-io/ctrf) 0.2.0, so other CTRF tools can read it. Davish.JustBdd data such as scenarios and features lives under `extra["davish.justbdd"]`; CTRF's `steps` also list the flattened step names and statuses.

### Collecting results yourself

To collect results yourself, implement `IScenarioResultSink` and register it with `ScenarioResults.AddSink`; dispose the returned object to unregister. Each top-level scenario is reported once, passed or failed; nested scenarios appear in their step's `Children`.

## Diagnostics

| ID | Kind | Reported when |
| --- | --- | --- |
| `JBDD001` | Analyzer warning | A scenario is never run: the chain doesn't end with `Run()` or `RunAsync()`, the `RunAsync()` result is discarded, or a `Given` / `When` lambda returns a scenario |
| `JUSTBDD0001` | Build error | The project uses xunit.v3's mtp-v1 packages (xunit.v3 3.2.x); upgrade to 4.0.0 or later |

To have JBDD001 block the build, add `dotnet_diagnostic.JBDD001.severity = error` to `.editorconfig`.

## C# language version

The same lambda can match both the `Task` and `ValueTask` overloads of a step, so Davish.JustBdd uses `OverloadResolutionPriorityAttribute` to make the compiler pick `Task`. The attribute takes effect only in C# 13 and later; earlier versions ignore it ([language proposal](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/overload-resolution-priority.md)).

The default language version follows the target framework. A test project that targets `net8.0` compiles as C# 12 even with a newer SDK, and async lambdas and throwing lambdas in `Given`, `When`, and `Then` then fail with CS0121 (ambiguous call).

Pick one fix:

- Set the language version to 13 or later (requires the .NET 9 SDK or later):

  ```xml
  <PropertyGroup>
    <LangVersion>13</LangVersion>
  </PropertyGroup>
  ```

- State the lambda's return type:

  ```csharp
  await new Scenario()
      .Given(async Task<User> () => await LoadUserAsync())
      .When(async ValueTask (user) => await user.RenameAsync("Davish"))
      .Then(void (_) => throw new InvalidOperationException())
      .RunAsync();
  ```

  Passing a method group or a typed delegate variable, such as `.Then(CheckAsync)`, also works.

Projects that target `net9.0` or later default to C# 13 or later and need neither fix.

## Third-party notices

The HTML report template is adapted from Microsoft.Testing.Extensions.HtmlReport 2.4.0 (MIT) and ships in `Davish.JustBdd.XunitV3` and `Davish.JustBdd.Tool`. See [THIRD-PARTY-NOTICES.md](src/JustBdd.Xunit/THIRD-PARTY-NOTICES.md).
