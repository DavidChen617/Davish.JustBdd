using System.Runtime.CompilerServices;
using XAssert = Xunit.Assert;

namespace Davish.JustBdd;

public static partial class XunitAssertions
{
    extension<T>(ThenStage<T> a)
    {
        // ---- True / False: also pass desc as the xUnit userMessage

        public ThenStage<T> True(string desc, Func<T, bool?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("True", actualCode), x => XAssert.True(actual(x), desc));

        public ThenStage<T> True(Func<T, bool?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("True", actualCode), x => XAssert.True(actual(x)));

        public ThenStage<T> False(string desc, Func<T, bool?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("False", actualCode), x => XAssert.False(actual(x), desc));

        public ThenStage<T> False(Func<T, bool?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("False", actualCode), x => XAssert.False(actual(x)));

        // ---- Null / NotNull: an empty Nullable<T> boxes to null, so object? covers both value and reference types

        public ThenStage<T> Null(string desc, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Null", actualCode), x => XAssert.Null(actual(x)));

        public ThenStage<T> Null(Func<T, object?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Null", actualCode), x => XAssert.Null(actual(x)));

        public ThenStage<T> NotNull(string desc, Func<T, object?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotNull", actualCode), x => XAssert.NotNull(actual(x)));

        public ThenStage<T> NotNull(Func<T, object?> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotNull", actualCode), x => XAssert.NotNull(actual(x)));
    }
}
