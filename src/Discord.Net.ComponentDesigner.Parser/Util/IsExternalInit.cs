
#if NETSTANDARD

namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit : Attribute;
    internal sealed class CompilerFeatureRequiredAttribute(string s) : Attribute;
    internal sealed class RequiredMemberAttribute : Attribute;
}

namespace System.Diagnostics.CodeAnalysis
{
    internal sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }
    
    internal sealed class NotNullIfNotNullAttribute(string parameterName) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }

}
#endif
