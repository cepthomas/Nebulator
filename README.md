
# What This Is

Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.

Requires VS2019 and .NET5. Windows only... uses some Win32 calls but they could be re-worked for Linux.

I played around with various scripting concepts, but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported using midi/OSC inputs.

See [Main Documentation](Nebulator.html).
