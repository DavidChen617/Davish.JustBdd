using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using XAssert = Xunit.Assert;

namespace Davish.JustBdd;

public static partial class XunitAssertions
{
    extension<T>(ThenStage<T> a)
    {
        // ---- Types: provide the Type-parameter overloads.
        // Generic IsType<TX> cannot specify only TX while inferring the extension receiver type T.

        public ThenStage<T> IsType(string desc, Type expected, Func<T, object?> actual, bool exactMatch = true,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("IsType", expectedCode, actualCode), x => XAssert.IsType(expected, actual(x), exactMatch));

        public ThenStage<T> IsType(Type expected, Func<T, object?> actual, bool exactMatch = true,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("IsType", expectedCode, actualCode), x => XAssert.IsType(expected, actual(x), exactMatch));

        public ThenStage<T> IsNotType(string desc, Type expected, Func<T, object?> actual, bool exactMatch = true,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("IsNotType", expectedCode, actualCode), x => XAssert.IsNotType(expected, actual(x), exactMatch));

        public ThenStage<T> IsNotType(Type expected, Func<T, object?> actual, bool exactMatch = true,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("IsNotType", expectedCode, actualCode), x => XAssert.IsNotType(expected, actual(x), exactMatch));

        public ThenStage<T> IsAssignableFrom(string desc, Type expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("IsAssignableFrom", expectedCode, actualCode), x => XAssert.IsAssignableFrom(expected, actual(x)));

        public ThenStage<T> IsAssignableFrom(Type expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("IsAssignableFrom", expectedCode, actualCode), x => XAssert.IsAssignableFrom(expected, actual(x)));

        public ThenStage<T> IsNotAssignableFrom(string desc, Type expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("IsNotAssignableFrom", expectedCode, actualCode), x => XAssert.IsNotAssignableFrom(expected, actual(x)));

        public ThenStage<T> IsNotAssignableFrom(Type expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("IsNotAssignableFrom", expectedCode, actualCode), x => XAssert.IsNotAssignableFrom(expected, actual(x)));

        // ---- Ranges: omitted comparers use Comparer<TV>.Default; no IComparable constraint is needed

        public ThenStage<T> InRange<TV>(string desc, TV low, TV high, Func<T, TV> actual, IComparer<TV>? comparer = null,
            [CallerArgumentExpression(nameof(low))] string lowCode = "",
            [CallerArgumentExpression(nameof(high))] string highCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("InRange", lowCode, highCode, actualCode), x => XAssert.InRange(actual(x), low, high, comparer ?? Comparer<TV>.Default));

        public ThenStage<T> InRange<TV>(TV low, TV high, Func<T, TV> actual, IComparer<TV>? comparer = null,
            [CallerArgumentExpression(nameof(low))] string lowCode = "",
            [CallerArgumentExpression(nameof(high))] string highCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("InRange", lowCode, highCode, actualCode), x => XAssert.InRange(actual(x), low, high, comparer ?? Comparer<TV>.Default));

        public ThenStage<T> NotInRange<TV>(string desc, TV low, TV high, Func<T, TV> actual, IComparer<TV>? comparer = null,
            [CallerArgumentExpression(nameof(low))] string lowCode = "",
            [CallerArgumentExpression(nameof(high))] string highCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotInRange", lowCode, highCode, actualCode), x => XAssert.NotInRange(actual(x), low, high, comparer ?? Comparer<TV>.Default));

        public ThenStage<T> NotInRange<TV>(TV low, TV high, Func<T, TV> actual, IComparer<TV>? comparer = null,
            [CallerArgumentExpression(nameof(low))] string lowCode = "",
            [CallerArgumentExpression(nameof(high))] string highCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotInRange", lowCode, highCode, actualCode), x => XAssert.NotInRange(actual(x), low, high, comparer ?? Comparer<TV>.Default));

        // ---- Strings: preserve xUnit defaults by calling its overload without StringComparison when omitted

        public ThenStage<T> StartsWith(string desc, string? expected, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("StartsWith", expectedCode, actualCode), x => StartsWithCore(expected, actual(x), comparison));

        public ThenStage<T> StartsWith(string? expected, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("StartsWith", expectedCode, actualCode), x => StartsWithCore(expected, actual(x), comparison));

        public ThenStage<T> EndsWith(string desc, string? expected, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("EndsWith", expectedCode, actualCode), x => EndsWithCore(expected, actual(x), comparison));

        public ThenStage<T> EndsWith(string? expected, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("EndsWith", expectedCode, actualCode), x => EndsWithCore(expected, actual(x), comparison));

        public ThenStage<T> Contains(string desc, string expectedSubstring, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expectedSubstring))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Contains", expectedCode, actualCode), x => ContainsCore(expectedSubstring, actual(x), comparison));

        public ThenStage<T> Contains(string expectedSubstring, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expectedSubstring))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Contains", expectedCode, actualCode), x => ContainsCore(expectedSubstring, actual(x), comparison));

