# Discord.Net Component Designer

The Component Designer allows for defining components in a html-like syntax

```cs
using static Discord.ComponentDesigner;

var myComponent = cx(
    """
    <text>
        Hello world!
    </text>
    """
);
```
 
