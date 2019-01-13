
# What This Is
Most music software uses piano roll midi editors. This is an alternative - writing scripts to generate sounds.

I played around with various scripting concepts (I do like Lua and it may reappear), but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

It supports midi and OSC, and has a WIP native synthesizer engine.

While the primary intent is to generate music-by-code, runtime interaction is also supported. It's called Nebulator after a Mark S C++ noisemaker called Nebula which allowed manipulation of parameters using custom UI inputs, and display of whatever on the UI.

The script syntax is roughly based on that used by [Processing](https://processing.org/), combined with C# language features.
A subset of the Processing graphics functions is implemented to support the UI/graphical aspects. Simple Processing scripts should port easily and run fine. For specifics on that aspect see [NProcessing](https://github.com/cepthomas/NProcessing).

For lots more info see the [Wiki](https://github.com/cepthomas/Nebulator/wiki)

# Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Most could be stripped out for subsequent reuse.
- A theoretically better multimedia timer with improved accuracy for sub 10 msec period.
- Midi classes may be useful elsewhere in conjunction with NAudio.
- Partial import of Yahama style (.sty) files.
- Multiple file change watcher.
- General purpose embedded C# in memory compiler.
- Virtual keyboard control based on Leslie Sanford's piano.
- Synthesizer engine based on ChucK/STK.
- Various utilities and extensions.
- Simple plotting control.
- Example of the use of [pnut unit tester](https://github.com/cepthomas/pnut) 

# Third Party
This application uses these FOSS components:
- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- SkiaSharp for graphics: [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT). Waaaay faster than native GDI+.
- Modified multimedia timer and piano control: [Leslie Sanford's Midi Toolkit](https://github.com/tebjan/Sanford.Multimedia.Midi) (MIT).
- Logging: [NLog](http://nlog-project.org/) (BSD 3-Clause).
- [MoreLinq](https://morelinq.github.io) (MIT).
- Markdown processing: [Markdig](https://github.com/lunet-io/markdig) (BSD 2-Clause)
- Json processor: [Newtonsoft](https://github.com/JamesNK/Newtonsoft.Json) (MIT) Note - uses 9.0.1 - don't update!
- Web server: [embedio](https://github.com/unosquare/embedio) (MIT).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
- The Synth component is ported from [ChucK](http://chuck.cs.princeton.edu/) (GPL)

# License
https://github.com/cepthomas/Nebulator/blob/master/LICENSE
