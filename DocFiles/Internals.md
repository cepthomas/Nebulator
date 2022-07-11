# Internals

- VS2022 solution, targets .NET6 WinForms.
- Uses Roslyn for in-memory compilation.
- No installer yet, it's a build-it-yerself for now. Eventually a nuget package might be created.
- Settings and log are in `C:\Users\<user>\AppData\Local\Ephemera\Nebulator`.

## Design

- `Script.csproj` is compiled separately into an assembly so it can be linked with the user script.
- Main UI and non user script stuff is all in the `App` project.
- Channels and Controllers follow the midi model. Devices represent ports (and corresponding physical dvices.)
- You can have up to 16 Channels attached to each IOutputDevice.


## Midi Controllers

Nebulator adds a couple of hidden controller values for internal use.

Controller          | Number |
----------          | ------ |
NoteControl         | 250    |
PitchControl        | 251    |


