
# What This Is
Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

I played around with various scripting concepts, but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported. It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of parameters using custom UI inputs.

API, examples etc are in the [Wiki](https://github.com/cepthomas/Nebulator/wiki)

# Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Most could be stripped out for subsequent reuse.

- Midi classes may be useful elsewhere in conjunction with NAudio.
- General purpose embedded C# in memory compiler.
- See [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks) for more goodies.

# Third Party
This application uses these FOSS components.

- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
