using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Discord.CX;

public static class StringUtils
{
    public static string Indent(this string value, int size)
    {
        if (size is 0) return value;

        var padStr = new string(' ', size);
        
        var split = value.Split('\n');

        if (split.Length is 1) return $"{padStr}{value}";

        return string.Join(
            "\n",
            split.Select(x => $"{padStr}{x}")
        );
    }
    
    public static string Prefix(this string str, int count, char prefixChar = ' ')
        => count > 0 ? $"{new string(prefixChar, count)}{str}" : str;

    public static string Postfix(this string str, int count, char prefixChar = ' ')
        => count > 0 ? $"{str}{new string(prefixChar, count)}" : str;

    public static string WithNewlinePadding(this string str, int pad)
        => str.Replace("\n", "\n".Postfix(pad));

    public static string WrapIfSome(this string str, string wrapping)
        => string.IsNullOrWhiteSpace(str) ? str : $"{wrapping}{str}{wrapping}";

    public static string PrefixIfSome(this string str, int count, char prefixChar = ' ')
        => string.IsNullOrWhiteSpace(str) ? str : $"{new string(prefixChar, count)}{str}";

    public static string PrefixIfSome(this string str, string prefix)
        => string.IsNullOrWhiteSpace(str) ? str : $"{prefix}{str}";

    public static string PostfixIfSome(this string str, int count, char prefixChar = ' ')
        => string.IsNullOrWhiteSpace(str) ? str : $"{str}{new string(prefixChar, count)}";

    public static string PostfixIfSome(this string str, string postfix)
        => string.IsNullOrWhiteSpace(str) ? str : $"{str}{postfix}";

    public static string Map(this string str, Func<string, string> mapper)
        => string.IsNullOrWhiteSpace(str) ? str : mapper(str);

    public static string NormalizeIndentation(this string str)
    {
        var rawLines = str.Split('\n');
        var lines = new List<string>(rawLines);

        // remove leading empty lines
        foreach (var line in rawLines)
        {
            if (string.IsNullOrWhiteSpace(line)) lines.Remove(line);
            else break;
        }
        
        // remove trailing empty lines
        for (var i = rawLines.Length - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(rawLines[i])) lines.Remove(rawLines[i]);
            else break;
        }
        
        var minSpacing = lines.Min(x =>
            x.TakeWhile(char.IsWhiteSpace).Count()
        );

        if (minSpacing is 0 or int.MaxValue) return str;
        
        return string.Join(
            "\n",
            lines.Select(x =>
                x.Length > minSpacing ? x.Substring(minSpacing) : string.Empty
            )
        );
    }
    
    public static void NormalizeIndentation(this StringBuilder str)
    {
        var normal = str.ToString().NormalizeIndentation();
        str.Clear().Append(normal);
    }
}