using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Nebulator.Common;
using Nebulator.Midi;


/*** TODO1 graphics faster: - try drawRecursive() in script. SharpDx? Separate thread?

Basically you would create a game loop. Here you handle user inputs, perform game logic, and render a new frame:

Sub GameLoop()
  Do
    HandeUserInput() 'Handles keys or mouse movement
    PerformGameLogic() 'Move NPCs, etc.
    RenderNewFrame() 'Redraw the new state of the game
  Loop
End Sub

Userinput and game logic are up to you. Rendering will be done by creating a new bitmap in the size you want, and then use a 
GDI+ graphics object to draw stuff on it. Then you show this new bitmap in the picturebox once it is done.

Sub RenderNewFrame()
  Dim NewFrame as New Bitmap(640, 480) 'Or whatever resolution you want
  Using g as Graphics = Graphics.FromImage(NewFrame)
    DrawWorld(g)
    DrawPlayer(g)
  End Using
  If Picturebox1.Image IsNot Nothing Then Picturebox1.Image.Dispose()
  Picturebox1.Image = NewFrame
End Sub

I dispose the previous frame, since the bitmaps are not managed and would pile up in memory quickly.

This is the basic framework for a game and works actually quite nicely in VB.NET with GDI+. You may want to add small delays 
between each frame, 1..2 ms. This will prevent much of the processor load while not really affecting the game performace.

You don't really need a PictureBox, you can blit your buffer directly to the form. But I strongly recommend doing this in 
response to a Paint event and just invalidating the screen when you need to redraw.

This is fundamentally wrong. The Paint event passes e.Graphics to let you draw whatever you want to paint. When you turn on 
double-buffering, e.Graphics refers to a bitmap, it is initialized with the BackColor. You then proceed to drawing using 
another Graphics object you got from CreateGraphics(). That one draws directly to the screen.

The flicker effect you see if very pronounced. For a split second you see what the other Graphics context draws. Then your
panelDraw_Paint() method returns and Winforms draws the double-buffered bitmap. There's nothing on it so it immediately erases what you drew.

Modify the redrawDrawingPanel() method and give it an argument of type Graphics. Pass e.Graphics in the call. And only use 
that Graphics object, remove all calls to CreateGraphics()

Set ResizeRedraw = true in the constructor so a paint is triggered when the panel resizes. Use Invalidate() when you know something changed. 

***/



// TODO1 all this algo stuff:
//
// Generative Music becomes Reflective Music when your text can be used as a seed for how it starts.
// http://spheric-lounge-live-ambient-music.blogspot.com/
//
//The random item stream pattern type uses the optional keyword :weight to alter the probability 
//of an event being selected in relation to the other events in the item stream.
//need this, for scale note selections.
//
//scaleMaj = 0, 2, 4, 5, 7, 9, 11
//
//The graph item stream pattern type creates an item stream of user-specified rules for traversing a series 
//of nodes called a graph.
// my loops - rules to change/transition.
//
// Sonification uses data that is typically not musical and involves remapping this to musical parameters 
//to create a composition.



namespace Nebulator.Scripting
{
    /// <summary>
    /// Core functions of script. User scripts inherit from this class.
    /// </summary>
    public partial class Script
    {
        #region Properties
        /// <summary>The client hosts this control in their UI. It performs the actual graphics drawing and input. </summary>
        public UserControl Surface { get; private set; } = null;

        /// <summary>Variables, controls, etc.</summary>
        public Dynamic Dynamic { get; set; } = new Dynamic(); // TODO1 refactor?

        /// <summary>Steps added at runtime.</summary>
        public StepCollection ScriptSteps { get; private set; } = new StepCollection();
        #endregion

        #region Events
        /// <summary>
        /// Interaction with the host. TODO1 A bit klunky, could be improved. Separate events for each?
        /// </summary>
        public class ScriptEventArgs : EventArgs
        {
            /// <summary>If not null, print this.</summary>
            public string Message { get; set; } = null;

            /// <summary>Master speed in bpm. If null means get otherwise set.</summary>
            public double? Speed { get; set; } = null;

            /// <summary>Master volume in midi velocity range: 0 - 127. If null means get otherwise set.</summary>
            public int? Volume { get; set; } = null;

            /// <summary>Script can select UI rate in fps. If null means get otherwise set.</summary>
            public int? FrameRate { get; set; } = null;
        }
        public event EventHandler<ScriptEventArgs> ScriptEvent;
        #endregion

        #region Fields
        /// <summary>Script randomizer.</summary>
        Random _rand = new Random();

        /// <summary>Script functions that are called from the main nebulator. They are identified by name/key. Typically they are controller input handlers such that the key is the name of the input.</summary>
        protected Dictionary<string, ScriptFunction> _scriptFunctions = new Dictionary<string, ScriptFunction>();
        public delegate void ScriptFunction();
        #endregion

        #region Internal overhead
        /// <summary>
        /// Constructor called by derived scripts.
        /// </summary>
        protected Script()
        {
            CreateSurface();
        }

        /// <summary>
        /// Execute a script function. No error checking, presumably the compiler did that.
        /// </summary>
        /// <param name="which"></param>
        public void ExecScriptFunction(string which)
        {
            if(_scriptFunctions.ContainsKey(which))
            {
                _scriptFunctions[which].Invoke();
            }
        }
        #endregion
    }
}
