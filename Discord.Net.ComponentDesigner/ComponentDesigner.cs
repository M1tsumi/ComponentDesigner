using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Discord;

public static class ComponentDesigner
{
    // ReSharper disable once InconsistentNaming
    public static MessageComponent cx(
        [StringSyntax("html")] DesignerInterpolationHandler designer
    ) => throw new InvalidOperationException();

    // ReSharper disable once InconsistentNaming
    public static MessageComponent cx(
        [StringSyntax("html")] string cx
    ) => throw new InvalidOperationException();
}
