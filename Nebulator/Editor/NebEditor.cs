using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using NLog;



namespace Nebulator.Editor
{
    public partial class NebEditor : UserControl
    {
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>The editor control.</summary> 
        TextEditorControl _editor = new TextEditorControl();

        /// <summary>Holds the settings whether to show line numbers, etc.</summary> 
        ITextEditorProperties _editorSettings;

        /// <summary>Create this now.</summary>
        Finder _finder = new Finder();

        /// <summary>For compiler errors.</summary>
        string _fn = Globals.UNKNOWN_STRING;

        /// <summary>Edited flag.</summary> 
        public bool Dirty { get; set; } = false;

        /// <summary>The edit text.</summary> 
        public string TextContent { get { return _editor.Document.TextContent; } set { _editor.Document.TextContent = value; } }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NebEditor()
        {
            Controls.Add(_editor);
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the controls.
        /// </summary>
        private void NebEditor_Load(object sender, EventArgs e)
        {
            _editor.Dock = DockStyle.Fill;
            _editor.IsReadOnly = false;
            _editor.Visible = true;
            _editor.Document.DocumentChanged += new DocumentEventHandler((ds, de) => { Dirty = true; });
                
            // Editor settings.
            _editorSettings = _editor.TextEditorProperties;
            // I wrote this app - I decide how to handle tabs.
            _editorSettings.ConvertTabsToSpaces = true;
            _editorSettings.TabIndent = 4;
            _editorSettings.ShowVerticalRuler = false;
            //_editorSettings.ShowSpaces = true;
            _editorSettings.LineViewerStyle = LineViewerStyle.FullRow;
            _editorSettings.AllowCaretBeyondEOL = true;
            _editorSettings.ShowLineNumbers = true;
            _editorSettings.Font = Globals.UserSettings.EditorFont;
            _editorSettings.IsIconBarVisible = true;
            // Set the folding strategy.
            _editor.Document.FoldingManager.FoldingStrategy = new NebFoldingStrategy();

            // Other props:
            // AutoInsertCurlyBracket, CaretLine, CutCopyWholeLine, EnableFolding, 
            // HideMouseCursor, MouseWheelScrollDown, MouseWheelTextZoom, ShowEOLMarker, ShowHorizontalRuler, 
            // ShowInvalidLines, ShowMatchingBracket, ShowSpaces, ShowTabs, SupportReadOnlySegments, 
            // BracketMatchingStyle, DocumentSelectionMode, Encoding, IndentStyle, IndentationSize, VerticalRulerRow, 
            // LineTerminator, TextRenderingHint

            // Set the menu.
            //cmsMergeView_Opening(null, null);
            //editor.ContextMenuStrip = cmsMergeView;

            _editor.ActiveTextAreaControl.TextArea.MouseClick += TextArea_MouseClick;
            //_editor.ActiveTextAreaControl.TextArea.MouseHover += TextArea_MouseHover;

            //TODO2 _findForm.ShowFor(_editor, false);
        }

        /// <summary>
        /// Initialize from file contents.
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="fn"></param>
        public void Init(IEnumerable<string> lines, string fn)
        {
            _fn = fn;
            TextContent = string.Join(Environment.NewLine, lines);

            // Update the foldings.
            _editor.Document.FoldingManager.UpdateFoldings(null, null);

            // Set the special highlighting. TODO2 this mangles the text - needs debug.
            //_editor.Document.HighlightingStrategy = new NebHighlightingStrategy();
            _editor.Refresh();

            Dirty = false;
        }

        ///// <summary>
        ///// Compile the source file into a Composition.
        ///// </summary>
        ///// <returns>The composition or null if failed.</returns>
        //public Compiler Compile()
        //{
        //    Compiler compiler = new Compiler();
        //    List<string> lines = _editor.Document.TextContent.SplitByToken(Environment.NewLine, false);
        //    compiler.Execute(lines, _fn);

        //    if(compiler.Errors.Count > 0)
        //    {
        //        // TODO2 display them in editor.
        //        List<string> errors = new List<string>();
        //        compiler.Errors.ForEach(e => errors.Add(e.ToString()));
        //        using (TextViewer lv = new TextViewer() { Lines = errors, Text = "Compiler Errors" })
        //        {
        //            lv.ShowDialog();
        //        }
        //    }

        //    return compiler;
        //}

        #region TODO2 add in all/some of these features:
        /// <summary>Standard command/action.</summary>
        private void btnSplit_Click(object sender, EventArgs e)
        {
            _editor.Split();
        }

        /// <summary>Helper to determine selection area.</summary>
        /// <returns>Has a selection.</returns>
        private bool HaveSelection()
        {
            return _editor.ActiveTextAreaControl.TextArea.SelectionManager.HasSomethingSelected;
        }

        /// <summary>Standard command/action.</summary>
        private void menuEditFind_Click(object sender, EventArgs e)
        {
            _finder.ShowFor(_editor, false);
        }

        /// <summary>Standard command/action.</summary>
        private void menuEditReplace_Click(object sender, EventArgs e)
        {
            _finder.ShowFor(_editor, true);
        }

        /// <summary>Standard command/action.</summary>
        private void menuFindAgain_Click(object sender, EventArgs e)
        {
            _finder.FindNext(true, false, $"Search text \"{_finder.LookFor}\" not found.");
        }

        /// <summary>Standard command/action.</summary>
        private void menuFindAgainReverse_Click(object sender, EventArgs e)
        {
            _finder.FindNext(true, true, $"Search text \"{_finder.LookFor}\" not found.");
        }

        /// <summary>Standard command/action.</summary>
        private void menuToggleBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(_editor, new ICSharpCode.TextEditor.Actions.ToggleBookmark());
            _editor.IsIconBarVisible = _editor.Document.BookmarkManager.Marks.Count > 0;
        }

