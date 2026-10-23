#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

// Polyfill for init accessors used by records on netstandard2.1.
internal static class IsExternalInit { }
#endif
