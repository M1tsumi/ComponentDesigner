using Discord.CX.Parser;
using Microsoft.CodeAnalysis;

namespace Discord.CX;

public static partial class Diagnostics
{
    public static Diagnostic CreateParsingDiagnostic(CXDiagnostic diagnostic, Location location)
        => Diagnostic.Create(
            new DiagnosticDescriptor(
                $"DCP{diagnostic.Code.ToString().PadLeft(3, '0')}",
                diagnostic.Message,
                string.Empty,
                "CX Parser",
                diagnostic.Severity,
                true
            ),
            location
        );

    public static readonly DiagnosticDescriptor InvalidEnumVariant = new(
        "DC0001",
        "Invalid enum variant",
        "'{0}' is not a valid variant of '{1}'; valid values are '{2}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TypeMismatch = new(
        "DC0002",
        "Type mismatch",
        "'{0}' is not of expected type '{1}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor OutOfRange = new(
        "DC0003",
        "Type mismatch",
        "'{0}' must be {1} in length",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor UnknownComponent = new(
        "DC0004",
        "Unknown component",
        "'{0}' is not a known component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonCustomIdUrlConflict = new(
        "DC0005",
        "Invalid button",
        "Buttons cannot contain both a 'url' and a 'customid'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonCustomIdOrUrlMissing = new(
        "DC0006",
        "Invalid button",
        "A button must specify either a 'customId' or a 'url'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor LinkButtonUrlMissing = new(
        "DC0007",
        "Invalid button",
        "A 'link' button must specify 'url'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor PremiumButtonSkuMissing = new(
        "DC0008",
        "Invalid button",
        "A 'premium' button must specify 'skuId'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor PremiumButtonPropertyNotAllowed = new(
        "DC0009",
        "Invalid button",
        "A 'premium' button cannot specify '{0}'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ButtonLabelDuplicate = new(
        "DC0010",
        "Duplicate label definition",
        "A button cannot specify both a body and a 'label'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor EmptyActionRow = new(
        "DC0011",
        "Empty Action Row",
        "An action row must contain at least one child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingRequiredProperty = new(
        "DC0012",
        "Missing Property",
        "'{0}' requires the property '{1}' to be specified",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor UnknownProperty = new(
        "DC0013",
        "Unknown Property",
        "'{0}' is not a known property of '{1}'",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );

    public static readonly DiagnosticDescriptor EmptyAccessory = new(
        "DC0014",
        "Empty Accessory",
        "An accessory must have 1 child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyAccessoryChildren = new(
        "DC0015",
        "Too many accessory children",
        "An accessory must have 1 child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor EmptySection = new(
        "DC0016",
        "Section cannot be empty",
        "A section must have an accessory and a child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidAccessoryChild = new(
        "DC0017",
        "Invalid accessory child",
        "'{0}' is not a valid accessory, only buttons and thumbnails are allowed",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingAccessory = new(
        "DC0018",
        "Missing accessory",
        "A section must contain an accessory",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManyAccessories = new(
        "DC0019",
        "Too many accessories",
        "A section can only contain one accessory",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingSectionChild = new(
        "DC0020",
        "Missing section child",
        "A section must contain at least 1 non-accessory component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor TooManySectionChildren = new(
        "DC0021",
        "Too many section children",
        "A section must contain at most 3 non-accessory components",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidSectionChildComponentType = new(
        "DC0022",
        "Invalid section child component type",
        "'{0}' is not a valid child component of a section; only text displays are allowed",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingSelectMenuType = new(
        "DC0023",
        "Missing select menu type",
        "You must specify the type of the select menu, being one of 'string', 'user', 'role', 'channel', or 'mentionable'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidSelectMenuType = new(
        "DC0024",
        "Invalid select menu type",
        "Select menu type must be either 'string', 'user', 'role', 'channel', or 'mentionable'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor SpecifiedInvalidSelectMenuType = new(
        "DC0025",
        "Invalid select menu type",
        "'{0}' is not a valid elect menu type; must be either 'string', 'user', 'role', 'channel', or 'mentionable'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor ActionRowInvalidChild = new(
        "DC0026",
        "Invalid action row child component",
        "An action row can only contain 1 select menu OR at most 5 buttons",
        "Components",
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor InvalidPropertyValueSyntax = new(
        "DC0027",
        "Invalid syntax",
        "Expected '{}' as the property value",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor ButtonMustHaveALabelOrEmoji = new(
        "DC0028",
        "A button must have a label or emoji",
        "A button must have a label or emoji",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor PossibleInvalidEmote = new(
        "DC0029",
        "Possible invalid emote",
        "'{0}' doesn't look like a unicode emoji or a custom emote",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );
    
    public static readonly DiagnosticDescriptor InvalidChildComponentCardinality = new(
        "DC0030",
        "Too many children",
        "'{0}' only accepts one child",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor FileUploadNotInLabel = new(
        "DC0031",
        "A file upload component must be placed in a label",
        "File uploads' parent must be a label",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor MissingTypeInAssembly = new(
        "DC0032",
        "Missing type in assembly",
        "Could not find '{0}' in your assembly",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor MissingLabelComponent = new(
        "DC0033",
        "Label is missing a child component",
        "Label requires a child component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor TooManyChildrenInLabel = new(
        "DC0034",
        "Too many children in Label",
        "Labels can only contain 1 component",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor InvalidLabelChild = new(
        "DC0035",
        "Invalid label child component",
        "Labels can only contain a text input, file upload, or select menu",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor InvalidContainerChild = new(
        "DC0036",
        "Invalid container child component",
        "'{0}' is not a valid child component of 'container'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor InvalidMediaGalleryChild = new(
        "DC0037",
        "Invalid media gallery child component",
        "'{0}' is not a valid child component of 'media gallery'",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor PropertyNotAllowed = new(
        "DC0038",
        "Property not allowed",
        "'{0}' doesn't allow the property '{1}' to be specified in the current configuration",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor ButtonCannotHaveALabelAndEmoji = new(
        "DC0039",
        "Buttons can't have a label and emoji",
        "Buttons can't have both a label and an emoji",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor CardinalityForcedToRuntime = new(
        "DC0040",
        "Cardinality forced to runtime check",
        "'{0}' can be more than 1 component, a runtime check will occur to enforce a single component",
        "Components",
        DiagnosticSeverity.Warning,
        true
    );
    
    public static readonly DiagnosticDescriptor LabelComponentDuplicate = new(
        "DC0041",
        "Duplicate component definition",
        "A label cannot specify 'component' both in an attribute and in the children",
        "Components",
        DiagnosticSeverity.Error,
        true
    );
}
