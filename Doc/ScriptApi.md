
Support for Nebulator sound functionality. It's loosely modelled after the Processing API???

# Nebulator Specific

## Time
Time is described as a floating point value `Beat.Subdiv` where
- Beat as in the traditional definition = quarter note. From 0 to N.
- Subdiv is the subdivision of Beat. Fixed at 4 giving resolution of sixteenth notes. From 0 to 3.

Neb doesn't care about measures, that's up to you.

Previous incarnations used a floating point number instead of the Time class but this caused some annoying edge conditions.

Some pieces work better with absolute time so scripts have access to a `Now` property that supplies the number of seconds since the start button was pressed.

## Devices and Protocols
Midi and OSC are supported by Nebulator (or at least reasonable subsets). When creating IO with `CreateController()` and
`CreateChannel()`, the device parameter identifies the protocol of interest:
- Midi in or out ports you have installed on your computer as specified in your settings.
- OSC outputs as specified in the settings as "127.0.0.1:1234" where `127.0.0.1` is the endpoint IP and `1234` is the port number.
- OSC inputs as specified in the settings as the local port number.
- Also available is a virtual keyboard on the screen.

The help button will show you the devices on your computer.


There a couple of options for playing midi locally.
- Since the built-in Windows GM player sounds terrible, you can replace it with [virtualmidisynth](https://coolsoft.altervista.org/en/virtualmidisynth) and your favorite soundfont.
- If you are using a DAW for sound generation, you can use a virtual midi loopback like [loopMIDI](http://www.tobias-erichsen.de/software/loopmidi.html) to send to it.

## Musical Notes
The specification of notes (single, chord tones, scale notes) accomodates several flavors. Note numbers are the midi
values but as `double` to support microtonal.
- "F4" - Named note with octave.
- "F4.o7" - Named chord from [ScriptDefinitions](ScriptDefinitions).
- "F4.MY_CHORD" - Named chord from a script `CreateNotes()`.
- "MY_SCALE" - Named scale from a script `CreateNotes()`.
- SideStick - Drum name from [ScriptDefinitions](ScriptDefinitions).
- 57 - Midi note number. Can be a double for microtonal support.

## Warnings
Do not call any of the SendXXX() functions while in the Setup() function. The channels are not created
until the Setup2() function.

## New Types
Nebulator introduces a few new types to support music construction. They are not meant to be used directly
(aka opaque references) but created at start and passed around to the actual functions. It will make
more sense when looking at the example code.

```c#
NVariable vColor;
NChannel chDrums;
NSequence seqDrumsVerse;
```



## Include
A simple include mechanism is supplied in lieu of the Processing `import`. It's pretty much an insert-whole-file-body-here, no wildcard support. Tries absolute path first, then relative to current. It needs to be specified before contents are referenced. If one were to get grandiose, a true import could be implemented.
```c#
Include("utils.neb");
```

## Config()




## Properties
```c#
bool Playing
```
Indicates that sound is playing. Read only.

```c#
Time StepTime
```
Current Nebulator step time object. Read only.

```c#
int Beat
```
Current Nebulator Beat. Read only.

```c#
int Subdiv
```
Current Nebulator Subdiv. Read only.

```c#
double Now
```
Seconds since playing started. Read only.

```c#
double Speed
```
Nebulator speed in BPM. Read/write.

```c#
int Volume
```
Nebulator master volume. Read/write.

```c#
int SubdivsPerBeat
```
Beat subdivision. Currently fixed at 4. Read only.

## Callback Functions
These can be overridden in the user script.

```c#
public override void Setup()
```
Called once to initialize Nebulator stuff.

```c#
public override void Setup2()
```
Called if you need to do something with devices after they have been created which happens after Setup() returns.

```c#
public override void Step()
```
Called every Subdiv.

## Variables
NVariables contain the data of interest as a double. They can be bound to control inputs and outputs
or to note inputs.

```c#
NVariable CreateVariable("name", value, min, max, Function handler)
```
Create a variable. Used for dynamic things like control, note number, UI color, etc.
- name: For display in the UI.
- value: The initial value.
- min: Minimum value.
- max: Maximum value.
- handler: Callback function when value changes.

```c#
double NVariable.Value
```
Used to get or set the actual value stored in the variable. Setting it triggers the handler function.

## Input
These interface to control I/O, UI widgets, etc. They are bound to a NVariable.

```c#
void CreateController(device, channelNum, controlId, boundVar)
```
Create a controller input. Note that VkeyIn (Virtual Keyboard) always uses channelNum of 1.
- device: DeviceType.MidiIn or DeviceType.OscIn or DeviceType.VkeyIn.
- channelNum: Associated channel. Uses midi 1-16.
- controlId: The controller id - integer or value from [ScriptDefinitions](ScriptDefinitions). It can also be
  `NoteControl` to use keyboard notes as a controller. The value will then be positive for note on and
  negative for note off.
- boundVar: Defined NVariable.

```c#
void CreateLever(boundVar)
```
Create a lever. Levers are UI controls that can control variables.
- boundVar: Defined NVariable.

```c#
void CreateDisplay(boundVar, type)
```
Create a meter. Meters are UI controls that display values of variables.
- boundVar: Defined NVariable.
- type: One of Linear, Log, Chart.

## Output

```c#
NChannel CreateChannel("name", device, channelNum, volWobble)
```
An NChannel is essentially an output comm channel.
- name: For display in the UI.
- device: DeviceType.MidiOut or DeviceType.OscOut.
- channelNum: Channel number to play on.
- volWobble: Optional value to add some randomization.

## Sequences and Sections
A composition is comprised of NSections each of which has one or more NSequences.
You first create your sequences like this:
```c#
NSequence CreateSequence(beats, elements[]);
```
Create a sequence of notes.
- beats: Overall length in beats.
- elements: 1 to N descriptors of what to play when. This can take several forms:
    - { when, which, volume, dur }
    - { when, drum, volume }
    - { when, function, volume }
    - { pattern, subdivs, which, volume, dur }
    - { pattern, subdivs, drum, volume }
        - when: When to play the element in the sequence, in Beat.Subdiv format.
        - which/drum: One of [Notes](#notes).
        - volume: Note volume. Normalized to 0.0 - 1.0.
        - dur: Optional duration in Beat.Subdiv format e.g. 2.2. Drums and functions don't use duration.
        - function: Name of a defined function - executed at when.
        - pattern: describes a sequence of notes, kind of like a piano roll. `1 to 9` (volume) starts a note which is held 
          for subsequent `-`. The note is ended with any other character than `-`. `|`, `.` and ` ` are ignored, 
          used for visual assist only. These are particularly useful for drum patterns.
        - subdivs: Subdivisions per beat.
        
A nonsensical example:
```c#
NSequence seqKeysChorus = CreateSequence(2, new NSequenceElements
{
    { 0.0, "F4",  0.7,  0.2 },
    { "|7--     7--     |7--     7--     |", "G4.m7", KEYS_VOL, 1.2 },
    { 1.0,  RideCymbal1,  DRUM_VOL },
    { 1.2,  "B4.m7",  0.7,  0.3 },
    { 1.3, MyAlgoFunc, 0.8 },
});
```

Then you group sequences into sections, typically things live verse, chorus, bridge, etc.
```c#
void CreateSection(beats, name, elements[]);
```
Create a defined section.
- beats: Overall length in beats.
- name: Displayed in time control while playing.
- elements: 1 to N descriptors of which sequences to play - { channel, mode, sequences[] }
    - channel: pertinent channel
    - mode: Once or Loop for the measure
    - sequences: 1 to N sequences to play sequentially

An example:
```c#
CreateSection(16, "Middle", new NSectionElements
{
    { chKeys,  Loop,  seqKeysChorus },
    { chDrums, Loop,  seqDrumsVerse },
    { chBass,  Loop,  seqBassVerse  },
    { chSynth, Once,  seqAlgo,  seqEmpty, seqDynamic, seqEmpty }
});
```

## Script Functions
Call these from inside your script.

```c#
void SendNote(channel, note, vol, dur)
```
Send a note immediately. Respects solo/mute. Adds a note off to play after dur time.
- channel: Channel to send it on - from a NChannel definition.
- note: One of [Notes](#notes). Named notes and chords need quotes.
- vol: Note volume. Normalized to 0.0 - 1.0. 0.0 means note off.
- dur: How long it lasts in Beat.Subdiv or Time object representation.

```c#
void SendNoteOn(channel, note, vol)
```
Send a note on immediately. Respects solo/mute. Doesn't add note off.
- channel: NChannel to send it on - from a channel definition.
- note: Numbered note. Floating point to support OSC.
- vol: Note volume. Normalized to 0.0 - 1.0.

```c#
void SendNoteOff(channel, note)
```
Send a note off immediately.
- channel: NChannel to send it on - from a channel definition.
- note: Numbered note. Floating point to support OSC.

```c#
void SendSequence(channel, seq) 
```
Send a sequence immediately.
- channel: NChannel to send it on - from a Channel definition.
- seq: Which defined NSequence to send.

```c#
void SendController(channel, ctl, val)
```
Send a controller immediately. Useful for things like panning and bank select.
- channel: NChannel to send it on - from a channel definition.
- ctl: Controller from [Script Definitions](ScriptDefinitions) or const() or simple integer.
- val: Controller value.

```c#
void SendPatch(channel, patch)
```
Send a midi patch immediately. Really only needed if using the windows GM.
- channel: NChannel to send it on - from a channel definition.
- patch: Instrument from [Script Definitions](ScriptDefinitions) or const() or simple integer.

Windows GM drum kits:
- Standard = 0
- Room = 8
- Power = 16
- Electronic = 24
- TR-808 = 25
- Jazz = 32
- Brush = 40
- Orchestra = 48
- SFX = 56

```c#
void CreateNotes("name", "parts")
```
Define a group of notes for use as a note, or in a chord or scale.
- name: Reference name.
- note: List of [notes](#notes).

```c#
double[] GetChordNotes("note")
```
Convert the argument into numbered notes.
- note: One of [Notes](#notes).
- returns: Array of notes or empty if invalid.

```c#
double[] GetScaleNotes("scale", "key")
```
Get an array of scale notes.
- scale: One of the named scales from ScriptDefinitions.md or defined in `notes`.
- key: Note name and octave.
- returns: Array of notes or empty if invalid.

## Debugging
```c#
void Print(params object[] args)
```
