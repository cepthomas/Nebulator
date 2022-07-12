
# Script API

What the script supports.

## Basics

### Time
Time uses the `BarTime` class. A shorthand is provided to specify a time using `var mytime = new BarTime(2.3)` where the 

Neb doesn't care about measures, that's up to you.

Some pieces work better with absolute time so scripts have access to a `Now` property that supplies the number of seconds since the start button was pressed.

### Devices and Protocols
Midi and OSC are supported by Nebulator (or at least reasonable subsets). Devices are specified in the settings file:

- MidiInput: Midi in port name on your machine.
- MidiOutput: Midi out port name on your machine.
- OscInput: As `OSC:1234` where `1234` is the local port number.
- OscOutput: As `OSC:127.0.0.1:1234` where `127.0.0.1` is the endpoint IP and `1234` is the port number.
- VirtualKeyboard: Virtual keyboard as input.
- BingBong: 2D input surface.

Since the built-in Windows GM player sounds terrible, there are a couple of options for playing midi locally:

- Replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
- If you are using a DAW for sound generation, you can use a virtual midi loopback like [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to send to it.

### Musical Notes/Chords/Scales
Note groups are specified by strings like `"1 4 6 b13"` using `CreateNotes("FOO", "1 4 6 b13")`.

Notes (single) and note groups (chords, scales) are referenced in several ways:
- "F4" - Named note with octave.
- "F4.o7" - Named chord from [Chords](#musicdefinitions/chords) in the key of middle F.
- "F4.Aeolian" - Named scale from [Scales](#musicdefinitions/scales).
- "F4.FOO" - Custom chord or scale created with `CreateNotes()` see above
- SideStick - Drum name from [Drums](#musicdefinitions/generalmididrums).
- 57 - Simple midi note number.


## Directives

### Include
```c#
Include("user_path\utils.neb");
```

A simple include mechanism is supplied. It's pretty much an insert-whole-file-body-here, no wildcard support.
Tries absolute path first, then relative to the setting `ProjectPath`. It needs to be specified before contents are referenced.
If one were to get grandiose, a true import could be implemented.


### Channel
```c#
Channel("chname", "devid", chnum, "patch")
```

Defines an output channel.

- chname: For display in the UI.
- devid: A user-specified device id as entered in the user settings.
- chnum: Channel number to play on.
- patch: Name of the patch.


## Composition
A composition is comprised of one or more Sections each of which has one or more Sequences of notes.
You first create your Sequences like this:
```c#
Sequence CreateSequence(beats, elements[]);
```

There are several ways to do this. (see `example.neb`.)
A list of notes:
```c#
Sequence seq1 = CreateSequence(8, new()
{
    { 0.0, "F4",  0.7, 0.2 },
    { 0.4, "D#4", 1.1, 0.2 },
    { 1.0, "C4",  0.7, 0.2 },
});
```
- 8 is the number of beats in the sequence.
- Notes are `{ when to play in the sequence, note or chord or drum, volume, duration }`. If duration is omitted, it defaults to 0.1, useful for drum hits.

```c#
Sequence seq2 = CreateSequence(8, new()
{
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7", 0.9  },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6", 0.75 },
});
```
- Notes are `{ pattern, note or chord or drum, volume, duration }`.
- Pattern: describes a sequence of notes, kind of like a piano roll. `1 to 9` (volume) starts a note which is held 
  for subsequent `-`. The note is ended with any other character than `-`. `|`, `.` and ` ` are ignored, 
  used for visual assist only. These are particularly useful for drum patterns.

```c#
Sequence seqAlgo = CreateSequence(4, new()
{
    { 1.2, AlgoFunc, 0.8 },
});

void AlgoFunc()
{
    int notenum = Random(0, scaleNotes.Count());
    SendNote("synth", scaleNotes[notenum], 0.7, 0.5);
}
```
- Notes are `{ when, function, volume }`.

Then you group Sequences into Sections, typically things like verse, chorus, bridge, etc.
```c#
Section sectMiddle = CreateSection(32, "Middle", new()
{
    { "keys",  seqKeysChorus  },
    { "drums", seqDrumsChorus },
    { "bass",  seqBassChorus  },
    { "synth", seqAlgo, seqEmpty, seqAlgo, seqDynamic, seqEmpty }
});
```
- beats: Overall length in beats.
- name: Displayed in time control while playing.
- sequences: 1 to N descriptors of which sequences to play sequentially. They are played in order and repeat to fill the section.

## Runtime

### Properties
Accessible or modified in the script.

```c#
bool Playing
```
Indicates that script is executing. Read only.

```c#
BarTime StepTime
```
Current Nebulator step time object. Read only. Also use for StepTime.Beat and StepTime.subdiv.

```c#
double RealTime
```
Seconds since playing started. Read only.

```c#
double Speed
```
Nebulator speed in BPM. Read/write.

```c#
int MasterVolume
```
Nebulator master volume. Read/write.


### Callback Functions
These can be overridden in the user script.

```c#
public override void Setup()
```
Called once to initialize your script stuff.

```c#
public override void Step()
```
Called every Subdiv.

```c#
public override void InputNote(dev, chnum, note)
```
Called when input note arrives.

- dev: DeviceType.
- chnum: Channel number.
- note: Note number.

```c#
public override void InputControl(dev, chnum, ctlid, value)
```
Called when input controller arrives.
- dev: DeviceType.
- chnum: Channel number.
- ctlid: ControllerDef.
- value: Controller value.


### Send Functions
Call these from inside your script.

```c#
void SendNote("chname", note, vol, dur)
```
Send a note immediately. Respects solo/mute. Adds a note off to play after dur time.

- chname: Channel name to send it on.
- note: One of [Notes](#scriptapi/basics/musicalnotes).
- vol: Note volume. Normalized to 0.0 - 1.0. 0.0 means note off.
- dur: How long it lasts in beats or BarTime object representation.

```c#
void SendNoteOn("chname", note, vol)
```
Send a note on immediately. Respects solo/mute. Doesn't add note off.

- chname: Channel name to send it on.
- note: One of [Notes](#scriptapi/basics/musicalnotes).
- vol: Note volume. Normalized to 0.0 - 1.0.

```c#
void SendNoteOff("chname", note)
```
Send a note off immediately.

- chname: Channel name to send it on.
- note: One of [Notes](#scriptapi/basics/musicalnotes).

```c#
void SendController("chname", ctl, val)
```
Send a controller immediately. Useful for things like panning and bank select.

- chname: Channel name to send it on.
- ctl: Controller from [Controllers](#musicdefinitions/midicontrollers) or const() or simple integer.
- val: Controller value.

```c#
void SendPatch("chname", patch)
```
Send a midi patch immediately. Really only needed if using the windows GM.

- chname: Channel name to send it on.
- patch: Instrument from [Instruments](#musicdefinitions/generalmidiinstruments).

### Utilities

```c#
void CreateNotes("name", "parts")
```
Define a group of notes for use as a note, or in a chord or scale.

- name: Reference name.
- note: List of [Notes](#scriptapi/basics/musicalnotes).

```c#
List<double> GetNotes("scale_or_chord", "key")
```
Get an array of scale or chord notes.

- scale: One of the named scales from ScriptDefinitions.md or defined in `notes`.
- key: Note name and octave.
- returns: Array of notes or empty if invalid.

```c#
double Random(double max);
double Random(double min, double max);
int Random(int max);
int Random(int min, int max);
```
Various flavors of randomizers.


```c#
void Print(object arg, object arg, ...);
```
You know what this is.