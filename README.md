
# What This Is

Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.


Requires VS2022 and .NET6.

C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

Supports midi and midi-over-OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported using midi or OSC inputs.

It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.

# Usage
- Main window includes the transport control and one per channel controls.
- Also has log and comm tracing. Note that comm tracing has an impact on performance so use it judiciously.
- Basically open a .neb file, press compile, then run.
- Use your favorite external text editor. The application will watch for changes you make and indicate that recompile
  is needed. I use Sublime - you can associate .neb files with C# for pretty-close syntax coloring.
- Click on the settings icon to edit your devices and options.


# Example Script Files
See the Examples directory for material while perusing the docs.

File        | Description
----------- | -----------
example.neb | Source file showing example of static sequence and loop definitions, and creating notes by script functions.
airport.neb | A take on Eno's Music for Airports - adapted from [this](https://github.com/teropa/musicforairports.js).
utils.neb   | Example of a library file for simple functions.
scale.neb   | Example of a library file for playing with a scale.
*.nebp      | Storage for dynamic stuff. This is created and managed by the application and not generally manually edited.
temp\\\*.cs | Generated C# files which are compiled and executed.
example.mp3 | A bit of some generated sound (not music!) using Reaper with good instruments and lots of reverb. I like lots of reverb.
airport.mp3 | Snippet generated by airport.neb and Reaper.


# The Documentation

- [Script Syntax](DocFiles/ScriptSyntax.md)
- [Script API](DocFiles/ScriptApi.md)
- [Internals](DocFiles/Internals.md)


# Third Party

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
- Markdown rendering: [Markdeep](https://casual-effects.com/markdeep).
- [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md)
- [NBagOfUis](https://github.com/cepthomas/NBagOfUis/blob/main/README.md)
- [MidiLib](https://github.com/cepthomas/MidiLib/blob/main/README.md)
- [NebOsc](https://github.com/cepthomas/NebOsc/blob/main/README.md)