        public ThenStage<T> DoesNotContain(string desc, string expectedSubstring, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expectedSubstring))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("DoesNotContain", expectedCode, actualCode), x => DoesNotContainCore(expectedSubstring, actual(x), comparison));

        public ThenStage<T> DoesNotContain(string expectedSubstring, Func<T, string?> actual, StringComparison? comparison = null,
            [CallerArgumentExpression(nameof(expectedSubstring))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("DoesNotContain", expectedCode, actualCode), x => DoesNotContainCore(expectedSubstring, actual(x), comparison));

        public ThenStage<T> Matches(string desc, string expectedPattern, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedPattern))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Matches", expectedCode, actualCode), x => XAssert.Matches(expectedPattern, actual(x)));

        public ThenStage<T> Matches(string expectedPattern, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedPattern))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Matches", expectedCode, actualCode), x => XAssert.Matches(expectedPattern, actual(x)));

        public ThenStage<T> Matches(string desc, Regex expectedRegex, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedRegex))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Matches", expectedCode, actualCode), x => XAssert.Matches(expectedRegex, actual(x)));

        public ThenStage<T> Matches(Regex expectedRegex, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedRegex))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Matches", expectedCode, actualCode), x => XAssert.Matches(expectedRegex, actual(x)));

        public ThenStage<T> DoesNotMatch(string desc, string expectedPattern, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedPattern))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("DoesNotMatch", expectedCode, actualCode), x => XAssert.DoesNotMatch(expectedPattern, actual(x)));

        public ThenStage<T> DoesNotMatch(string expectedPattern, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedPattern))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("DoesNotMatch", expectedCode, actualCode), x => XAssert.DoesNotMatch(expectedPattern, actual(x)));

        public ThenStage<T> DoesNotMatch(string desc, Regex expectedRegex, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedRegex))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("DoesNotMatch", expectedCode, actualCode), x => XAssert.DoesNotMatch(expectedRegex, actual(x)));

        public ThenStage<T> DoesNotMatch(Regex expectedRegex, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(expectedRegex))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("DoesNotMatch", expectedCode, actualCode), x => XAssert.DoesNotMatch(expectedRegex, actual(x)));

        // The string Empty overload is more specific than IEnumerable when a lambda returns string.
        public ThenStage<T> Empty(string desc, Func<T, string?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Empty", actualCode), x => XAssert.Empty(actual(x)!));

        public ThenStage<T> Empty(Func<T, string?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Empty", actualCode), x => XAssert.Empty(actual(x)!));
    }

    private static void StartsWithCore(string? expected, string? actual, StringComparison? comparison)
    {
        if (comparison is { } c) XAssert.StartsWith(expected, actual, c);
        else XAssert.StartsWith(expected, actual);
    }

    private static void EndsWithCore(string? expected, string? actual, StringComparison? comparison)
    {
        if (comparison is { } c) XAssert.EndsWith(expected, actual, c);
        else XAssert.EndsWith(expected, actual);
    }

    private static void ContainsCore(string expected, string? actual, StringComparison? comparison)
    {
        if (comparison is { } c) XAssert.Contains(expected, actual, c);
        else XAssert.Contains(expected, actual);
    }

    private static void DoesNotContainCore(string expected, string? actual, StringComparison? comparison)
    {
        if (comparison is { } c) XAssert.DoesNotContain(expected, actual, c);
        else XAssert.DoesNotContain(expected, actual);
    }
}
