
# Music Definitions

Definitions for use in Nebulator script functions.

## Chords
These are the built-in chords. You can add your own in your script.

Chord   | Notes             | Description
------- | ----------------- | -----------
M       | 1 3 5             | Named after the major 3rd interval between root and 3.
m       | 1 b3 5            | Named after the minor 3rd interval between root and b3.
7       | 1 3 5 b7          | Also called dominant 7th.
M7      | 1 3 5 7           | Named after the major 7th interval between root and 7th major scale note.
m7      | 1 b3 5 b7         |
6       | 1 3 5 6           | Major chord with 6th major scale note added.
m6      | 1 b3 5 6          | Minor chord with 6th major scale note added.
o       | 1 b3 b5           | Diminished.
o7      | 1 b3 b5 bb7       | Diminished added 7.
m7b5    | 1 b3 b5 b7        | Also called minor 7b5.
\+      | 1 3 #5            | Augmented.
7#5     | 1 3 #5 b7         |
9       | 1 3 5 b7 9        |
7#9     | 1 3 5 b7 #9       | The 'Hendrix' chord.
M9      | 1 3 5 7 9         |
Madd9   | 1 3 5 9           | Chords extended beyond the octave are called added when the 7th is not present.
m9      | 1 b3 5 b7 9       |
madd9   | 1 b3 5 9          |
11      | 1 3 5 b7 9 11     | The 3rd is often omitted to avoid a clash with the 11th.
m11     | 1 b3 5 b7 9 11    |
7#11    | 1 3 5 b7 #11      | Often used in preference to 11th chords to avoid the dissonant clash between 11 and 3 .
M7#11   | 1 3 5 7 9 #11     |
13      | 1 3 5 b7 9 11 13  | The 11th is often omitted to avoid a clash with the 3rd.
M13     | 1 3 5 7 9 11 13   | The 11th is often omitted to avoid a clash with the 3rd.
m13     | 1 b3 5 b7 9 11 13 |
sus4    | 1 4 5             |
sus2    | 1 2 5             | Sometimes considered as an inverted sus4 (GCD).
5       | 1 5               | Power chord.

## Scales
These are the built-in scales. You can add your own in your script.

