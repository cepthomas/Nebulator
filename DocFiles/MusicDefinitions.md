
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

