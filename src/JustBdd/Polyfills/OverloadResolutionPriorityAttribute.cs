#if !NET9_0_OR_GREATER
namespace System.Runtime.CompilerServices;

// Polyfill for netstandard2.1; the compiler recognizes the fully qualified attribute name.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}
#endif
