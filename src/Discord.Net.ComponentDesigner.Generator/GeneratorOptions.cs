using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Discord.CX;

public sealed record GeneratorOptions(
    bool EnableAutoRows = false,
    LanguageVersion? CSharpLangVersion = null
)
{
    public static readonly GeneratorOptions Default = new();
    
    private const string ENABLE_AUTO_ROWS_KEY = "EnableAutoRows";

    public static IncrementalValueProvider<GeneratorOptions> CreateProvider(
        IncrementalGeneratorInitializationContext context
    )
    {
        return context.CompilationProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(CreateOptions);

        static GeneratorOptions CreateOptions(
            (Compilation Compilation, AnalyzerConfigOptionsProvider Options) tuple,
            CancellationToken token
        )
        {
            var compilation = tuple.Compilation;
            var analyzerConfig = tuple.Options;

            LanguageVersion? langVersion = compilation is CSharpCompilation csharp
                ? csharp.LanguageVersion
                : null;

            var autoRows
                = analyzerConfig
                      .GlobalOptions
                      .TryGetValue(ENABLE_AUTO_ROWS_KEY, out var val) &&
                  bool.TryParse(val.ToLowerInvariant(), out var bl) && bl;

            return new GeneratorOptions(
                EnableAutoRows: autoRows,
                CSharpLangVersion: langVersion
            );
        }
    }
}