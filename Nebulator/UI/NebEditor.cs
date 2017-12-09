using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;
using NLog;
using Nebulator.Common;



namespace Nebulator.UI
{
    public partial class NebEditor : UserControl
    {
        /// <summary>App logger.</summary>
        Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>The editor control.</summary> 
        TextEditorControl _editor = new TextEditorControl();

        /// <summary>Holds the settings whether to show line numbers, etc.</summary> 
        ITextEditorProperties _editorSettings;

        ///// <summary>Create this now.</summary>
        //Finder _finder = new Finder();

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
            // Other props:
            // AutoInsertCurlyBracket, CaretLine, CutCopyWholeLine, EnableFolding, 
            // HideMouseCursor, MouseWheelScrollDown, MouseWheelTextZoom, ShowEOLMarker, ShowHorizontalRuler, 
            // ShowInvalidLines, ShowMatchingBracket, ShowSpaces, ShowTabs, SupportReadOnlySegments, 
            // BracketMatchingStyle, DocumentSelectionMode, Encoding, IndentStyle, IndentationSize, VerticalRulerRow, 
            // LineTerminator, TextRenderingHint

            // Set the folding strategy.
            _editor.Document.FoldingManager.FoldingStrategy = new NebFoldingStrategy();

            // Set the special highlighting.
            //NebHighlightingStrategy sst = new NebHighlightingStrategy();
            //sst.TagSyntaxColors = _tagSyntaxColors;
            //sst.BodySyntaxColors = _bodySyntaxColors;
            //_editor.Document.HighlightingStrategy = sst;
            //_editor.Refresh();


            // Set the menu.
            //cmsMergeView_Opening(null, null);
            //editor.ContextMenuStrip = cmsMergeView;

            _editor.ActiveTextAreaControl.TextArea.MouseClick += TextArea_MouseClick;
            //_editor.ActiveTextAreaControl.TextArea.MouseHover += TextArea_MouseHover;

            //TODO2 _findForm.ShowFor(_editor, false);
        }

        /// <summary>
        /// Initialize from file.
        /// </summary>
        /// <param name="fn"></param>
        public void Init(string fn)
        {
            Init(File.ReadAllLines(fn), fn);
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
            _editor.Document.HighlightingStrategy = new NebHighlightingStrategy();
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
            //_finder.ShowFor(_editor, false);
        }

        /// <summary>Standard command/action.</summary>
        private void menuEditReplace_Click(object sender, EventArgs e)
        {
            //_finder.ShowFor(_editor, true);
        }

        /// <summary>Standard command/action.</summary>
        private void menuFindAgain_Click(object sender, EventArgs e)
        {
            //_finder.FindNext(true, false, $"Search text \"{_finder.LookFor}\" not found.");
        }

        /// <summary>Standard command/action.</summary>
        private void menuFindAgainReverse_Click(object sender, EventArgs e)
        {
            //_finder.FindNext(true, true, $"Search text \"{_finder.LookFor}\" not found.");
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
                HashSet<string> kwds = new HashSet<string>() { "include", "constant", "variable", "sequence", "track" };

                for (int lineNum = 0; lineNum < lines.Count; lineNum++)
                {
                    LineSegment line = lines[lineNum];
                    string s = document.GetText(line.Offset, line.Length);
                    if(!s.TrimStart().StartsWith("//"))
                    {
                        foreach (TextWord tw in line.Words)
                        {
                            if (kwds.Contains(tw.Word)) // ignores like constant(123)
                            {
                                tw.SyntaxColor = _hcMark;
                            }
                        }

                        // Tell the text editor to redraw the changed line.
                        document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, line.LineNumber));
                    }
                }
            }

            public void MarkTokens(IDocument document)
            {
                MarkTokens(document, new List<LineSegment>(document.LineSegmentCollection));
            }

            // old DEX
            // public void MarkTokens(IDocument document, List<LineSegment> lines)
            // {
            //     if (TagSyntaxColors == null || BodySyntaxColors == null)
            //     {
            //         // Trying to open a Dex file without a project loaded, should tell the user.
            //     }
            //     else
            //     {
            //         for (int lineNum = 0; lineNum < lines.Count; lineNum++)
            //         {
            //             LineSegment line = lines[lineNum];

            //             // Reset default whitespace tokenizing.
            //             line.Words = new List<TextWord>();

            //             if (line.Length > Parser.POS_CONTENT)
            //             {
            //                 // Tokenize the line into the way we want it.
            //                 //2007-08-07 12:55:13.660 FEMU R:* **FAILED** OP=SaHaConvyIndexLedOn&DEV=SAHA_CONVY_INDEX_LED_DEV&CMD=SET_STATE&STATUS=PASS&

            //                 // Add the datetime as one word with default color.
            //                 line.Words.Add(new TextWord(document, line, 0, Parser.POS_TIME + Parser.LEN_TIME, _hcDef, true)); // date and time
            //                 line.Words.Add(new TextWord(document, line, Parser.POS_TIME + Parser.LEN_TIME, 1, _hcDef, false));

            //                 // Process the tag.
            //                 string tag = document.GetText(line.Offset + Parser.POS_LINE_TYPE, Parser.LEN_LINE_TYPE);

            //                 SyntaxColor sct = TagSyntaxColors.Where(x => x.Pattern == tag).FirstOrDefault() ?? new SyntaxColor();
            //                 HighlightColor hct = new HighlightColor(sct.FgColor, sct.BgColor, sct.Bold, sct.Italic);
            //                 TextWord twTag = new TextWord(document, line, Parser.POS_LINE_TYPE, Parser.LEN_LINE_TYPE, hct, true);
            //                 line.Words.Add(twTag);
            //                 line.Words.Add(new TextWord(document, line, Parser.POS_LINE_TYPE + Parser.LEN_LINE_TYPE, 1, _hcDef, false));

            //                 // Process the body.
            //                 List<TextWord> matchWords = new List<TextWord>();
            //                 List<TextWord> gapWords = new List<TextWord>();

            //                 // Take a copy of the line for with markers to indicate a char has been dealt with.
            //                 char[] chmarked = document.GetText(line.Offset + Parser.POS_CONTENT, line.Length - Parser.POS_CONTENT).ToCharArray();

            //                 // Go through all matches and add to the words list.
            //                 foreach (SyntaxColor sc in BodySyntaxColors)
            //                 {
            //                     foreach (Match m in sc.RegexObj.Matches(document.GetText(line.Offset, line.Length), Parser.POS_CONTENT))
            //                     {

            //                         TextWord tw = new TextWord(document, line, m.Index, m.Length, new HighlightColor(sc.FgColor, sc.BgColor, sc.Bold, sc.Italic), false);
            //                         // Place cookie crumbs to indicate this chunk is done.
            //                         matchWords.Add(tw);
            //                         for (int i = m.Index - Parser.POS_CONTENT; i < m.Index - Parser.POS_CONTENT + m.Length; i++)
            //                         {
            //                             chmarked[i] = '~';
            //                         }
            //                     }
            //                 }

            //                 for (int i = 0; i < chmarked.Length; i++)
            //                 {
            //                     if (chmarked[i] != '~')
            //                     {
            //                         gapWords.Add(new TextWord(document, line, i + Parser.POS_CONTENT, 1, _hcDef, false));
            //                     }
            //                 }

            //                 gapWords.AddRange(matchWords);
            //                 gapWords.Sort((x, y) => x.Offset - y.Offset);
            //                 line.Words.AddRange(gapWords);
            //             }

            //             // Tell the text editor to redraw the changed line.
            //             document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, line.LineNumber));
            //         }
            //     }
            // }

            #endregion
        }
        #endregion
    }
}