        /// <summary>Standard command/action.</summary>
        private void menuGoToNextBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(_editor, new ICSharpCode.TextEditor.Actions.GotoNextBookmark(bookmark => true));
        }

        /// <summary>Standard command/action.</summary>
        private void menuGoToPrevBookmark_Click(object sender, EventArgs e)
        {
            DoEditAction(_editor, new ICSharpCode.TextEditor.Actions.GotoPrevBookmark(bookmark => true));
        }
        #endregion

        /// <summary>Performs an action encapsulated in IEditAction.</summary>
        /// <remarks>
        /// There is an implementation of IEditAction for every action that 
        /// the user can invoke using a shortcut key (arrow keys, Ctrl+X, etc.)
        /// The editor control doesn't provide a public functiiton to perform one
        /// of these actions directly, so I wrote DoEditAction() based on the
        /// code in TextArea.ExecuteDialogKey(). You can call ExecuteDialogKey
        /// directly, but it is more fragile because it takes a Keys value (e.g.
        /// Keys.Left) instead of the action to perform.
        /// Clipboard commands could also be done by calling methods in
        /// editor.ActiveTextAreaControl.TextArea.ClipboardHandler.
        /// </remarks>
        private void DoEditAction(TextEditorControl editor, ICSharpCode.TextEditor.Actions.IEditAction action)
        {
            if (editor != null && action != null)
            {
                var area = editor.ActiveTextAreaControl.TextArea;
                editor.BeginUpdate();
                try
                {
                    lock (editor.Document)
                    {
                        action.Execute(area);
                        if (area.SelectionManager.HasSomethingSelected && area.AutoClearSelection /*&& caretchanged*/)
                        {
                            if (area.Document.TextEditorProperties.DocumentSelectionMode == DocumentSelectionMode.Normal)
                            {
                                area.SelectionManager.ClearSelection();
                            }
                        }
                    }
                }
                finally
                {
                    editor.EndUpdate();
                    area.Caret.UpdateCaretPosition();
                }
            }
        }

        /// <summary>Editor area click.</summary>
        void TextArea_MouseClick(object sender, MouseEventArgs e)
        {
            int lineNumber = _editor.ActiveTextAreaControl.Caret.Line;
            LineSegment seg = _editor.Document.GetLineSegment(lineNumber);
            lblInfo.Text = seg.Words.Count > 0 ? seg.Words[0].Word : ""; //TODO3 some help would be nice here.
        }

