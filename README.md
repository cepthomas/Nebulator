
# What This Is
Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

Windows only... uses some Win32 calls but they could be re-worked for Linux.

I played around with various scripting concepts, but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported using midi/OSC inputs.

API, examples etc are in:
* [General Script Syntax](ScriptSyntax.md.html) TODO1 this ext won't work for github flavor. also inserts below.
* [API for User scripts](ScriptApi.md.html)
* [Midi defs, scales, chords Definitions](MusicDefinitions.md.html)
* [Design and Build notes](Internals.md.html)


## Usage
- Top pane is the global transport control and channel controls.
- Bottom pane is log and comm tracing. Note that comm tracing has an impact on performance so use it judiciously.
- Basically open a .neb file, press compile, then run.
- Rather than spending the effort on a built-in script editor, use your favorite external text editor. The application will watch for
  changes you make and indicate that recompile is needed. I use Sublime - you can associate .neb files with C# for pretty-close syntax coloring.


## Example Script Files
See the Examples directory for material while perusing this.

File | Description
---- | -----------
example.neb | Source file showing example of static sequence and loop definitions, and creating notes by script functions.
airport.neb | My take on Eno's Music for Airports - from [this](https://github.com/teropa/musicforairports.js).
utils.neb   | Example of a library file for simple functions.
*.nebp      | Storage for dynamic stuff. This is created and managed by the application and not generally manually edited.
temp\\\*.cs | Generated C# files which are compiled and executed.
example.mp3 | A bit of some generated sound (not music!) using Reaper with good instruments and lots of reverb. I like lots of reverb.
airport.mp3 | Snippet generated by airport.neb and Reaper.


## Third Party
This application uses these FOSS components.

- NAudio including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Logging: [NLog](http://nlog-project.org/) (BSD 3-Clause).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
