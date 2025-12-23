using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Discord;

public static class ComponentDesigner
{
    // ReSharper disable once InconsistentNaming
    public static CXMessageComponent cx(
        [StringSyntax("html")] DesignerInterpolationHandler designer
    ) => throw new UnreachableException("Make sure interceptors are enabled for the component designer, if they are, this is a bug");

    // ReSharper disable once InconsistentNaming
    public static CXMessageComponent cx(
        [StringSyntax("html")] string cx
    ) => throw new UnreachableException("Make sure interceptors are enabled for the component designer, if they are, this is a bug");
}
