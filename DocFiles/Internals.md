
# Internals

- VS 2019 solution, targets .NET5 WinForms.
- Uses Roslyn for in-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\Nebulator`.

## Design TODO1

- assemblies -> user script
- N Channels to each IOutputDevice
- 1 ChannelControl per Channel
- LogLevel: Trace Debug Info Warn Error Fatal Off

## Code Files

```
root
|   Nebulator.sln 
|   README.md
|   LICENSE
|   version.txt (contains current version string)
|   mkdoc.py (builds the single doc file Main.md.html)
|   
+---App (main app and UI)
|   |   App.csproj
|   |   Program.cs
|   |   MainForm.*
|   |   Compiler.cs
|   |   Logging.cs
|   |   NLog.config
|   |               
|   +---UI (controls and forms)
|   |       ChannelControl.*
|   |       Keyboard.*
|   |       SettingsEditor.*
|   |       TimeControl.*
|   |       
|   +---Midi (midi I/O)
|   |       MidiInput.cs
|   |       MidiOutput.cs
|   |       MidiUtils.cs
|   |                   
|   +---OSC (OSC I/O)
|   |       OscCommon.cs
|   |       OscInput.cs
|   |       OscOutput.cs
|   |       
|   \---Resources
|           glyphicons-*.png
|           medusa.ico
|           
+---Common (separate assembly so it can be linked with the user script)
|       Common.csproj
|       Bag.cs
|       Channel.cs
|       IDevice.cs
|       MusicDefinitions.cs
|       Step.cs
|       StepCollection.cs
|       Time.cs
|       UserSettings.cs
|       Utils.cs
|       Wobbler.cs
|
+---Script (base class/assembly for user script)
|       Script.csproj
|       ScriptApi.cs
|       ScriptCore.cs
|       ScriptUtils.cs
|       Section.cs
|       Sequence.cs
|       
+---Examples (see #main/examplescriptfiles)
|           
+---lib (third party non-nuget)
|       
+---Test (not enough test stuff)
|
\---DocFiles (source for doc build)
        Main.md
        Internals.md
        MusicDefinitions.md
        ScriptApi.md
        ScriptSyntax.md
```
      

## Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Chunks could be stripped out for subsequent reuse.

- Midi and OSC classes may be useful elsewhere in conjunction with NAudio.
- Embedded Roslyn in-memory compiler.
- See [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks) for more goodies.


## Third Party
This application uses these FOSS components.

- NAudio including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Logging: [NLog](http://nlog-project.org/) (BSD 3-Clause).
- Markdown rendering: [Markdeep](https://casual-effects.com/markdeep).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
