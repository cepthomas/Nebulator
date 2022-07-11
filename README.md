
# What This Is

Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.

Requires VS2022 and .NET6.

I played around with various scripting concepts, but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported using midi/OSC inputs.


# Main Documentation TODOX scrub all

- [Main Documentation](DocFiles/Nebulator.md)
- [Script Syntax](DocFiles/ScriptSyntax.md)
- [Script API](DocFiles/ScriptApi.md)
- [Music Definitions](DocFiles/MusicDefinitions.md)
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

