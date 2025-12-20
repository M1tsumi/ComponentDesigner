namespace System.Diagnostics.CodeAnalysis;

#if NETSTANDARD
internal sealed class MemberNotNullAttribute(params string[] members) : Attribute
{
    public string[] Members { get; } = members;

    public MemberNotNullAttribute(string member) : this([member])
    {
    }
}

internal sealed class MemberNotNullWhenAttribute(bool returnValue, params string[] members) : Attribute
{
    public string[] Members { get; } = members;
    public bool ReturnValue { get; } = returnValue;

    public MemberNotNullWhenAttribute(bool returnValue, string member) : this(returnValue, [member])
    {
    }
}

internal sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
{
    public bool ReturnValue { get; } = returnValue;
}

internal sealed class NotNullIfNotNullAttribute(string parameterName) : Attribute
{
    public string ParameterName { get; } = parameterName;
}
#endif