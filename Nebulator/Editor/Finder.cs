﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Document;



namespace Nebulator.Editor
{
    /// <summary>
    /// A general purpose editor form for use with the ICSharpCode.TextEditor control.
    /// Original from the ICSharpCode project. Could use better documentation.
    /// </summary>
    public partial class Finder : Form
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
}