        #region Folding Strategies
        /// <summary>The class to generate the foldings.</summary>
        public class NebFoldingStrategy : IFoldingStrategy
        {
            /// <summary>Generates the foldings for our document.</summary>
            /// <param name="document">The current document.</param>
            /// <param name="fileName">The filename of the document.</param>
            /// <param name="parseInformation">Extra parse information - not used here.</param>
            /// <returns>A list of FoldMarkers.</returns>
            public List<FoldMarker> GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
            {
                // Create foldmarkers for the whole document, enumerate through every line.
                List<FoldMarker> list = new List<FoldMarker>();
                int startLine = -1;
                int endCol = 0;
                string foldText = "";

                for (int i = 0; i < document.TotalNumberOfLines; i++)
                {
                    var seg = document.GetLineSegment(i);
                    string s = document.GetText(seg);

                    bool foldStart = s.StartsWith("sequence") || s.StartsWith("track");
                    bool foldCont = s.StartsWith("note") || s.StartsWith("loop") || s.StartsWith("//");

                    if(startLine != -1) // in a fold
                    {
                        if(foldCont) // still gathering
                        {
                            endCol = Math.Max(endCol, seg.Length);
                        }
                        else // finished fold
                        {
                            FoldMarker mk = new FoldMarker(document, startLine, 0, i - 1, endCol, FoldType.Region, foldText, true);
                            list.Add(mk);
                            startLine = -1; // reset flag
                            endCol = 0;
                        }
                    }

                    if (foldStart)
                    {
                        startLine = i;
                        endCol = seg.Length;
                        foldText = s;
                    }
                }

                // Last fold?
                if (startLine != -1) // in a fold
                {
                    FoldMarker mk = new FoldMarker(document, startLine, 0, document.TotalNumberOfLines, endCol, FoldType.Region, foldText, true);
                    list.Add(mk);
                }

                return list;
            }
        }
        #endregion

        #region Highlighting Strategies
        /// <summary>A highlighting strategy for neb files.</summary>
        class NebHighlightingStrategy : IHighlightingStrategy
        {
            #region IHighlightingStrategy Properties
            public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();
            public string Name { get { return "NebHighlighting"; } }
            public string[] Extensions { get { return new string[] { ".neb" }; } }
            #endregion

            #region Fields
            DefaultHighlightingStrategy _defaultHighlightingStrategy = new DefaultHighlightingStrategy();
            HighlightColor _hcDef = new HighlightColor(Color.Black, false, false);
            HighlightColor _hcMark = new HighlightColor(Color.Red, Color.Aqua, true, true); //TODO2 make user setting?
            #endregion

            #region IHighlightingStrategy Methods
            public HighlightColor GetColorFor(string name)
            {
                //Console.WriteLine("====" + name); >>>
                //CaretMarker, Default, FoldLine, FoldMarker, LineNumbers, SelectedFoldLine, Selection, SpaceMarkers, TabMarkers

                //switch (name)
                //{
                //    case "Selection":
                //        return new HighlightColor(Color.Black, Color.AliceBlue, false, false);
                //        break;
                //    default:
                //        return _defaultHighlightingStrategy.GetColorFor(name);
                //        break;
                //}

                return _defaultHighlightingStrategy.GetColorFor(name);
            }

            public void MarkTokens(IDocument document, List<LineSegment> lines)
            {
                for (int lineNum = 0; lineNum < lines.Count; lineNum++)
                {
                    LineSegment line = lines[lineNum];
                    //string s = document.GetText(line.Offset, line.Length);
                    HashSet<string> kwds = new HashSet<string>() { "project:", "var:", "noteseq:", "notetrack:" };

                    foreach (TextWord tw in line.Words)
                    {
                        if (kwds.Contains(tw.Word))
                        {
                            tw.SyntaxColor = _hcMark;
                        }
                    }

                    // Tell the text editor to redraw the changed line.
                    document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, line.LineNumber));
                }
            }

            public void MarkTokens(IDocument document)
            {
                MarkTokens(document, new List<LineSegment>(document.LineSegmentCollection));
            }
            #endregion
        }
        #endregion
    }
}
