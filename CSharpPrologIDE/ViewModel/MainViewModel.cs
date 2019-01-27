using CSharpPrologIDE.Code;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows.Input;
using System;
using System.Linq;
using System.Diagnostics;
using Miktemk.Logging;
using Prolog;
using Miktemk;
using Miktemk.Models;
using System.Windows;
using CSharpPrologIDE.Services;
using Miktemk.Wpf.Core.Behaviors.VM;
using System.IO;

namespace CSharpPrologIDE.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly MyAppStateService appStateService;

        // ui config
        public UIElementDragDropConfig DragDropConfigProlog { get; } = Constants.Config.DragDropConfigProlog;

        // avalon-edit
        public TextDocument CodeDocument { get; } = new TextDocument();
        public TextDocument CodeDocumentQuery { get; } = new TextDocument();

        public IHighlightingDefinition SyntaxHighlighting { get; set; }
        public string CurErrorMessage { get; set; }
        public WordHighlight CurErrorWordHighlight { get; set; }
        public WordHighlight CurErrorWordHighlightQuery { get; set; }
        public string ConsoleText { get; set; }
        public string CurFilename { get; set; }
        public string WindowTitle => $"CSharp Prolog Editor{CurFilename.PrefixIfNotEmpty(" - ")}{(CodeDocument.UndoStack.IsOriginalFile ? " *" : "")}";

        // commands
        public ICommand CmdWindow_Loaded { get; }
        public ICommand CmdWindow_Closing { get; }
        public ICommand CmdUser_TriggerBuild { get; }
        public ICommand CmdAvalon_CaretPositionChanged { get; }
        public ICommand CmdConsole_CopyAll { get; }
        public ICommand CmdConsole_ClearAll { get; }
        public ICommand CmdUser_OnDragDropProlog { get; }
        public ICommand CmdUser_Open { get; }
        public ICommand CmdUser_RevealInExplorer { get; }
        public ICommand CmdUser_Save { get; }
        public ICommand CmdUser_SaveAs { get; }

        public MainViewModel(
            MyAppStateService appStateService)
        {
            this.appStateService = appStateService;

            // .... set up view
            SyntaxHighlighting = MyPrologUtils.LoadSyntaxHighlightingFromResource(Constants.Resources.AvalonSyntaxProlog2);
            CodeDocument.Text = Constants.Samples.SampleProlog;
            CodeDocumentQuery.Text = Constants.Samples.SampleQuery;

            // .... assign commands
            CmdWindow_Loaded = new RelayCommand(_CmdWindow_Loaded);
            CmdWindow_Closing = new RelayCommand(_CmdWindow_Closing);
            CmdUser_TriggerBuild = new RelayCommand(_CmdUser_TriggerBuild);
            CmdAvalon_CaretPositionChanged = new RelayCommand<Caret>(_CmdAvalon_CaretPositionChanged);
            CmdConsole_CopyAll = new RelayCommand(_CmdConsole_CopyAll);
            CmdConsole_ClearAll = new RelayCommand(_CmdConsole_ClearAll);
            CmdUser_OnDragDropProlog = new RelayCommand<string>(_CmdUser_OnDragDropProlog);
            CmdUser_Open = new RelayCommand(_CmdUser_Open);
            CmdUser_RevealInExplorer = new RelayCommand(_CmdUser_RevealInExplorer);
            CmdUser_Save = new RelayCommand(_CmdUser_Save);
            CmdUser_SaveAs = new RelayCommand(_CmdUser_SaveAs);

            // .... restore from previous state
            var appState = appStateService.LoadOrCreateNew();
            if (appState.LastFilename != null)
                LoadFile(appState.LastFilename);
            if (appState.LastQueryText != null)
                CodeDocumentQuery.Text = appState.LastQueryText;

            CodeDocument.UndoStack.ClearAll();
            CodeDocumentQuery.UndoStack.ClearAll();
        }

        #region ------------------ commands -----------------------

        private void _CmdWindow_Loaded() { }

        private void _CmdWindow_Closing()
        {
            appStateService.ModifyAndSave(appState =>
            {
                appState.LastQueryText = CodeDocumentQuery.Text;
            });
        }

        private void _CmdUser_TriggerBuild()
        {
            try
            {
                var prolog = new PrologEngine(persistentCommandHistory: false);
                prolog.ConsultFromString(CodeDocument.Text);
                prolog.GetFirstSolution(CodeDocumentQuery.Text);
                MyConsole.WriteLine("----------------------");
                foreach (var sol in prolog.GetEnumerator())
                {
                    var errorInfo = MyPrologUtils.TryToGetErrorFromPrologSolution(sol);
                    if (errorInfo != null)
                    {
                        CurErrorMessage = errorInfo.Message;
                        var errorLine = CodeDocumentQuery.GetLineByNumber(1);
                        CurErrorWordHighlightQuery = new WordHighlight(errorLine.Offset, errorLine.Length);
                        break;
                    }
                    var stringified = sol.VarValuesIterator.Select(val => $"{val.Name}:{val.Value}").StringJoin(", ");
                    MyConsole.WriteLine(stringified);
                }
            }
            catch (Exception ex)
            {
                var errorInfo = MyPrologUtils.TryToGetErrorFromException(ex);
                if (errorInfo.Line != null)
                {
                    var errorLine = CodeDocument.GetLineByNumber(errorInfo.Line.Value);
                    CurErrorWordHighlight = new WordHighlight(errorLine.Offset, errorLine.Length);
                }
                CurErrorMessage = errorInfo.Message;
            }
        }

        private void _CmdAvalon_CaretPositionChanged(Caret caret)
        {
            CurErrorMessage = null;
            CurErrorWordHighlight = null;
            CurErrorWordHighlightQuery = null;

            //var curCaretOffset = caret.Offset;
            //var curCaretLine = CodeDocument.GetLineByNumber(caret.Line);
            //var curLineText = CodeDocument.GetText(curCaretLine);
            // NOTE: this is a test output
            //CodeDocumentOutput.Text = $@"
            //curCaretLine: [{curCaretLine.Offset}..{curCaretLine.EndOffset}]
            //curLineText: {curLineText}
            //curCaret: {caret.Offset}
            //          {caret.Line} : {caret.Column}
            //";
        }

        private void _CmdConsole_CopyAll()
        {
            Clipboard.SetText(ConsoleText);
        }
        private void _CmdConsole_ClearAll()
        {
            ConsoleText = "";
        }

        private void _CmdUser_OnDragDropProlog(string filename)
        {
            LoadFile(filename);
        }

        private void _CmdUser_Open()
        {
            MessageBox.Show("Open via file dialog not yet implemented, use Drag+Drop instead", "Not implemented!", MessageBoxButton.OK);
        }

        private void _CmdUser_RevealInExplorer()
        {
            UtilsOp.OpenWinExplorerAndSelectThisFile(CurFilename);
        }

        private void _CmdUser_Save()
        {
            if (CurFilename == null)
            {
                MessageBox.Show("Save file dialog not yet implemented. Open a file via Drag+Drop and then you can save it", "Not implemented!", MessageBoxButton.OK);
                return;
            }
            File.WriteAllText(CurFilename, CodeDocument.Text);
            CodeDocument.UndoStack.ClearAll();
        }

        private void _CmdUser_SaveAs()
        {
            _CmdUser_Save();
        }

        #endregion

        #region ------------------------------------------- privates ----------------------------------------

        private void LoadFile(string filename)
        {
            if (!File.Exists(filename))
                return;
            CurFilename = filename;
            CodeDocument.Text = File.ReadAllText(filename);
            CodeDocument.UndoStack.ClearAll();
            appStateService.ModifyAndSave(appState =>
            {
                appState.LastFilename = filename;
            });
        }

        #endregion
    }
}