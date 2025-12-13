using System.Collections.Generic;

namespace Discord.CX.Nodes;

public delegate void PropertyValidator(
    IComponentContext context,
    ComponentPropertyValue value,
    IList<DiagnosticInfo> diagnostics
);