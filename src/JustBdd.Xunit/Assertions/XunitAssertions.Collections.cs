using System.Collections;
using System.Runtime.CompilerServices;
using XAssert = Xunit.Assert;

namespace Davish.JustBdd;

public static partial class XunitAssertions
{
    extension<T>(ThenStage<T> a)
    {
        // ---- Contains / DoesNotContain: elements and predicates; IEnumerable<TItem> also covers sets

        public ThenStage<T> Contains<TItem>(string desc, TItem expected, Func<T, IEnumerable<TItem>> actual, IEqualityComparer<TItem>? comparer = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Contains", expectedCode, actualCode), x => ContainsItemCore(expected, actual(x), comparer));

        public ThenStage<T> Contains<TItem>(TItem expected, Func<T, IEnumerable<TItem>> actual, IEqualityComparer<TItem>? comparer = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Contains", expectedCode, actualCode), x => ContainsItemCore(expected, actual(x), comparer));

        public ThenStage<T> Contains<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Predicate<TItem> filter,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(filter))] string filterCode = "")
            => Check(a, desc, Code("Contains", actualCode, filterCode), x => XAssert.Contains(actual(x), filter));

        public ThenStage<T> Contains<TItem>(Func<T, IEnumerable<TItem>> actual, Predicate<TItem> filter,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(filter))] string filterCode = "")
            => Check(a, null, Code("Contains", actualCode, filterCode), x => XAssert.Contains(actual(x), filter));

        public ThenStage<T> DoesNotContain<TItem>(string desc, TItem expected, Func<T, IEnumerable<TItem>> actual, IEqualityComparer<TItem>? comparer = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("DoesNotContain", expectedCode, actualCode), x => DoesNotContainItemCore(expected, actual(x), comparer));

        public ThenStage<T> DoesNotContain<TItem>(TItem expected, Func<T, IEnumerable<TItem>> actual, IEqualityComparer<TItem>? comparer = null,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("DoesNotContain", expectedCode, actualCode), x => DoesNotContainItemCore(expected, actual(x), comparer));

        public ThenStage<T> DoesNotContain<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Predicate<TItem> filter,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(filter))] string filterCode = "")
            => Check(a, desc, Code("DoesNotContain", actualCode, filterCode), x => XAssert.DoesNotContain(actual(x), filter));

        public ThenStage<T> DoesNotContain<TItem>(Func<T, IEnumerable<TItem>> actual, Predicate<TItem> filter,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(filter))] string filterCode = "")
            => Check(a, null, Code("DoesNotContain", actualCode, filterCode), x => XAssert.DoesNotContain(actual(x), filter));

        // ---- Dictionary keys: collection overload inference conflicts on key versus KeyValuePair, avoiding ambiguity

        public ThenStage<T> Contains<TKey, TValue>(string desc, TKey expected, Func<T, IReadOnlyDictionary<TKey, TValue>> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            where TKey : notnull
            => Check(a, desc, Code("Contains", expectedCode, actualCode), x => XAssert.Contains(expected, actual(x)));

        public ThenStage<T> Contains<TKey, TValue>(TKey expected, Func<T, IReadOnlyDictionary<TKey, TValue>> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            where TKey : notnull
            => Check(a, null, Code("Contains", expectedCode, actualCode), x => XAssert.Contains(expected, actual(x)));

        public ThenStage<T> DoesNotContain<TKey, TValue>(string desc, TKey expected, Func<T, IReadOnlyDictionary<TKey, TValue>> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            where TKey : notnull
            => Check(a, desc, Code("DoesNotContain", expectedCode, actualCode), x => XAssert.DoesNotContain(expected, actual(x)));

        public ThenStage<T> DoesNotContain<TKey, TValue>(TKey expected, Func<T, IReadOnlyDictionary<TKey, TValue>> actual,
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            where TKey : notnull
            => Check(a, null, Code("DoesNotContain", expectedCode, actualCode), x => XAssert.DoesNotContain(expected, actual(x)));

        // ---- Empty / NotEmpty

        public ThenStage<T> Empty(string desc, Func<T, IEnumerable> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Empty", actualCode), x => XAssert.Empty(actual(x)));

        public ThenStage<T> Empty(Func<T, IEnumerable> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Empty", actualCode), x => XAssert.Empty(actual(x)));

        public ThenStage<T> NotEmpty(string desc, Func<T, IEnumerable> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("NotEmpty", actualCode), x => XAssert.NotEmpty(actual(x)));

        public ThenStage<T> NotEmpty(Func<T, IEnumerable> actual,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("NotEmpty", actualCode), x => XAssert.NotEmpty(actual(x)));

        // ---- Single: validate without returning the element

        public ThenStage<T> Single<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Predicate<TItem>? predicate = null,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(predicate))] string predicateCode = "")
            => Check(a, desc, Code("Single", Args(actualCode, predicateCode)), x => SingleCore(actual(x), predicate));

        public ThenStage<T> Single<TItem>(Func<T, IEnumerable<TItem>> actual, Predicate<TItem>? predicate = null,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(predicate))] string predicateCode = "")
            => Check(a, null, Code("Single", Args(actualCode, predicateCode)), x => SingleCore(actual(x), predicate));

        public ThenStage<T> Single(string desc, Func<T, IEnumerable> actual, object? expected,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "")
            => Check(a, desc, Code("Single", actualCode, expectedCode), x => XAssert.Single(actual(x), expected));

        public ThenStage<T> Single(Func<T, IEnumerable> actual, object? expected,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(expected))] string expectedCode = "")
            => Check(a, null, Code("Single", actualCode, expectedCode), x => XAssert.Single(actual(x), expected));

        // ---- Distinct

        public ThenStage<T> Distinct<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, IEqualityComparer<TItem>? comparer = null,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Distinct", actualCode), x => XAssert.Distinct(actual(x), comparer ?? EqualityComparer<TItem>.Default));

        public ThenStage<T> Distinct<TItem>(Func<T, IEnumerable<TItem>> actual, IEqualityComparer<TItem>? comparer = null,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Distinct", actualCode), x => XAssert.Distinct(actual(x), comparer ?? EqualityComparer<TItem>.Default));

        // ---- All / AllAsync: retain the original wrapper surface without the optional throwIfEmpty parameter

        public ThenStage<T> All<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Action<TItem> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => Check(a, desc, Code("All", actualCode, actionCode), x => AllCore(actual(x), (item, _) => action(item)));

        public ThenStage<T> All<TItem>(Func<T, IEnumerable<TItem>> actual, Action<TItem> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => Check(a, null, Code("All", actualCode, actionCode), x => AllCore(actual(x), (item, _) => action(item)));

        public ThenStage<T> All<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Action<TItem, int> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => Check(a, desc, Code("All", actualCode, actionCode), x => AllCore(actual(x), action));

        public ThenStage<T> All<TItem>(Func<T, IEnumerable<TItem>> actual, Action<TItem, int> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => Check(a, null, Code("All", actualCode, actionCode), x => AllCore(actual(x), action));

        public ThenStage<T> AllAsync<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Func<TItem, Task> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => CheckAsync(a, desc, Code("AllAsync", actualCode, actionCode), x => AllAsyncCore(actual(x), (item, _) => action(item)));

        public ThenStage<T> AllAsync<TItem>(Func<T, IEnumerable<TItem>> actual, Func<TItem, Task> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => CheckAsync(a, null, Code("AllAsync", actualCode, actionCode), x => AllAsyncCore(actual(x), (item, _) => action(item)));

        public ThenStage<T> AllAsync<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Func<TItem, int, Task> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => CheckAsync(a, desc, Code("AllAsync", actualCode, actionCode), x => AllAsyncCore(actual(x), action));

        public ThenStage<T> AllAsync<TItem>(Func<T, IEnumerable<TItem>> actual, Func<TItem, int, Task> action,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(action))] string actionCode = "")
            => CheckAsync(a, null, Code("AllAsync", actualCode, actionCode), x => AllAsyncCore(actual(x), action));

        // ---- Collection / CollectionAsync: arrays allow trailing code parameters; call with [a => ..., b => ...]

        public ThenStage<T> Collection<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Action<TItem>[] inspectors,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(inspectors))] string inspectorsCode = "")
            => Check(a, desc, Code("Collection", actualCode, inspectorsCode), x => XAssert.Collection(actual(x), inspectors));

        public ThenStage<T> Collection<TItem>(Func<T, IEnumerable<TItem>> actual, Action<TItem>[] inspectors,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(inspectors))] string inspectorsCode = "")
            => Check(a, null, Code("Collection", actualCode, inspectorsCode), x => XAssert.Collection(actual(x), inspectors));

        public ThenStage<T> CollectionAsync<TItem>(string desc, Func<T, IEnumerable<TItem>> actual, Func<TItem, Task>[] inspectors,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(inspectors))] string inspectorsCode = "")
            => CheckAsync(a, desc, Code("CollectionAsync", actualCode, inspectorsCode), x => XAssert.CollectionAsync(actual(x), inspectors));

        public ThenStage<T> CollectionAsync<TItem>(Func<T, IEnumerable<TItem>> actual, Func<TItem, Task>[] inspectors,
            [CallerArgumentExpression(nameof(actual))] string actualCode = "",
            [CallerArgumentExpression(nameof(inspectors))] string inspectorsCode = "")
            => CheckAsync(a, null, Code("CollectionAsync", actualCode, inspectorsCode), x => XAssert.CollectionAsync(actual(x), inspectors));

        // ---- Set relationships

        public ThenStage<T> Subset<TItem>(string desc, ISet<TItem> expectedSubset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSubset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Subset", expectedCode, actualCode), x => XAssert.Subset(expectedSubset, actual(x)));

        public ThenStage<T> Subset<TItem>(ISet<TItem> expectedSubset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSubset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Subset", expectedCode, actualCode), x => XAssert.Subset(expectedSubset, actual(x)));

        public ThenStage<T> Superset<TItem>(string desc, ISet<TItem> expectedSuperset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSuperset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("Superset", expectedCode, actualCode), x => XAssert.Superset(expectedSuperset, actual(x)));

        public ThenStage<T> Superset<TItem>(ISet<TItem> expectedSuperset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSuperset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("Superset", expectedCode, actualCode), x => XAssert.Superset(expectedSuperset, actual(x)));

        public ThenStage<T> ProperSubset<TItem>(string desc, ISet<TItem> expectedSubset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSubset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("ProperSubset", expectedCode, actualCode), x => XAssert.ProperSubset(expectedSubset, actual(x)));

        public ThenStage<T> ProperSubset<TItem>(ISet<TItem> expectedSubset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSubset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("ProperSubset", expectedCode, actualCode), x => XAssert.ProperSubset(expectedSubset, actual(x)));

        public ThenStage<T> ProperSuperset<TItem>(string desc, ISet<TItem> expectedSuperset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSuperset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, desc, Code("ProperSuperset", expectedCode, actualCode), x => XAssert.ProperSuperset(expectedSuperset, actual(x)));

        public ThenStage<T> ProperSuperset<TItem>(ISet<TItem> expectedSuperset, Func<T, ISet<TItem>?> actual,
            [CallerArgumentExpression(nameof(expectedSuperset))] string expectedCode = "",
            [CallerArgumentExpression(nameof(actual))] string actualCode = "")
            => Check(a, null, Code("ProperSuperset", expectedCode, actualCode), x => XAssert.ProperSuperset(expectedSuperset, actual(x)));
    }

    // ---- Internals

    // Omitted optional arguments produce empty CallerArgumentExpression strings; exclude them from code.
    private static string[] Args(params string[] codes) => codes.Where(c => c.Length > 0).ToArray();

    private static void ContainsItemCore<TItem>(TItem expected, IEnumerable<TItem> collection, IEqualityComparer<TItem>? comparer)
    {
        if (comparer is null) XAssert.Contains(expected, collection);
        else XAssert.Contains(expected, collection, comparer);
    }

    private static void DoesNotContainItemCore<TItem>(TItem expected, IEnumerable<TItem> collection, IEqualityComparer<TItem>? comparer)
    {
        if (comparer is null) XAssert.DoesNotContain(expected, collection);
        else XAssert.DoesNotContain(expected, collection, comparer);
    }

    private static void SingleCore<TItem>(IEnumerable<TItem> collection, Predicate<TItem>? predicate)
    {
        if (predicate is null) XAssert.Single(collection);
        else XAssert.Single(collection, predicate);
    }

    private static void AllCore<TItem>(IEnumerable<TItem> collection, Action<TItem, int> action)
        => XAssert.All(collection, action);

    private static Task AllAsyncCore<TItem>(IEnumerable<TItem> collection, Func<TItem, int, Task> action)
        => XAssert.AllAsync(collection, action);
}
