
# What This Is
Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

Windows only... uses some Win32 calls but they could be re-worked for Linux.

I played around with various scripting concepts, but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC.

While the primary intent is to generate music-by-code, runtime interaction is also supported using midi/OSC inputs.

See [Main Documentation](Nebulator.md.html).
