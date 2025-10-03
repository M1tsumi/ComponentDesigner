namespace Discord;

public sealed class CXPropertyAttribute : Attribute
{
    public string? Name { get; set; }
    
    public bool IsOptional { get; set; }
    
    public string[]? Aliases { get; set; }
}