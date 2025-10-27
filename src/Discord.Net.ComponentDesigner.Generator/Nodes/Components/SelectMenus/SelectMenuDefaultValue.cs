using System.Collections.Generic;
using System.Linq;
using Discord.CX.Parser;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Discord.CX.Nodes.Components.SelectMenus;

public abstract class SelectMenuDefaultValue
{
    public void Validate(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
    {
        this.ValidateInternal(context, selectState);    
    }
    
    protected abstract void ValidateInternal(ComponentContext context, SelectMenuComponentNode.SelectState selectState);
    public abstract string Render(ComponentContext context, SelectMenuComponentNode.SelectState selectState);

    protected static string FromIdAndKind(ComponentContext context, string id, SelectMenuDefaultValueKind kind)
    {
        var dotnetType = context.KnownTypes
            .SelectMenuDefaultValueType!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var kindType = context.KnownTypes
            .SelectDefaultValueTypeEnumType!
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return $"new {dotnetType}(id: {id}, type: {kindType}.{kind})";
    }
    
    public static SelectMenuDefaultValue Create(ICXNode node)
    {
        switch (node)
        {
            case CXElement element: return new Element(element);
            case CXValue.Interpolation interpolation: return new Interpolation(interpolation);
            default: return new InvalidNode(node);
        }
    }

    private sealed class InvalidNode(ICXNode node) : SelectMenuDefaultValue
    {
        protected override void ValidateInternal(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
        {
            context.AddDiagnostic(
                Diagnostics.InvalidSelectMenuDefaultKind,
                node,
                node.GetType().Name
            );
        }

        public override string Render(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
            => string.Empty;
    }

    public sealed class Interpolation(CXValue.Interpolation interpolation) : SelectMenuDefaultValue
    {
        private string? _source;
        private SelectMenuDefaultValueKind? _kind;

        private bool TryGetSourceAndKind(
            ComponentContext context, 
            out string source,
            out SelectMenuDefaultValueKind kind)
        {
            var info = context.GetInterpolationInfo(interpolation);

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IUserType
                )
            )
            {
                source = FromIdAndKind(
                    context,
                    $"{
                        context.GetDesignerValue(
                            info,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    }.Id",
                    kind = SelectMenuDefaultValueKind.User
                );
                return true;
            }
            
            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IChannelType
                )
            )
            {
                source = FromIdAndKind(
                    context,
                    $"{
                        context.GetDesignerValue(
                            info,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    }.Id",
                    kind = SelectMenuDefaultValueKind.Channel
                );
                return true;
            }
            
            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.IRoleType
                )
            )
            {
                source = FromIdAndKind(
                    context,
                    $"{
                        context.GetDesignerValue(
                            info,
                            info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                    }.Id",
                    kind = SelectMenuDefaultValueKind.Role
                );
                return true;
            }

            if (
                context.Compilation.HasImplicitConversion(
                    info.Symbol,
                    context.KnownTypes.SelectMenuDefaultValueType
                )
            )
            {
                kind = SelectMenuDefaultValueKind.Unknown;
                source = context.GetDesignerValue(
                    info,
                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                );
                return true;
            }

            kind = SelectMenuDefaultValueKind.Unknown;
            source = string.Empty;
            return false;
        }
        
        protected override void ValidateInternal(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
        {
            if (!TryGetSourceAndKind(context, out _source, out var kind))
            {
                context.AddDiagnostic(
                    Diagnostics.TypeMismatch,
                    interpolation,
                    context.GetInterpolationInfo(interpolation).Symbol,
                    "select menu default"
                );

                return;
            }

            switch (selectState.Kind)
            {
                case SelectKind.Channel when kind is not SelectMenuDefaultValueKind.Channel and not SelectMenuDefaultValueKind.Unknown:
                case SelectKind.User when kind is not SelectMenuDefaultValueKind.User and not SelectMenuDefaultValueKind.Unknown:
                case SelectKind.Role when kind is not SelectMenuDefaultValueKind.Role and not SelectMenuDefaultValueKind.Unknown:
                    context.AddDiagnostic(
                        Diagnostics.InvalidSelectMenuDefaultKindInCurrentMenu,
                        interpolation,
                        kind,
                        selectState.Kind
                    );
                    break;
            }

            _kind = kind;
        }

        public override string Render(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
        {
            if (_kind is null || _source is null) return string.Empty;

            if (_kind is SelectMenuDefaultValueKind.Unknown) return _source;

            return FromIdAndKind(context, _source, _kind.Value);
        }
    }
    
    public sealed class Element(CXElement element) : SelectMenuDefaultValue
    {
        private SelectMenuDefaultValueKind? _kind;
        private CXValue? _value;

        protected override void ValidateInternal(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
        {
            _kind = element.Identifier.ToLowerInvariant() switch
            {
                "user" => SelectMenuDefaultValueKind.User,
                "role" => SelectMenuDefaultValueKind.Role,
                "channel" => SelectMenuDefaultValueKind.Channel,
                _ => null
            };

            if (_kind is null)
            {
                context.AddDiagnostic(
                    Diagnostics.InvalidSelectMenuDefaultKind,
                    element,
                    element.Identifier
                );

                return;
            }

            if (element.Children.Count is 0)
            {
                context.AddDiagnostic(
                    Diagnostics.MissingSelectMenuDefaultValue,
                    element
                );

                return;
            }

            if (element.Children.Count > 1)
            {
                context.AddDiagnostic(
                    Diagnostics.TooManyValuesInSelectMenuDefault,
                    TextSpan.FromBounds(
                        element.Children[1].Span.Start,
                        element.Children.Last().Span.End
                    )
                );

                return;
            }

            if (element.Children[0] is not CXValue value)
            {
                context.AddDiagnostic(
                    Diagnostics.InvalidSelectMenuDefaultChild,
                    element.Children[0],
                    element.Children[0].GetType().Name
                );

                return;
            }

            _value = value;
        }

        public override string Render(ComponentContext context, SelectMenuComponentNode.SelectState selectState)
        {
            if (_value is null || !_kind.HasValue) return string.Empty;
            
            switch (_value)
            {
                case CXValue.Scalar scalar:
                    if (!ulong.TryParse(scalar.Value, out var id))
                    {
                        context.AddDiagnostic(
                            Diagnostics.TypeMismatch,
                            scalar,
                            "text",
                            "ulong"
                        );

                        return string.Empty;
                    }

                    return FromId(id.ToString());

                case CXValue.Interpolation interpolation:
                    var info = context.GetInterpolationInfo(interpolation);

                    if (
                        context.Compilation.HasImplicitConversion(
                            info.Symbol,
                            context.Compilation.GetSpecialType(SpecialType.System_UInt64)
                        )
                    )
                    {
                        return FromId(
                            $"(global::System.UInt64){
                                context.GetDesignerValue(
                                    info,
                                    info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                )
                            }"
                        );
                    }

                    switch (_kind)
                    {
                        case SelectMenuDefaultValueKind.User
                            when context.Compilation.HasImplicitConversion(
                                info.Symbol,
                                context.KnownTypes.IUserType
                            ):
                        case SelectMenuDefaultValueKind.Role
                            when context.Compilation.HasImplicitConversion(
                                info.Symbol,
                                context.KnownTypes.IRoleType
                            ):
                        case SelectMenuDefaultValueKind.Channel
                            when context.Compilation.HasImplicitConversion(
                                info.Symbol,
                                context.KnownTypes.IChannelType
                            ):
                            return FromId(
                                $"{
                                    context.GetDesignerValue(
                                        info,
                                        info.Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                    )
                                }.Id"
                            );
                    }

                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        interpolation,
                        info.Symbol!.ToDisplayString(),
                        _kind.Value.ToString()
                    );

                    return string.Empty;
                default:
                    context.AddDiagnostic(
                        Diagnostics.TypeMismatch,
                        _value?.Span ?? element.Span,
                        _value?.GetType().Name,
                        _kind.Value.ToString()
                    );

                    return string.Empty;
            }

            string FromId(string id)
            {
                if (_kind is null) return string.Empty;

                return FromIdAndKind(context, id, _kind.Value);
            }
        }
    }
}