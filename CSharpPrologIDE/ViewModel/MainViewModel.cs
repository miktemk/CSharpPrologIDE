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
using Miktemk.Wpf.Services;
using System.ComponentModel;

namespace CSharpPrologIDE.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly MyAppStateService appStateService;
        private readonly FileDialogsServiceWin32 fileDialogsService;

        private DateTime? curFileLoadedWhen = null;

        // ui config
        public UIElementDragDropConfig DragDropConfigProlog { get; } = Constants.Config.DragDropConfigProlog;

        // avalon-edit
        public TextDocument CodeDocument { get; } = new TextDocument();
        public TextDocument CodeDocumentQuery { get; } = new TextDocument();

        public IHighlightingDefinition SyntaxHighlighting { get; set; }
        public string CurInfoMessage { get; set; }
        public string CurErrorMessage { get; set; }
        public WordHighlight CurErrorWordHighlight { get; set; }
        public WordHighlight CurErrorWordHighlightQuery { get; set; }
        public string ConsoleText { get; set; }
        public string CurFilename { get; set; }
        public bool IsCurDocumentChanged { get; private set; } = false;
        public bool IsResultsPanelTextWrappingEnabled { get; set; }
        public string WindowTitle => $"CSharp Prolog Editor{CurFilename.PrefixIfNotEmpty(" - ")}{(IsCurDocumentChanged ? " *" : "")}";
        public string CurFilenameDir => (CurFilename != null) ? Path.GetDirectoryName(CurFilename) : null;
        public TextWrapping ResultsPanelTextWrapping => IsResultsPanelTextWrappingEnabled ? TextWrapping.Wrap : TextWrapping.NoWrap;

        // commands
        public ICommand CmdWindow_Loaded { get; }
        public ICommand CmdWindow_Closing { get; }
        public ICommand CmdWindow_Activated { get; }
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
            FileDialogsServiceWin32 fileDialogsService,
            MyAppStateService appStateService
            )
        {
            this.appStateService = appStateService;
            this.fileDialogsService = fileDialogsService;

            // .... set up view
            SyntaxHighlighting = MyPrologUtils.LoadSyntaxHighlightingFromResource(Constants.Resources.AvalonSyntaxProlog2);
            CodeDocument.Text = Constants.Samples.SampleProlog;
            CodeDocumentQuery.Text = Constants.Samples.SampleQuery;

            // .... assign commands
            CmdWindow_Loaded = new RelayCommand(_CmdWindow_Loaded);
            CmdWindow_Closing = new RelayCommand(_CmdWindow_Closing);
            CmdWindow_Activated = new RelayCommand(_CmdWindow_Activated);
            CmdUser_TriggerBuild = new RelayCommand(_CmdUser_TriggerBuild);
            CmdAvalon_CaretPositionChanged = new RelayCommand<Caret>(_CmdAvalon_CaretPositionChanged);
            CmdConsole_CopyAll = new RelayCommand(_CmdConsole_CopyAll);
            CmdConsole_ClearAll = new RelayCommand(_CmdConsole_ClearAll);
            CmdUser_OnDragDropProlog = new RelayCommand<string>(_CmdUser_OnDragDropProlog);
            CmdUser_Open = new RelayCommand(_CmdUser_Open);
            CmdUser_RevealInExplorer = new RelayCommand(_CmdUser_RevealInExplorer);
            CmdUser_Save = new RelayCommand(_CmdUser_Save);
            CmdUser_SaveAs = new RelayCommand(_CmdUser_SaveAs);

            this.PropertyChanged += THIS_PropertyChanged;
            CodeDocument.UndoStack.PropertyChanged += CodeDocument_UndoStack_PropertyChanged;

            // .... restore from previous state
            var appState = appStateService.LoadOrCreateNew();
            if (appState.LastQueryText != null)
                CodeDocumentQuery.Text = appState.LastQueryText;
            IsResultsPanelTextWrappingEnabled = appState.IsResultsPanelTextWrappingEnabled;

            // .... load file in question
            var argFilename = (string)Application.Current.Resources[Constants.Resources.Arg1Key];
            if (argFilename != null && File.Exists(argFilename))
                LoadFile(argFilename);
            else if (appState.LastFilename != null && File.Exists(appState.LastFilename))
                LoadFile(appState.LastFilename);

            CodeDocument.UndoStack.ClearAll();
            CodeDocumentQuery.UndoStack.ClearAll();
        }

        ~MainViewModel()
        {
            CodeDocument.UndoStack.PropertyChanged -= CodeDocument_UndoStack_PropertyChanged;
        }

        #region ------------------ commands -----------------------

        private void _CmdWindow_Loaded() { }
        
        private void _CmdWindow_Closing()
        {
            appStateService.ModifyAndSave(appState =>
            {
                appState.LastQueryText = CodeDocumentQuery.Text;
                appState.IsResultsPanelTextWrappingEnabled = IsResultsPanelTextWrappingEnabled;
            });
        }

        private void _CmdWindow_Activated()
        {
            if (IsCurDocumentChanged || curFileLoadedWhen == null || CurFilename == null || !File.Exists(CurFilename))
                return;
            var lastModified = File.GetLastWriteTime(CurFilename);
            if (lastModified > curFileLoadedWhen)
            {
                // .... reload the current file
                LoadFile(CurFilename, clearUndoBuffer: false);
                CurInfoMessage = $"File reloaded. Last write time: {lastModified.ToString("d MMM yyyy HH:mm:ss")}";
            }
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
            CurInfoMessage = null;
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
            if (IsCurDocumentChanged)
            {
                MessageBox.Show("You have unsaved changes!", "Unsaved changes!", MessageBoxButton.OK);
                return;
            }
            var filename = fileDialogsService.ShowOpenFileDialog(Constants.OpenSaveDialogFilter, CurFilenameDir);
            if (filename != null)
                LoadFile(filename);
        }

        private void _CmdUser_RevealInExplorer()
        {
            UtilsOp.OpenWinExplorerAndSelectThisFile(CurFilename);
        }

        private void _CmdUser_Save()
        {
            if (CurFilename == null)
            {
                _CmdUser_SaveAs();
                return;
            }
            SaveCurFile();
        }

        private void _CmdUser_SaveAs()
        {
            var filename = fileDialogsService.ShowSaveFileDialog(Constants.OpenSaveDialogFilter, CurFilenameDir);
            if (filename == null)
                return;
            CurFilename = filename;
            SaveCurFile();
        }

        #endregion

        #region ------------------ commands -----------------------

        private void THIS_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurErrorMessage) && CurErrorMessage != null)
                CurInfoMessage = null;
            if (e.PropertyName == nameof(CurInfoMessage) && CurInfoMessage != null)
                CurErrorMessage = null;
        }

        private void CodeDocument_UndoStack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            IsCurDocumentChanged = !CodeDocument.UndoStack.IsOriginalFile;
        }

        #endregion

        #region ------------------------------------------- privates ----------------------------------------

        private void LoadFile(string filename, bool clearUndoBuffer = true)
        {
            if (!File.Exists(filename))
                return;
            CurFilename = filename;
            CodeDocument.Text = File.ReadAllText(filename);
            if (clearUndoBuffer)
                CodeDocument.UndoStack.ClearAll();
            CodeDocument.UndoStack.MarkAsOriginalFile();
            appStateService.ModifyAndSave(appState =>
            {
                appState.LastFilename = filename;
            });
            curFileLoadedWhen = DateTime.Now;
        }

        private void SaveCurFile()
        {
            File.WriteAllText(CurFilename, CodeDocument.Text);
            CodeDocument.UndoStack.MarkAsOriginalFile();
        }

        #endregion
    }
}