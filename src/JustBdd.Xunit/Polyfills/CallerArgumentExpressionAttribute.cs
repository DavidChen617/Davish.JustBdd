#if !NET6_0_OR_GREATER
namespace System.Runtime.CompilerServices;

// Polyfill for netstandard2.1; the compiler recognizes the fully qualified attribute name.
[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
    public string ParameterName { get; } = parameterName;
}
#endif
