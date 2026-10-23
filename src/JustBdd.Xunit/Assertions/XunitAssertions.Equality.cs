using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using XAssert = Xunit.Assert;

namespace Davish.JustBdd;

public static partial class XunitAssertions
{
    extension<T>(ThenStage<T> a)
    {
        // ---- Equal: generic values

        public ThenStage<T> Equal<TV>(string desc, TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x)));

        public ThenStage<T> Equal<TV>(TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x)));

        public ThenStage<T> Equal<TV>(string desc, TV expected, Func<T, TV> actual, IEqualityComparer<TV> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        public ThenStage<T> Equal<TV>(TV expected, Func<T, TV> actual, IEqualityComparer<TV> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        public ThenStage<T> Equal<TV>(string desc, TV expected, Func<T, TV> actual, Func<TV, TV, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        public ThenStage<T> Equal<TV>(TV expected, Func<T, TV> actual, Func<TV, TV, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        // ---- Equal: collections of different concrete types, such as int[] and List<int>

        public ThenStage<T> Equal<TItem>(string desc, IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x)));

        public ThenStage<T> Equal<TItem>(IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x)));

        public ThenStage<T> Equal<TItem>(string desc, IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, IEqualityComparer<TItem> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        public ThenStage<T> Equal<TItem>(IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, IEqualityComparer<TItem> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        public ThenStage<T> Equal<TItem>(string desc, IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, Func<TItem, TItem, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        public ThenStage<T> Equal<TItem>(IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, Func<TItem, TItem, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), comparer));

        // ---- Equal: string
        // The optional bool parameters make string and generic Equal<TV> calls ambiguous (CS0121).
        // Prefer this overload explicitly because the non-generic tie-break does not resolve that case.

        [OverloadResolutionPriority(1)]
        public ThenStage<T> Equal(string desc, string? expected, Func<T, string?> actual,
            bool ignoreCase = false, bool ignoreLineEndingDifferences = false, bool ignoreWhiteSpaceDifferences = false, bool ignoreAllWhiteSpace = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode),
                x => XAssert.Equal(expected, actual(x), ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences, ignoreAllWhiteSpace));

        [OverloadResolutionPriority(1)]
        public ThenStage<T> Equal(string? expected, Func<T, string?> actual,
            bool ignoreCase = false, bool ignoreLineEndingDifferences = false, bool ignoreWhiteSpaceDifferences = false, bool ignoreAllWhiteSpace = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode),
                x => XAssert.Equal(expected, actual(x), ignoreCase, ignoreLineEndingDifferences, ignoreWhiteSpaceDifferences, ignoreAllWhiteSpace));

        // ---- Equal: double

        public ThenStage<T> Equal(string desc, double expected, Func<T, double> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(double expected, Func<T, double> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(string desc, double expected, Func<T, double> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision, rounding));

        public ThenStage<T> Equal(double expected, Func<T, double> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision, rounding));

        public ThenStage<T> Equal(string desc, double expected, Func<T, double> actual, double tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), tolerance));

        public ThenStage<T> Equal(double expected, Func<T, double> actual, double tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), tolerance));

        // ---- Equal: float

        public ThenStage<T> Equal(string desc, float expected, Func<T, float> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(float expected, Func<T, float> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(string desc, float expected, Func<T, float> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision, rounding));

        public ThenStage<T> Equal(float expected, Func<T, float> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision, rounding));

        public ThenStage<T> Equal(string desc, float expected, Func<T, float> actual, float tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), tolerance));

        public ThenStage<T> Equal(float expected, Func<T, float> actual, float tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), tolerance));

        // ---- Equal: decimal

        public ThenStage<T> Equal(string desc, decimal expected, Func<T, decimal> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(decimal expected, Func<T, decimal> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        // ---- Equal: DateTime / DateTimeOffset; use the generic overload when no tolerance is needed

        public ThenStage<T> Equal(string desc, DateTime expected, Func<T, DateTime> actual, TimeSpan precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(DateTime expected, Func<T, DateTime> actual, TimeSpan precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(string desc, DateTimeOffset expected, Func<T, DateTimeOffset> actual, TimeSpan precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        public ThenStage<T> Equal(DateTimeOffset expected, Func<T, DateTimeOffset> actual, TimeSpan precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equal", expectedCode, actualCode), x => XAssert.Equal(expected, actual(x), precision));

        // ---- NotEqual: xUnit has no specialized string or DateTime overloads

        public ThenStage<T> NotEqual<TV>(string desc, TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x)));

        public ThenStage<T> NotEqual<TV>(TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x)));

        public ThenStage<T> NotEqual<TV>(string desc, TV expected, Func<T, TV> actual, IEqualityComparer<TV> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TV>(TV expected, Func<T, TV> actual, IEqualityComparer<TV> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TV>(string desc, TV expected, Func<T, TV> actual, Func<TV, TV, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TV>(TV expected, Func<T, TV> actual, Func<TV, TV, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TItem>(string desc, IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x)));

        public ThenStage<T> NotEqual<TItem>(IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x)));

        public ThenStage<T> NotEqual<TItem>(string desc, IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, IEqualityComparer<TItem> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TItem>(IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, IEqualityComparer<TItem> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TItem>(string desc, IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, Func<TItem, TItem, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual<TItem>(IEnumerable<TItem>? expected, Func<T, IEnumerable<TItem>?> actual, Func<TItem, TItem, bool> comparer,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), comparer));

        public ThenStage<T> NotEqual(string desc, double expected, Func<T, double> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision));

        public ThenStage<T> NotEqual(double expected, Func<T, double> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision));

        public ThenStage<T> NotEqual(string desc, double expected, Func<T, double> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision, rounding));

        public ThenStage<T> NotEqual(double expected, Func<T, double> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision, rounding));

        public ThenStage<T> NotEqual(string desc, double expected, Func<T, double> actual, double tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), tolerance));

        public ThenStage<T> NotEqual(double expected, Func<T, double> actual, double tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), tolerance));

        public ThenStage<T> NotEqual(string desc, float expected, Func<T, float> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision));

        public ThenStage<T> NotEqual(float expected, Func<T, float> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision));

        public ThenStage<T> NotEqual(string desc, float expected, Func<T, float> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision, rounding));

        public ThenStage<T> NotEqual(float expected, Func<T, float> actual, int precision, MidpointRounding rounding,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision, rounding));

        public ThenStage<T> NotEqual(string desc, float expected, Func<T, float> actual, float tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), tolerance));

        public ThenStage<T> NotEqual(float expected, Func<T, float> actual, float tolerance,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), tolerance));

        public ThenStage<T> NotEqual(string desc, decimal expected, Func<T, decimal> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision));

        public ThenStage<T> NotEqual(decimal expected, Func<T, decimal> actual, int precision,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEqual", expectedCode, actualCode), x => XAssert.NotEqual(expected, actual(x), precision));

        // ---- StrictEqual / NotStrictEqual

        public ThenStage<T> StrictEqual<TV>(string desc, TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("StrictEqual", expectedCode, actualCode), x => XAssert.StrictEqual(expected, actual(x)));

        public ThenStage<T> StrictEqual<TV>(TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("StrictEqual", expectedCode, actualCode), x => XAssert.StrictEqual(expected, actual(x)));

        public ThenStage<T> NotStrictEqual<TV>(string desc, TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotStrictEqual", expectedCode, actualCode), x => XAssert.NotStrictEqual(expected, actual(x)));

        public ThenStage<T> NotStrictEqual<TV>(TV expected, Func<T, TV> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotStrictEqual", expectedCode, actualCode), x => XAssert.NotStrictEqual(expected, actual(x)));

        // ---- Same / NotSame

        public ThenStage<T> Same(string desc, object? expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Same", expectedCode, actualCode), x => XAssert.Same(expected, actual(x)));

        public ThenStage<T> Same(object? expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Same", expectedCode, actualCode), x => XAssert.Same(expected, actual(x)));

        public ThenStage<T> NotSame(string desc, object? expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotSame", expectedCode, actualCode), x => XAssert.NotSame(expected, actual(x)));

        public ThenStage<T> NotSame(object? expected, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotSame", expectedCode, actualCode), x => XAssert.NotSame(expected, actual(x)));

        // ---- Equivalent / EquivalentWithExclusions: use arrays rather than params to allow trailing code parameters

        public ThenStage<T> Equivalent(string desc, object? expected, Func<T, object?> actual, bool strict = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Equivalent", expectedCode, actualCode), x => XAssert.Equivalent(expected, actual(x), strict));

        public ThenStage<T> Equivalent(object? expected, Func<T, object?> actual, bool strict = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Equivalent", expectedCode, actualCode), x => XAssert.Equivalent(expected, actual(x), strict));

        public ThenStage<T> EquivalentWithExclusions<TV>(string desc, object? expected, Func<T, TV> actual,
            Expression<Func<TV, object?>>[] exclusions, bool strict = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(exclusions))] string exclusionsCode = "")
            => Check(a, desc, Code("EquivalentWithExclusions", expectedCode, actualCode, exclusionsCode),
                x => XAssert.EquivalentWithExclusions(expected, actual(x), strict, exclusions));

        public ThenStage<T> EquivalentWithExclusions<TV>(object? expected, Func<T, TV> actual,
            Expression<Func<TV, object?>>[] exclusions, bool strict = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(exclusions))] string exclusionsCode = "")
            => Check(a, null, Code("EquivalentWithExclusions", expectedCode, actualCode, exclusionsCode),
                x => XAssert.EquivalentWithExclusions(expected, actual(x), strict, exclusions));

        public ThenStage<T> EquivalentWithExclusions(string desc, object? expected, Func<T, object?> actual,
            string[] exclusions, bool strict = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(exclusions))] string exclusionsCode = "")
            => Check(a, desc, Code("EquivalentWithExclusions", expectedCode, actualCode, exclusionsCode),
                x => XAssert.EquivalentWithExclusions(expected, actual(x), strict, exclusions));

        public ThenStage<T> EquivalentWithExclusions(object? expected, Func<T, object?> actual,
            string[] exclusions, bool strict = false,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(exclusions))] string exclusionsCode = "")
            => Check(a, null, Code("EquivalentWithExclusions", expectedCode, actualCode, exclusionsCode),
                x => XAssert.EquivalentWithExclusions(expected, actual(x), strict, exclusions));
    }
}