//<?xml version="1.0"?>
//<!-- syntaxdefinition for C# 2000 by Mike Krueger -->
//
//<SyntaxDefinition name="C#" folding="CSharp" extensions=".cs">
//	
//	<Properties>
//		<Property name="LineComment" value="//"/>
//	</Properties>
//	
//	<Digits name = "Digits" bold = "false" italic = "false" color = "DarkBlue"/>
//	
//	<RuleSets>
//		<RuleSet ignorecase="false">
//			<Delimiters>&amp;&lt;&gt;~!%^*()-+=|\#/{}[]:;"' ,	.?</Delimiters>
//			
//			<Span name = "PreprocessorDirectives" rule = "PreprocessorSet" bold="false" italic="false" color="Green" stopateol = "true">
//				<Begin>#</Begin>
//			</Span>
//			
//			<Span name = "DocLineComment" rule = "DocCommentSet" bold = "false" italic = "false" color = "Green" stopateol = "true" noescapesequences="true">
//				<Begin bold = "false" italic = "false" color = "Gray">///@!/@</Begin>
//			</Span>
//			
//			<Span name = "LineComment" rule = "CommentMarkerSet" bold = "false" italic = "false" color = "Green" stopateol = "true">
//				<Begin>//@!/@</Begin>
//			</Span>
//			<Span name = "LineComment2" rule = "CommentMarkerSet" bold = "false" italic = "false" color = "Green" stopateol = "true">
//				<Begin>////</Begin>
//			</Span>
//			
//			<Span name = "BlockComment" rule = "CommentMarkerSet" bold = "false" italic = "false" color = "Green" stopateol = "false">
//				<Begin>/*</Begin>
//				<End>*/</End>
//			</Span>
//			
//			<Span name = "String" bold = "false" italic = "false" color = "Blue" stopateol = "true" escapecharacter="\">
//				<Begin>"</Begin>
//				<End>"</End>
//			</Span>
//			
//			<Span name = "MultiLineString" bold = "false" italic = "false" color = "Blue" stopateol = "false" escapecharacter='"'>
//				<Begin>@@"</Begin>
//				<End>"</End>
//			</Span>
//			
//			<Span name = "Char" bold = "false" italic = "false" color = "Magenta" stopateol = "true" escapecharacter="\">
//				<Begin>&apos;</Begin>
//				<End>&apos;</End>
//			</Span>
//			
//			<MarkPrevious bold = "true" italic = "false" color = "MidnightBlue">(</MarkPrevious>
//			
//			<KeyWords name = "Punctuation" bold = "false" italic = "false" color = "DarkGreen">
//				<Key word = "?" />
//				<Key word = "," />
//				<Key word = "." />
//				<Key word = ";" />
//				<Key word = "(" />
//				<Key word = ")" />
//				<Key word = "[" />
//				<Key word = "]" />
//				<Key word = "{" />
//				<Key word = "}" />
//				<Key word = "+" />
//				<Key word = "-" />
//				<Key word = "/" />
//				<Key word = "%" />
//				<Key word = "*" />
//				<Key word = "&lt;" />
//				<Key word = "&gt;" />
//				<Key word = "^" />
//				<Key word = "=" />
//				<Key word = "~" />
//				<Key word = "!" />
//				<Key word = "|" />
//				<Key word = "&amp;" />
//			</KeyWords>
//			
//			<KeyWords name = "AccessKeywords" bold="true" italic="false" color="Black">
//				<Key word = "this" />
//				<Key word = "base" />
//			</KeyWords>
//			
//			<KeyWords name = "OperatorKeywords" bold="true" italic="false" color="DarkCyan">
//				<Key word = "as" />
//				<Key word = "is" />
//				<Key word = "new" />
//				<Key word = "sizeof" />
//				<Key word = "typeof" />
//				<Key word = "true" />
//				<Key word = "false" />
//				<Key word = "stackalloc" />
//			</KeyWords>
//			
//			
//			<KeyWords name = "SelectionStatements" bold="true" italic="false" color="Blue">
//				<Key word = "else" />
//				<Key word = "if" />
//				<Key word = "switch" />
//				<Key word = "case" />
//				<Key word = "default" />
//			</KeyWords>
//			
//			<KeyWords name = "IterationStatements" bold="true" italic="false" color="Blue">
//				<Key word = "do" />
//				<Key word = "for" />
//				<Key word = "foreach" />
//				<Key word = "in" />
//				<Key word = "while" />
//			</KeyWords>
//			
//			<KeyWords name = "JumpStatements" bold="false" italic="false" color="Navy">
//				<Key word = "break" />
//				<Key word = "continue" />
//				<Key word = "goto" />
//				<Key word = "return" />
//			</KeyWords>
//			
//			<KeyWords name = "ContextKeywords" bold="false" italic="false" color="Navy">
//				<Key word = "yield" />
//				<Key word = "partial" />
//				<Key word = "global" />
//				<Key word = "where" />
//				<Key word = "select" />
//				<Key word = "group" />
//				<Key word = "by" />
//				<Key word = "into" />
//				<Key word = "from" />
//				<Key word = "ascending" />
//				<Key word = "descending" />
//				<Key word = "orderby" />
//				<Key word = "let" />
//				<Key word = "join" />
//				<Key word = "on" />
//				<Key word = "equals" />
//				<Key word = "var" />
//			</KeyWords>
//			
//			<KeyWords name = "ExceptionHandlingStatements" bold="true" italic="false" color="Teal">
//				<Key word = "try" />
//				<Key word = "throw" />
//				<Key word = "catch" />
//				<Key word = "finally" />
//			</KeyWords>
//			
//			<KeyWords name = "CheckedUncheckedStatements" bold="true" italic="false" color="DarkGray">
//				<Key word = "checked" />
//				<Key word = "unchecked" />
//			</KeyWords>
//			
//			<KeyWords name = "UnsafeFixedStatements" bold="false" italic="false" color="Olive">
//				<Key word = "fixed" />
//				<Key word = "unsafe" />
//			</KeyWords>
//			
//			<KeyWords name = "ValueTypes" bold="true" italic="false" color="Red">
//				<Key word = "bool" />
//				<Key word = "byte" />
//				<Key word = "char" />
//				<Key word = "decimal" />
//				<Key word = "double" />
//				<Key word = "enum" />
//				<Key word = "float" />
//				<Key word = "int" />
//				<Key word = "long" />
//				<Key word = "sbyte" />
//				<Key word = "short" />
//				<Key word = "struct" />
//				<Key word = "uint" />
//				<Key word = "ushort" />
//				<Key word = "ulong" />
//			</KeyWords>
//			
//			<KeyWords name = "ReferenceTypes" bold="false" italic="false" color="Red">
//				<Key word = "class" />
//				<Key word = "interface" />
//				<Key word = "delegate" />
//				<Key word = "object" />
//				<Key word = "string" />
//			</KeyWords>
//			
//			<KeyWords name = "Void" bold="false" italic="false" color="Red">
//				<Key word = "void" />
//			</KeyWords>
//			
//			<KeyWords name = "ConversionKeyWords" bold="true" italic="false" color="Pink">
//				<Key word = "explicit" />
//				<Key word = "implicit" />
//				<Key word = "operator" />
//			</KeyWords>
//			
//			<KeyWords name = "MethodParameters" bold="true" italic="false" color="DeepPink">
//				<Key word = "params" />
//				<Key word = "ref" />
//				<Key word = "out" />
//			</KeyWords>
//			
//			<KeyWords name = "Modifiers" bold="false" italic="false" color="Brown">
//				<Key word = "abstract" />
//				<Key word = "const" />
//				<Key word = "event" />
//				<Key word = "extern" />
//				<Key word = "override" />
//				<Key word = "readonly" />
//				<Key word = "sealed" />
//				<Key word = "static" />
//				<Key word = "virtual" />
//				<Key word = "volatile" />
//			</KeyWords>
//			
//			<KeyWords name = "AccessModifiers" bold="true" italic="false" color="Blue">
//				<Key word = "public" />
//				<Key word = "protected" />
//				<Key word = "private" />
//				<Key word = "internal" />
//			</KeyWords>
//			
//			<KeyWords name = "NameSpaces" bold="true" italic="false" color="Green">
//				<Key word = "namespace" />
//				<Key word = "using" />
//			</KeyWords>
//			
//			<KeyWords name = "LockKeyWord" bold="false" italic="false" color="DarkViolet">
//				<Key word = "lock" />
//			</KeyWords>
//			
//			<KeyWords name = "GetSet" bold="false" italic="false" color="SaddleBrown">
//				<Key word = "get" />
//				<Key word = "set" />
//				<Key word = "add" />
//				<Key word = "remove" />
//			</KeyWords>
//			
//			<KeyWords name = "Literals" bold="true" italic="false" color="Black">
//				<Key word = "null" />
//				<Key word = "value" />
//			</KeyWords>
//		</RuleSet>
//		
//		<RuleSet name = "CommentMarkerSet" ignorecase = "false">
//			<Delimiters>&lt;&gt;~!@%^*()-+=|\#/{}[]:;"' ,	.?</Delimiters>
//			<KeyWords name = "ErrorWords" bold="true" italic="false" color="Red">
//				<Key word = "TODO" />
//				<Key word = "FIXME" />
//			</KeyWords>
//			<KeyWords name = "WarningWords" bold="true" italic="false" color="#EEE0E000">
//				<Key word = "HACK" />
//				<Key word = "UNDONE" />
//			</KeyWords>
//		</RuleSet>
//		
//		<RuleSet name = "DocCommentSet" ignorecase = "false">
//			<Delimiters>&lt;&gt;~!@%^*()-+=|\#/{}[]:;"' ,	.?</Delimiters>
//			
//			<Span name = "XmlTag" rule = "XmlDocSet" bold = "false" italic = "false" color = "Gray" stopateol = "true">
//				<Begin>&lt;</Begin>
//				<End>&gt;</End>
//			</Span>
//			
//			<KeyWords name = "ErrorWords" bold="true" italic="false" color="Red">
//				<Key word = "TODO" />
//				<Key word = "FIXME" />
//			</KeyWords>
//			
//			<KeyWords name = "WarningWords" bold="true" italic="false" color="#EEE0E000">
//				<Key word = "HACK" />
//				<Key word = "UNDONE" />
//			</KeyWords>
//		</RuleSet>
//		
//		<RuleSet name = "PreprocessorSet" ignorecase="false">
//			<Delimiters>&amp;&lt;&gt;~!%^*()-+=|\#/{}[]:;"' ,	.?</Delimiters>
//			
//			<KeyWords name = "PreprocessorDirectives" bold="true" italic="false" color="Green">
//				<Key word = "if" />
//				<Key word = "else" />
//				<Key word = "elif" />
//				<Key word = "endif" />
//				<Key word = "define" />
//				<Key word = "undef" />
//				<Key word = "warning" />
//				<Key word = "error" />
//				<Key word = "line" />
//				<Key word = "region" />
//				<Key word = "endregion" />
//				<Key word = "pragma" />
//			</KeyWords>
//		</RuleSet>
//		
//		<RuleSet name = "XmlDocSet" ignorecase = "false">
//			<Delimiters>&lt;&gt;~!@%^*()-+=|\#/{}[]:;"' ,	.?</Delimiters>
//			
//			<Span name = "String" bold = "true" italic = "false" color = "Silver" stopateol = "true">
//				<Begin>"</Begin>
//				<End>"</End>
//			</Span>
//			
//			
//			<KeyWords name = "Punctuation" bold = "true" italic = "false" color = "Gray">
//				<Key word = "/" />
//				<Key word = "|" />
//				<Key word = "=" />
//			</KeyWords>
//			
//			<KeyWords name = "SpecialComment" bold="true" italic="false" color="Gray">
//				<Key word = "c" />
//				<Key word = "code" />
//				<Key word = "example" />
//				<Key word = "exception" />
//				<Key word = "list" />
//				<Key word = "para" />
//				<Key word = "param" />
//				<Key word = "paramref" />
//				<Key word = "permission" />
//				<Key word = "remarks" />
//				<Key word = "returns" />
//				<Key word = "see" />
//				<Key word = "seealso" />
//				<Key word = "summary" />
//				<Key word = "value" />
//				<Key word = "inheritdoc" />
//				
//				<Key word = "type" />
//				<Key word = "name" />
//				<Key word = "cref" />
//				<Key word = "item" />
//				<Key word = "term" />
//				<Key word = "description" />
//				<Key word = "listheader" />
//			</KeyWords>
//		</RuleSet>
//	</RuleSets>
//</SyntaxDefinition>
//
//