Scale                         | Notes                        | Description                              | Lower tetrachord  | Upper tetrachord
----------------------------- | ---------------------------- | ---------------------------------------- | ----------------  | ------------------------
Acoustic                      | 1 2 3 #4 5 6 b7              | Acoustic scale                           | whole tone        | minor
Aeolian                       | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian
NaturalMinor                  | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian
Algerian                      | 1 2 b3 #4 5 b6 7             | Algerian scale                           |                   |
Altered                       | 1 b2 b3 b4 b5 b6 b7          | Altered scale                            | diminished        | whole tone
Augmented                     | 1 b3 3 5 #5 7                | Augmented scale                          |
Bebop                         | 1 2 3 4 5 6 b7 7             | Bebop dominant scale                     |                   |
Blues                         | 1 b3 4 b5 5 b7               | Blues scale                              |                   |
Chromatic                     | 1 #1 2 #2 3 4 #4 5 #5 6 #6 7 | Chromatic scale                          |                   |
Dorian                        | 1 2 b3 4 5 6 b7              | Dorian mode                              | minor             | minor
DoubleHarmonic                | 1 b2 3 4 5 b6 7              | Double harmonic scale                    | harmonic          | harmonic
Enigmatic                     | 1 b2 3 #4 #5 #6 7            | Enigmatic scale                          |                   |
Flamenco                      | 1 b2 3 4 5 b6 7              | Flamenco mode                            | Phrygian          | Phrygian
Gypsy                         | 1 2 b3 #4 5 b6 b7            | Gypsy scale                              | Gypsy             | Phrygian
HalfDiminished                | 1 2 b3 4 b5 b6 b7            | Half diminished scale                    | minor             | whole tone
HarmonicMajor                 | 1 2 3 4 5 b6 7               | Harmonic major scale                     | major             | harmonic
HarmonicMinor                 | 1 2 b3 4 5 b6 7              | Harmonic minor scale                     | minor             | harmonic
Hirajoshi                     | 1 3 #4 5 7                   | Hirajoshi scale                          |
HungarianGypsy                | 1 2 b3 #4 5 b6 7             | Hungarian Gypsy scale                    | Gypsy             | harmonic
HungarianMinor                | 1 2 b3 #4 5 b6 7             | Hungarian minor scale                    | Gypsy             | harmonic
In                            | 1 b2 4 5 b6                  | In scale                                 |                   |
Insen                         | 1 b2 4 5 b7                  | Insen scale                              |                   |
Ionian                        | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major
Istrian                       | 1 b2 b3 b4 b5 5              | Istrian scale                            |                   |
Iwato                         | 1 b2 4 b5 b7                 | Iwato scale                              |                   |
Locrian                       | 1 b2 b3 4 b5 b6 b7           | Locrian mode                             | Phrygian          | whole tone
LydianAugmented               | 1 2 3 #4 #5 6 7              | Lydian augmented scale                   | whole tone        | diminished
Lydian                        | 1 2 3 #4 5 6 7               | Lydian mode                              | whole tone        | major
Major                         | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major
MajorBebop                    | 1 2 3 4 5 #5 6 7             | Major bebop scale                        |                   |
MajorLocrian                  | 1 2 3 4 b5 b6 b7             | Major Locrian scale                      | major             | whole tone
MajorPentatonic               | 1 2 3 5 6                    | Major pentatonic scale                   |                   |
MelodicMinorAscending         | 1 2 b3 4 5 6 7               | Melodic minor scale (ascending)          | minor             | varies
MelodicMinorDescending        | 1 2 b3 4 5 b6 b7 8           | Melodic minor scale (descending)         | minor             | major
MinorPentatonic               | 1 b3 4 5 b7                  | Minor pentatonic scale                   |                   |
Mixolydian                    | 1 2 3 4 5 6 b7               | Mixolydian mode or Adonai malakh mode    | major             | minor
NeapolitanMajor               | 1 b2 b3 4 5 6 7              | Neapolitan major scale                   | Phrygian          | major
NeapolitanMinor               | 1 b2 b3 4 5 b6 7             | Neapolitan minor scale                   | Phrygian          | harmonic
Octatonic                     | 1 2 b3 4 b5 b6 6 7           | Octatonic scale (or 1 b2 b3 3 #4 5 6 b7) |                   |
Persian                       | 1 b2 3 4 b5 b6 7             | Persian scale                            | harmonic          | unusual
PhrygianDominant              | 1 b2 3 4 5 b6 b7             | Phrygian dominant scale                  | harmonic          | Phrygian
Phrygian                      | 1 b2 b3 4 5 b6 b7            | Phrygian mode                            | Phrygian          | Phrygian
Prometheus                    | 1 2 3 #4 6 b7                | Prometheus scale                         |                   |
Tritone                       | 1 b2 3 b5 5 b7               | Tritone scale                            |                   |
UkrainianDorian               | 1 2 b3 #4 5 6 b7             | Ukrainian Dorian scale                   | Gypsy             | minor
WholeTone                     | 1 2 3 #4 #5 #6               | Whole tone scale                         |                   |
Yo                            | 1 b3 4 5 b7                  | Yo scale                                 |                   |

## General Midi Instruments

Instrument          | Number
----------          | ------
AcousticGrandPiano  |  0
BrightAcousticPiano |  1
ElectricGrandPiano  |  2
HonkyTonkPiano      |  3
ElectricPiano1      |  4
ElectricPiano2      |  5
Harpsichord         |  6
Clavinet            |  7
Celesta             |  8
Glockenspiel        |  9
MusicBox            | 10
Vibraphone          | 11
Marimba             | 12
Xylophone           | 13
TubularBells        | 14
Dulcimer            | 15
DrawbarOrgan        | 16
PercussiveOrgan     | 17
RockOrgan           | 18
ChurchOrgan         | 19
ReedOrgan           | 20
Accordion           | 21
Harmonica           | 22
TangoAccordion      | 23
AcousticGuitarNylon | 24
AcousticGuitarSteel | 25
ElectricGuitarJazz  | 26
ElectricGuitarClean | 27
ElectricGuitarMuted | 28
OverdrivenGuitar    | 29
DistortionGuitar    | 30
GuitarHarmonics     | 31
AcousticBass        | 32
ElectricBassFinger  | 33
ElectricBassPick    | 34
FretlessBass        | 35
SlapBass1           | 36
SlapBass2           | 37
SynthBass1          | 38
SynthBass2          | 39
Violin              | 40
Viola               | 41
Cello               | 42
Contrabass          | 43
TremoloStrings      | 44
PizzicatoStrings    | 45
OrchestralHarp      | 46
Timpani             | 47
StringEnsemble1     | 48
StringEnsemble2     | 49
SynthStrings1       | 50
SynthStrings2       | 51
ChoirAahs           | 52
VoiceOohs           | 53
SynthVoice          | 54
OrchestraHit        | 55
Trumpet             | 56
Trombone            | 57
Tuba                | 58
MutedTrumpet        | 59
FrenchHorn          | 60
BrassSection        | 61
SynthBrass1         | 62
SynthBrass2         | 63
SopranoSax          | 64
AltoSax             | 65
TenorSax            | 66
BaritoneSax         | 67
Oboe                | 68
EnglishHorn         | 69
Bassoon             | 70
Clarinet            | 71
Piccolo             | 72
Flute               | 73
Recorder            | 74
PanFlute            | 75
BlownBottle         | 76
Shakuhachi          | 77
Whistle             | 78
Ocarina             | 79
Lead1Square         | 80
Lead2Sawtooth       | 81
Lead3Calliope       | 82
Lead4Chiff          | 83
Lead5Charang        | 84
Lead6Voice          | 85
Lead7Fifths         | 86
Lead8BassAndLead    | 87
Pad1NewAge          | 88
Pad2Warm            | 89
Pad3Polysynth       | 90
Pad4Choir           | 91
Pad5Bowed           | 92
Pad6Metallic        | 93
Pad7Halo            | 94
Pad8Sweep           | 95
Fx1Rain             | 96
Fx2Soundtrack       | 97
Fx3Crystal          | 98
Fx4Atmosphere       | 99
Fx5Brightness       | 100
Fx6Goblins          | 101
Fx7Echoes           | 102
Fx8SciFi            | 103
Sitar               | 104
Banjo               | 105
Shamisen            | 106
Koto                | 107
Kalimba             | 108
BagPipe             | 109
Fiddle              | 110
Shanai              | 111
TinkleBell          | 112
Agogo               | 113
SteelDrums          | 114
Woodblock           | 115
TaikoDrum           | 116
MelodicTom          | 117
SynthDrum           | 118
ReverseCymbal       | 119
GuitarFretNoise     | 120
BreathNoise         | 121
Seashore            | 122
BirdTweet           | 123
TelephoneRing       | 124
Helicopter          | 125
Applause            | 126
Gunshot             | 127

## General Midi Drums

Drum                | Number
----                | ------
AcousticBassDrum    | 35
BassDrum1           | 36
SideStick           | 37
AcousticSnare       | 38
HandClap            | 39
ElectricSnare       | 40
LowFloorTom         | 41
ClosedHiHat         | 42
HighFloorTom        | 43
PedalHiHat          | 44
LowTom              | 45
OpenHiHat           | 46
LowMidTom           | 47
HiMidTom            | 48
CrashCymbal1        | 49
HighTom             | 50
RideCymbal1         | 51
ChineseCymbal       | 52
RideBell            | 53
Tambourine          | 54
SplashCymbal        | 55
Cowbell             | 56
CrashCymbal2        | 57
Vibraslap           | 58
RideCymbal2         | 59
HiBongo             | 60
LowBongo            | 61
MuteHiConga         | 62
OpenHiConga         | 63
LowConga            | 64
HighTimbale         | 65
LowTimbale          | 66
HighAgogo           | 67
LowAgogo            | 68
Cabasa              | 69
Maracas             | 70
ShortWhistle        | 71
LongWhistle         | 72
ShortGuiro          | 73
LongGuiro           | 74
Claves              | 75
HiWoodBlock         | 76
LowWoodBlock        | 77
MuteCuica           | 78
OpenCuica           | 79
MuteTriangle        | 80
OpenTriangle        | 81

## Midi Controllers
- http://www.nortonmusic.com/midi_cc.html
- Undefined MIDI CCs: 3, 9, 14-15, 20-31, 85-90, 102-119
- For most controllers marked on/off, on=127 and off=0

Controller          | Number | Notes
----------          | ------ | -----
BankSelect          | 0      | MSB Followed by BankSelectLSB and Program Change
Modulation          | 1      |
BreathController    | 2      |
FootController      | 4      | MSB
PortamentoTime      | 5      | MSB Only use this for portamento time use 65 to turn on/off
Volume              | 7      | 7 and 11 both adjust the volume. Use 7 as your main control, set and forget
Balance             | 8      | MSB Some synths use it
Pan                 | 10     | MSB
Expression          | 11     | MSB See 7 - use 11 for volume changes during the track (crescendo, diminuendo, swells, etc.)
BankSelectLSB       | 32     | LSB
ModulationLSB       | 33     | LSB
BreathControllerLSB | 34     | LSB
FootControllerLSB   | 36     | LSB
PortamentoTimeLSB   | 37     | LSB
VolumeLSB           | 39     | LSB
BalanceLSB          | 40     | LSB
PanLSB              | 42     | LSB
ExpressionLSB       | 43     | LSB
Sustain             | 64     | Hold Pedal on/off
Portamento          | 65     | on/off
Sostenuto           | 66     | on/off
SoftPedal           | 67     | on/off
Legato              | 68     | on/off
Sustain2            | 69     | Hold Pedal 2 on/off
PortamentoControl   | 84     |
AllSoundOff         | 120    |
ResetAllControllers | 121    |
LocalKeyboard       | 122    |
AllNotesOff         | 123    |
NoteControl         | 250    | Special for internal use
PitchControl        | 251    | Special for internal use


## GM Drum Kits

Note that these will vary depending on your SF file.

Kit        | Number
-----------| ------
Standard   | 0
Room       | 8
Power      | 16
Electronic | 24
TR808      | 25
Jazz       | 32
Brush      | 40
Orchestra  | 48
SFX        | 56
