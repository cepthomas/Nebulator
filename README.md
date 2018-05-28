
# What It Is
I grew frustrated with traditional piano roll midi editors as found in your typical DAW. Another case of an implementation that sounded better during the requirements phase than after it was done. Instead of dragging little dots around, I would think that I could write a simple script to do that. So that's what this is intended to do.  

I played around with various scripting concepts (I do like Lua and it may reappear), but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time.

I decided to do this as all midi, rather than try to implement native sound generation. The rationale is that I (and you) have plenty of great sounding instruments and effects using my [DAW](https://www.reaper.fm/), so may as well use those via a midi loopback driver. Midi has some severe limitations so OSC could appear later.  

Some interesting reads on music-by-code:
- [Michael Gogins](http://csoundjournal.com/issue17/gogins_composing_in_cpp.html)
- [ChucK](http://chuck.cs.princeton.edu/)
- [Peter Langston](http://www.langston.com/Papers/llfm.pdf)

While the primary intent is to generate music-by-code, runtime interaction is also supported. It's called Nebulator after a Mark S C++ noisemaker called Nebula which allowed manipulation of parameters using custom UI inputs, and display of whatever on the UI.

The script syntax is roughly based on that used by [Processing](https://processing.org/), combined with C# language features. In fact, a subset of the Processing graphics functions is implemented to support the UI/graphical aspects. Simple Processing scripts should port easily and run fine.

For lots more info see the [Wiki](https://github.com/cepthomas/Nebulator/wiki)


# Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Most could be stripped out for subsequent reuse.
- A theoretically better multimedia timer with improved accuracy for sub 10 msec period.
- Midi classes may be useful elsewhere in conjunction with NAudio.
- A state machine class based on one that was used in several products.
- Partial import of Yahama style (.sty) files.
- Multiple file change watcher.
- General purpose embedded C# in memory compiler.
- Piano control based on Leslie Sanford's.
- Various utilities and extensions.
- Super lightweight unit tester for when NUnit is too much.


# Third Party
This application uses these excellent FOSS components:
- NAudio DLL and modified controls, midi file: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- SkiaSharp for graphics: [SkiaSharp](https://github.com/mono/SkiaSharp) (MIT). Waaaay faster than native GDI+.
- Modified multimedia timer and piano control: [Leslie Sanford's Midi Toolkit](https://github.com/tebjan/Sanford.Multimedia.Midi) (MIT).
- Logging: [NLog](http://nlog-project.org/) (BSD 3-Clause).
- [MoreLinq](https://morelinq.github.io) (MIT).
- Markdown processing: [Markdig](https://github.com/lunet-io/markdig) (BSD 2-Clause)
- Json processor: [Newtonsoft](https://github.com/JamesNK/Newtonsoft.Json) (MIT) Note - uses 9.0.1 - don't update!
- Web server: [embedio](https://github.com/unosquare/embedio) (MIT).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).

# License
https://github.com/cepthomas/Nebulator/blob/master/LICENSE
