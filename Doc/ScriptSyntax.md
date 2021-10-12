
# Script Syntax

This section describes the general structure and syntax rules of script files.  

Script functions are very C#-like because essentially it is C# (without stuff like namespaces). The compiler adds in the surrounding boilerplate and compiles the whole mess in memory where it executes.  

You can clean up your script file using [AStyle](http://astyle.sourceforge.net/).
```
AStyle --style=allman <your-file>
```

## General
Double slash `//` is used for comments. C style `/* ... */` is not supported.  

Names cannot have spaces or begin with a number.  


## Classes
Classes are supported (see utils.neb).
```c#
class klass
{
    public void DoIt(int val)
    {
        Print("DoIt got:", val);
    }
}    
```
