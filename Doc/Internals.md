
# Internals
It's called Nebulator after a ![MarkS](marks.bmp width="150px") C++ noisemaker called Nebula which allowed manipulation of synth parameters using code.

- Targets .NET5 WinForms.
- Uses Roslyn for iin-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package could be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\Nebulator`.


## Code

- TODO1 assemblies
- LogLevel: Trace Debug Info Warn Error Fatal Off
- Design
  - N Channels to each IOutputDevice
  - 1 ChannelControl per Channel


## Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Most could be stripped out for subsequent reuse.

- Midi and OSC classes may be useful elsewhere in conjunction with NAudio.
- General purpose embedded Roslyn in-memory compiler.
- See [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks) for more goodies.