/*
    /// <summary>
    /// A general purpose editor form for use with the ICSharpCode.TextEditor control.
    /// Original from the ICSharpCode project. Could use better documentation.
    /// </summary>
    public partial class Finder : Form TODO2
    {
        TextToolSearcher _search;

        public bool _lastSearchWasBackward = false;

        public bool _lastSearchLoopedAround;

        Dictionary<TextEditorControl, HighlightGroup> _highlightGroups = new Dictionary<TextEditorControl, HighlightGroup>();

        TextEditorControl _editor;

        public bool ReplaceMode
        {
            get { return txtReplaceWith.Visible; }
            set
            {
                btnReplace.Visible = btnReplaceAll.Visible = value;
                lblReplaceWith.Visible = txtReplaceWith.Visible = value;
                btnHighlightAll.Visible = !value;
                AcceptButton = value ? btnReplace : btnFindNext;
                UpdateTitleBar();
            }
        }

        public string LookFor { get { return txtLookFor.Text; } }

        #region Lifecycle
        /// <summary>Normal constructor.</summary>
        public Finder()
        {
            InitializeComponent();
            _search = new TextToolSearcher();
        }
        #endregion

        #region Public methods
        public void ShowFor(TextEditorControl editor, bool replaceMode)
        {
            _editor = editor;
            _search.Document = _editor.Document;
            UpdateTitleBar();

            _search.ClearScanRegion();

            var sm = editor.ActiveTextAreaControl.SelectionManager;
            if (sm.HasSomethingSelected && sm.SelectionCollection.Count == 1)
            {
                var sel = sm.SelectionCollection[0];
                if (sel.StartPosition.Line == sel.EndPosition.Line)
                {
                    txtLookFor.Text = sm.SelectedText;
                }
                else
                {
                    _search.SetScanRegion(sel);
                }
            }
            else
            {
                // Get the current word that the caret is on.
                Caret caret = editor.ActiveTextAreaControl.Caret;
                int start = TextUtilities.FindWordStart(editor.Document, caret.Offset);
                int endAt = TextUtilities.FindWordEnd(editor.Document, caret.Offset);
                txtLookFor.Text = editor.Document.GetText(start, endAt - start);
            }

            ReplaceMode = replaceMode;

            Owner = editor.TopLevelControl as Form;
            Show();

            txtLookFor.SelectAll();
            txtLookFor.Focus();
        }

        public TextRange FindNext(bool viaF3, bool searchBackward, string messageIfNotFound)
        {
            if (string.IsNullOrEmpty(txtLookFor.Text))
            {
                Text = "No string specified to look for!";
                return null;
            }

            _lastSearchWasBackward = searchBackward;
            _search.LookFor = txtLookFor.Text;
            _search.MatchCase = chkMatchCase.Checked;
            _search.MatchWholeWordOnly = chkMatchWholeWord.Checked;

            var caret = _editor.ActiveTextAreaControl.Caret;
            if (viaF3 && _search.HasScanRegion && !(caret.Offset > _search.BeginOffset && caret.Offset < _search.EndOffset)) 
            {
                // User moved outside of the originally selected region.
                _search.ClearScanRegion();
                UpdateTitleBar();
            }

            int startFrom = caret.Offset - (searchBackward ? 1 : 0);
            TextRange range = _search.FindNext(startFrom, searchBackward, out _lastSearchLoopedAround);
            if (range != null)
            {
                SelectResult(range);
            }
            else
            {
                Text = messageIfNotFound;
            }

            return range;
        }
        #endregion

        #region Private methods
        private void UpdateTitleBar()
        {
            string text = ReplaceMode ? "Find & replace" : "Find";
            if (_editor != null && _editor.FileName != null)
            {
                text += " - " + Path.GetFileName(_editor.FileName);
            }

            if (_search.HasScanRegion)
            {
                text += " (selection only)";
            }

            Text = text;
        }

        private void btnFindPrevious_Click(object sender, EventArgs e)
        {
            FindNext(false, true, "Text not found");
        }

        private void btnFindNext_Click(object sender, EventArgs e)
        {
            FindNext(false, false, "Text not found");
        }

        private void SelectResult(TextRange range)
        {
            TextLocation p1 = _editor.Document.OffsetToPosition(range.Offset);
            TextLocation p2 = _editor.Document.OffsetToPosition(range.Offset + range.Length);
            _editor.ActiveTextAreaControl.SelectionManager.SetSelection(p1, p2);
            _editor.ActiveTextAreaControl.ScrollTo(p1.Line, p1.Column);
            // Also move the caret to the end of the selection, because when the user 
            // presses F3, the caret is where we start searching next time.
            _editor.ActiveTextAreaControl.Caret.Position = _editor.Document.OffsetToPosition(range.Offset + range.Length);
        }

        private void btnHighlightAll_Click(object sender, EventArgs e)
        {
            if (!_highlightGroups.ContainsKey(_editor))
            {
                _highlightGroups[_editor] = new HighlightGroup(_editor);
            }
            HighlightGroup group = _highlightGroups[_editor];

            if (string.IsNullOrEmpty(LookFor))
            {
                // Clear highlights.
                group.ClearMarkers();
            }
            else
            {
                _search.LookFor = txtLookFor.Text;
                _search.MatchCase = chkMatchCase.Checked;
                _search.MatchWholeWordOnly = chkMatchWholeWord.Checked;

                bool looped = false;
                int offset = 0, count = 0;
                for (; ; )
                {
                    TextRange range = _search.FindNext(offset, false, out looped);
                    if (range == null || looped)
                    {
                        break;
                    }
                    offset = range.Offset + range.Length;
                    count++;

                    var m = new TextMarker(range.Offset, range.Length, TextMarkerType.SolidBlock, Color.Yellow, Color.Black);
                    group.AddMarker(m);
                }

                if (count == 0)
                {
                    MessageBox.Show("Search text not found.");
                }
                else
                {
                    //    Close();
                }
            }
        }
        
        private void Finder_FormClosing(object sender, FormClosingEventArgs e)
        {    // Prevent dispose, as this form can be re-used.
            if (e.CloseReason != CloseReason.FormOwnerClosing)
            {
                if (Owner != null)
                {
                    Owner.Select(); // Prevent another app from being activated instead.
                }

                e.Cancel = true;
                Hide();
                
                // Discard search region.
                _search.ClearScanRegion();
                _editor.Refresh(); // Must repaint manually.
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnReplace_Click(object sender, EventArgs e)
        {
            var sm = _editor.ActiveTextAreaControl.SelectionManager;
            if (string.Equals(sm.SelectedText, txtLookFor.Text, StringComparison.OrdinalIgnoreCase))
            {
                InsertText(txtReplaceWith.Text);
            }
            FindNext(false, _lastSearchWasBackward, "Text not found.");
        }

        private void btnReplaceAll_Click(object sender, EventArgs e)
        {
            int count = 0;
            // BUG FIX: if the replacement string contains the original search string
            // (e.g. replace "red" with "very red") we must avoid looping around and
            // replacing forever! To fix, start replacing at beginning of region (by 
            // moving the caret) and stop as soon as we loop around.
            _editor.ActiveTextAreaControl.Caret.Position = _editor.Document.OffsetToPosition(_search.BeginOffset);
            _editor.Document.UndoStack.StartUndoGroup();

            try 
            {
                while (FindNext(false, false, null) != null)
                {
                    if (_lastSearchLoopedAround)
                    {
                        break;
                    }

                    // Replace.
                    count++;
                    InsertText(txtReplaceWith.Text);
                }
            } 
            finally 
            {
                _editor.Document.UndoStack.EndUndoGroup();
            }

            if (count == 0)
            {
                MessageBox.Show("No occurrances found.");
            }
            else
            {
                MessageBox.Show($"Replaced {count} occurrances.");
                //Close();
            }
        }

        private void InsertText(string text)
        {
            var textArea = _editor.ActiveTextAreaControl.TextArea;
            textArea.Document.UndoStack.StartUndoGroup();

            try 
            {
                if (textArea.SelectionManager.HasSomethingSelected)
                {
                    textArea.Caret.Position = textArea.SelectionManager.SelectionCollection[0].StartPosition;
                    textArea.SelectionManager.RemoveSelectedText();
                }
                textArea.InsertString(text);
            }
            finally
            {
                textArea.Document.UndoStack.EndUndoGroup();
            }
        }
        #endregion
    }

    public class TextRange : AbstractSegment
    {
        IDocument _document;
        public TextRange(IDocument document, int os, int ln)
        {
            _document = document;
            offset = os;
            length = ln;
        }
    }

    /// <summary>
    /// This class finds occurrences of a search string in a text editor's IDocument. it's like Find box without a GUI.
    /// </summary>
    public class TextToolSearcher : IDisposable
    {
        #region Fields
        // I would have used the TextAnchor class to represent the beginning and 
        // end of the region to scan while automatically adjusting to changes in 
        // the document--but for some reason it is sealed and its constructor is 
        // internal. Instead I use a TextMarker, which is perhaps even better as 
        // it gives me the opportunity to highlight the region. Note that all the 
        // markers and coloring information is associated with the text document, 
        // not the editor control, so TexToolSearcher doesn't need a reference 
        // to the TexToolControl. After adding the marker to the document, we
        // must remember to remove it when it is no longer needed.
        TextMarker _region = null;
        #endregion

        #region Properties
        IDocument _document;
        public IDocument Document
        {
            get { return _document; }
            set
            {
                if (_document != value)
                {
                    ClearScanRegion();
                    _document = value;
                }
            }
        }

        /// <summary>Begins the start offset for searching</summary>
        public int BeginOffset
        {
            get
            {
                return _region != null ? _region.Offset : 0;
            }
        }

        /// <summary>Begins the end offset for searching</summary>
        public int EndOffset
        {
            get
            {
                return _region != null ? _region.EndOffset : _document.TextLength;
            }
        }

        public bool HasScanRegion
        {
            get { return _region != null; }
        }

        //string _lookFor;
        string _lookFor2; // uppercase in case-insensitive mode
        public string LookFor { get; set; }
        public bool MatchCase { get; set; }
        public bool MatchWholeWordOnly { get; set; }
        #endregion

        #region Lifecycle
        ~TextToolSearcher() 
        { 
            Dispose();
        }
        #endregion

        public Color HalfMix(Color one, Color two)
        {
            return Color.FromArgb(
                (one.A + two.A) >> 1,
                (one.R + two.R) >> 1,
                (one.G + two.G) >> 1,
                (one.B + two.B) >> 1);
        }

        #region Public methods
        /// <summary>Sets the region to search. The region is updated automatically as the document changes.</summary>
        public void SetScanRegion(ISelection sel)
        {
            SetScanRegion(sel.Offset, sel.Length);
        }

        /// <summary>Sets the region to search. The region is updated automatically as the document changes.</summary>
        public void SetScanRegion(int offset, int length)
        {
            Color bkgColor = _document.HighlightingStrategy.GetColorFor("Default").BackgroundColor;
            _region = new TextMarker(offset, length, TextMarkerType.SolidBlock, bkgColor = HalfMix(bkgColor, Color.FromArgb(160, 160, 160)));
            _document.MarkerStrategy.AddMarker(_region);
        }

        public void ClearScanRegion()
        {
            if (_region != null)
            {
                _document.MarkerStrategy.RemoveMarker(_region);
                _region = null;
            }
        }

        public void Dispose()
        {
            ClearScanRegion();
            GC.SuppressFinalize(this);
        }

        /// <summary>Finds next instance of LookFor, according to the search rules (MatchCase, MatchWholeWordOnly).</summary>
        /// <param name="beginAtOffset">Offset in Document at which to begin the search</param>
        /// <param name="searchBackward"></param>
        /// <param name="loopedAround"></param>
        /// <remarks>If there is a match at beginAtOffset precisely, it will be returned.</remarks>
        /// <returns>Region of document that matches the search string</returns>
        public TextRange FindNext(int beginAtOffset, bool searchBackward, out bool loopedAround)
        {
            //Debug.Assert(!string.IsNullOrEmpty(LookFor));
            loopedAround = false;

            int startAt = BeginOffset, endAt = EndOffset;
            // *** was int curOffs = beginAtOffset.InRange(startAt, endAt);
            int curOffs = beginAtOffset < startAt ? startAt : (beginAtOffset > endAt ? endAt : beginAtOffset);

            _lookFor2 = MatchCase ? LookFor : LookFor.ToUpperInvariant();

            TextRange result;
            if (searchBackward)
            {
                result = FindNextIn(startAt, curOffs, true);
                if (result == null)
                {
                    loopedAround = true;
                    result = FindNextIn(curOffs, endAt, true);
                }
            }
            else
            {
                result = FindNextIn(curOffs, endAt, false);
                if (result == null)
                {
                    loopedAround = true;
                    result = FindNextIn(startAt, curOffs, false);
                }
            }
            return result;
        }
        #endregion

        #region Private methods
        private TextRange FindNextIn(int offset1, int offset2, bool searchBackward)
        {
           // Debug.Assert(offset2 >= offset1);
            offset2 -= LookFor.Length;

            // Make behavior decisions before starting search loop.
            Func<char, char, bool> matchFirstCh;
            Func<int, bool> matchWord;

            if (MatchCase)
            {
                matchFirstCh = (lookFor, c) => (lookFor == c);
            }
            else
            {
                matchFirstCh = (lookFor, c) => (lookFor == Char.ToUpperInvariant(c));
            }

            if (MatchWholeWordOnly)
            {
                matchWord = IsWholeWordMatch;
            }
            else
            {
                matchWord = IsPartWordMatch;
            }

            // Search.
            char lookForCh = _lookFor2[0];
            if (searchBackward)
            {
                for (int offset = offset2; offset >= offset1; offset--)
                {
                    if (matchFirstCh(lookForCh, _document.GetCharAt(offset)) && matchWord(offset))
                    {
                        return new TextRange(_document, offset, LookFor.Length);
                    }
                }
            }
            else
            {
                for (int offset = offset1; offset <= offset2; offset++) 
                {
                    if (matchFirstCh(lookForCh, _document.GetCharAt(offset)) && matchWord(offset))
                    {
                        return new TextRange(_document, offset, LookFor.Length);
                    }
                }
            }
            return null;
        }

        private bool IsWholeWordMatch(int offset)
        {
            if (IsWordBoundary(offset) && IsWordBoundary(offset + LookFor.Length))
            {
                return IsPartWordMatch(offset);
            }
            else
            {
                return false;
            }
        }

        private bool IsWordBoundary(int offset)
        {
            return offset <= 0 || offset >= _document.TextLength || !IsAlphaNumeric(offset - 1) || !IsAlphaNumeric(offset);
        }

        private bool IsAlphaNumeric(int offset)
        {
            char c = _document.GetCharAt(offset);
            return Char.IsLetterOrDigit(c) || c == '_';
        }

        private bool IsPartWordMatch(int offset)
        {
            string substr = _document.GetText(offset, LookFor.Length);
            if (!MatchCase)
            {
                substr = substr.ToUpperInvariant();
            }
            return substr == _lookFor2;
        }
        #endregion
    }

    /// <summary>
    /// Bundles a group of markers together so that they can be cleared together.
    /// </summary>
    public class HighlightGroup : IDisposable
    {
        List<TextMarker> _markers = new List<TextMarker>();
        public IList<TextMarker> Markers { get { return _markers.AsReadOnly(); } }

        TextEditorControl _editor;
        IDocument _document;

        public HighlightGroup(TextEditorControl editor)
        {
            _editor = editor;
            _document = editor.Document;
        }

        public void AddMarker(TextMarker marker)
        {
            _markers.Add(marker);
            _document.MarkerStrategy.AddMarker(marker);
        }

        public void ClearMarkers()
        {
            foreach (TextMarker m in _markers)
            {
                _document.MarkerStrategy.RemoveMarker(m);
            }
            _markers.Clear();
            _editor.Refresh();
        }

        public void Dispose()
        { 
            ClearMarkers(); 
            GC.SuppressFinalize(this); 
        }

        ~HighlightGroup() 
        {
            Dispose();
        }
    }

    partial class Finder
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.lblReplaceWith = new System.Windows.Forms.Label();
            this.txtLookFor = new System.Windows.Forms.TextBox();
            this.txtReplaceWith = new System.Windows.Forms.TextBox();
            this.btnFindNext = new System.Windows.Forms.Button();
            this.btnReplace = new System.Windows.Forms.Button();
            this.btnReplaceAll = new System.Windows.Forms.Button();
            this.chkMatchWholeWord = new System.Windows.Forms.CheckBox();
            this.chkMatchCase = new System.Windows.Forms.CheckBox();
            this.btnHighlightAll = new System.Windows.Forms.Button();
            this.btnFindPrevious = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Fi&nd what:";
            // 
            // lblReplaceWith
            // 
            this.lblReplaceWith.AutoSize = true;
            this.lblReplaceWith.Location = new System.Drawing.Point(12, 85);
            this.lblReplaceWith.Name = "lblReplaceWith";
            this.lblReplaceWith.Size = new System.Drawing.Size(72, 13);
            this.lblReplaceWith.TabIndex = 2;
            this.lblReplaceWith.Text = "Re&place with:";
            // 
            // txtLookFor
            // 
            this.txtLookFor.Location = new System.Drawing.Point(90, 6);
            this.txtLookFor.Name = "txtLookFor";
            this.txtLookFor.Size = new System.Drawing.Size(326, 20);
            this.txtLookFor.TabIndex = 1;
            // 
            // txtReplaceWith
            // 
            this.txtReplaceWith.Location = new System.Drawing.Point(90, 85);
            this.txtReplaceWith.Name = "txtReplaceWith";
            this.txtReplaceWith.Size = new System.Drawing.Size(326, 20);
            this.txtReplaceWith.TabIndex = 3;
            // 
            // btnFindNext
            // 
            this.btnFindNext.Location = new System.Drawing.Point(131, 32);
            this.btnFindNext.Name = "btnFindNext";
            this.btnFindNext.Size = new System.Drawing.Size(75, 23);
            this.btnFindNext.TabIndex = 6;
            this.btnFindNext.Text = "&Find next";
            this.btnFindNext.UseVisualStyleBackColor = true;
            this.btnFindNext.Click += new System.EventHandler(this.btnFindNext_Click);
            // 
            // btnReplace
            // 
            this.btnReplace.Location = new System.Drawing.Point(131, 111);
            this.btnReplace.Name = "btnReplace";
            this.btnReplace.Size = new System.Drawing.Size(75, 23);
            this.btnReplace.TabIndex = 7;
            this.btnReplace.Text = "&Replace";
            this.btnReplace.UseVisualStyleBackColor = true;
            this.btnReplace.Click += new System.EventHandler(this.btnReplace_Click);
            // 
            // btnReplaceAll
            // 
            this.btnReplaceAll.Location = new System.Drawing.Point(221, 111);
            this.btnReplaceAll.Name = "btnReplaceAll";
            this.btnReplaceAll.Size = new System.Drawing.Size(84, 23);
            this.btnReplaceAll.TabIndex = 9;
            this.btnReplaceAll.Text = "Replace &All";
            this.btnReplaceAll.UseVisualStyleBackColor = true;
            this.btnReplaceAll.Click += new System.EventHandler(this.btnReplaceAll_Click);
            // 
            // chkMatchWholeWord
            // 
            this.chkMatchWholeWord.AutoSize = true;
            this.chkMatchWholeWord.Location = new System.Drawing.Point(12, 55);
            this.chkMatchWholeWord.Name = "chkMatchWholeWord";
            this.chkMatchWholeWord.Size = new System.Drawing.Size(113, 17);
            this.chkMatchWholeWord.TabIndex = 5;
            this.chkMatchWholeWord.Text = "Match &whole word";
            this.chkMatchWholeWord.UseVisualStyleBackColor = true;
            // 
            // chkMatchCase
            // 
            this.chkMatchCase.AutoSize = true;
            this.chkMatchCase.Location = new System.Drawing.Point(12, 32);
            this.chkMatchCase.Name = "chkMatchCase";
            this.chkMatchCase.Size = new System.Drawing.Size(82, 17);
            this.chkMatchCase.TabIndex = 4;
            this.chkMatchCase.Text = "Match &case";
            this.chkMatchCase.UseVisualStyleBackColor = true;
            // 
            // btnHighlightAll
            // 
            this.btnHighlightAll.Location = new System.Drawing.Point(320, 32);
            this.btnHighlightAll.Name = "btnHighlightAll";
            this.btnHighlightAll.Size = new System.Drawing.Size(96, 23);
            this.btnHighlightAll.TabIndex = 8;
            this.btnHighlightAll.Text = "Highlight &all";
            this.btnHighlightAll.UseVisualStyleBackColor = true;
            this.btnHighlightAll.Visible = false;
            this.btnHighlightAll.Click += new System.EventHandler(this.btnHighlightAll_Click);
            // 
            // btnFindPrevious
            // 
            this.btnFindPrevious.Location = new System.Drawing.Point(221, 32);
            this.btnFindPrevious.Name = "btnFindPrevious";
            this.btnFindPrevious.Size = new System.Drawing.Size(84, 23);
            this.btnFindPrevious.TabIndex = 6;
            this.btnFindPrevious.Text = "Find pre&vious";
            this.btnFindPrevious.UseVisualStyleBackColor = true;
            this.btnFindPrevious.Click += new System.EventHandler(this.btnFindPrevious_Click);
            // 
            // Finder
            // 
            this.AcceptButton = this.btnReplace;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 142);
            this.Controls.Add(this.chkMatchCase);
            this.Controls.Add(this.chkMatchWholeWord);
            this.Controls.Add(this.btnReplaceAll);
            this.Controls.Add(this.btnReplace);
            this.Controls.Add(this.btnHighlightAll);
            this.Controls.Add(this.btnFindPrevious);
            this.Controls.Add(this.btnFindNext);
            this.Controls.Add(this.txtReplaceWith);
            this.Controls.Add(this.txtLookFor);
            this.Controls.Add(this.lblReplaceWith);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Finder";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Find and Replace";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Finder_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblReplaceWith;
        private System.Windows.Forms.TextBox txtLookFor;
        private System.Windows.Forms.TextBox txtReplaceWith;
        private System.Windows.Forms.Button btnFindNext;
        private System.Windows.Forms.Button btnReplace;
        private System.Windows.Forms.Button btnReplaceAll;
        private System.Windows.Forms.CheckBox chkMatchWholeWord;
        private System.Windows.Forms.CheckBox chkMatchCase;
        private System.Windows.Forms.Button btnHighlightAll;
        private System.Windows.Forms.Button btnFindPrevious;
    }
*/
