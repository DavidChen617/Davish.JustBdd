# Davish.JustBdd

## C# language version

Davish.JustBdd uses `OverloadResolutionPriorityAttribute` to pick the `Task` overload when a lambda matches both the `Task` and `ValueTask` overloads of a step. The compiler honors this attribute only in C# 13 and later; earlier language versions ignore it ([language proposal](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-13.0/overload-resolution-priority.md)).

The default language version follows the target framework, so a test project that targets `net8.0` compiles as C# 12 even with a newer SDK. In that case async lambdas and throwing lambdas in `Given`, `When`, and `Then` fail with CS0121 (ambiguous call).

Use one of these options:

- Set the language version to 13 or later (requires the .NET 9 SDK or later):

  ```xml
  <PropertyGroup>
    <LangVersion>13</LangVersion>
  </PropertyGroup>
  ```

- Or state the lambda's return type explicitly:

  ```csharp
  await new Scenario()
      .Given(async Task<User> () => await LoadUserAsync())
      .When(async ValueTask (user) => await user.RenameAsync("Davish"))
      .Then(void (_) => throw new InvalidOperationException());
  ```

  Method groups and typed delegate variables, such as `.Then(CheckAsync)`, also work.

Projects that target `net9.0` or later use C# 13 or later by default and need neither option.
