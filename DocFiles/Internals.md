# Internals

- VS 2019 solution, targets .NET6 WinForms.
- Uses Roslyn for in-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\Nebulator`.

## Design

- Three projects. `Common` and `Script` are compiled separately into assemblies
  so that they can be linked with the compiled user script dynamically.
- Main UI and non user script stuff is all in the `App` project.
- Channels and Controllers follow the midi model. Devices represent ports (and corresponding physical dvices.)
- You can have up to 16 Channels attached to each IOutputDevice.
- There is one ChannelControl per Channel.


## Code Files

```
root
|   Nebulator.sln 
|   README.md
|   
+---App (main app and UI)
|   |   App.csproj
|   |   Program.cs
|   |   MainForm.*
|   |   Compiler.cs
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
+---Examples (see  #nebulator/examplescriptfiles)
|           
+---lib (third party non-nuget)
|       
+---Test (pathetic test stuff)
|
\---DocFiles
        Nebulator.md
        ScriptSyntax.md
        ScriptApi.md
        Internals.md
        MusicDefinitions.md
```

## Midi Controllers

Nebulator adds a couple of hidden controller values for internal use.

Controller          | Number |
----------          | ------ |
NoteControl         | 250    |
PitchControl        | 251    |


## Bonus Stuff
This project contains a bunch of components that are either recycled or created for this. Chunks could be stripped out for subsequent reuse.

- Midi and OSC classes may be useful elsewhere in conjunction with NAudio.
- Embedded Roslyn in-memory compiler.


# Third Party

This application uses these FOSS components:
- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
- Markdown rendering: [Markdeep](https://casual-effects.com/markdeep).
- [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md)
- [NBagOfUis](https://github.com/cepthomas/NBagOfUis/blob/main/README.md)

