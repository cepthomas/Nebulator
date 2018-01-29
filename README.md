
>>>>>>>>>
Nebulator includes an emulation of a subset of Processing functions. The following sections list the
supported elements in roughly the same structure as the original reference ([Processing API](https://processing.org/reference/)).  
There are lots of unimplemented functions and properties, including some of the overloaded flavors.
If it's not implemented, you get either a compiler error or a runtime ScriptNotImplementedException.
Note that a lot of these have not been properly tested. Eventually there may be a real unit tester project.


see NProcessing....




# What It Is
I grew frustrated with traditional piano roll midi editors as found in your typical DAW. Another case of an implementation that sounded better during the requirements phase than after it was done (Google `Data Visualization`). Instead of dragging little dots around, I would think that I could write a simple script to do that. So that's what this is intended to do.  

I played around with various scripting concepts (I do like Lua and it may reappear), but ended up realizing that C# makes a reasonable scripting language, given that we have the compiler available to us at run time. Actually you can create compositions without any script functions at all, but they are there if you want them. The main music part is all declarative.  

I decided to do this as all midi, rather than try to implement native sound generation. The rationale is that I (and you) have plenty of great sounding instruments and effects using my DAW ([Reaper](https://www.reaper.fm/)), so may as well use those via a midi loopback driver. Midi has some severe limitations so OSC may appear later.  

Some interesting reads on music-by-code:
- [Michael Gogins](http://csoundjournal.com/issue17/gogins_composing_in_cpp.html)
- [ChucK](http://chuck.cs.princeton.edu/)
- [Peter Langston](http://www.langston.com/Papers/llfm.pdf)

While the primary intent is to generate music-by-code, runtime interaction is also supported. It's called Nebulator after a MarkS C++ noisemaker called Nebula which allowed manipulation of parameters using custom UI inputs, and display of whatever on the UI. The API is similar to that for [Processing](https://processing.org/). The app uses [NProcessing](https://github.com/cepthomas/NProcessing) for the UI stuff.  

And a static declarative model is also supported if you want to write note sequence descriptions instead. Or you can combine declarative and procedural in the same piece. Wow!

It's all WinForms. I recognize that WPF/UWP is superior (right?) technology but I can bang out most UI I need in WF lickety split.

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
- [MoreLinq](https://morelinq.github.io)
- NAudio DLL and modified controls, midi file: [NAudio](https://github.com/naudio/NAudio)
- Modified multimedia timer and piano control: [Leslie Sanford's Midi Toolkit](https://github.com/tebjan/Sanford.Multimedia.Midi)
- Logging: [NLog](http://nlog-project.org/)
- Markdown processing: [Markdig](https://github.com/lunet-io/markdig)
- Json processor: [Newtonsoft](https://www.nuget.org/packages/Newtonsoft.Json/). Note - uses 9.0.1 - don't update!
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/)
- Button icons: [Glyphicons Free](http://glyphicons.com/)

# License
https://github.com/cepthomas/Nebulator/blob/master/LICENSE
