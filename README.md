
# What This Is
Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

I played around with various scripting concepts, but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported using midi/OSC inputs.

API, examples etc are in:
* [General Script Syntax](ScriptSyntax)
* [API for Nebulator](ScriptApi)
* [Script Definitions](ScriptDefinitions)

TODO1 mark pic.


# Usage
- Top pane is the global transport control and channel controls.
- Bottom pane is log and comm tracing. Note that comm tracing has an impact on performance so use it judiciously.
- Basically open a .neb file, press compile, then run.
- Rather than spending the effort on a built-in script editor, use your favorite external text editor. The application will watch for
  changes you make and indicate that recompile is needed. I use Sublime - you can associate `*.neb` files with C# for pretty-close syntax coloring.


# Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Most could be stripped out for subsequent reuse.

- Midi classes may be useful elsewhere in conjunction with NAudio.
- General purpose embedded C# in memory compiler.
- See [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks) for more goodies.


# Third Party
This application uses these FOSS components.

- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Logging: [NLog](http://nlog-project.org/) (BSD 3-Clause).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
