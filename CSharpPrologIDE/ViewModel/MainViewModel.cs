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

namespace CSharpPrologIDE.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public string WelcomeTitle { get; set; } = "MVVM test app";

        // avalon-edit
        public TextDocument CodeDocument { get; } = new TextDocument();
        public IHighlightingDefinition SyntaxHighlighting { get; set; }

        // commands
        public ICommand WindowLoadedCommand { get; }
        public ICommand WindowClosingCommand { get; }
        public ICommand TriggerBuildCommand { get; }
        public ICommand CaretPositionChangedCommand { get; }

        public MainViewModel()
        {
            // set up view
            SyntaxHighlighting = AlexaIdeUtils.LoadSyntaxHighlightingFromResource(Constants.Resources.SyntaxXshd);
            CodeDocument.Text = @"";

            // assign commands
            WindowLoadedCommand = new RelayCommand(WindowLoaded);
            WindowClosingCommand = new RelayCommand(WindowClosing);
            TriggerBuildCommand = new RelayCommand(TriggerBuild);
            CaretPositionChangedCommand = new RelayCommand<Caret>(CaretPositionChanged);
        }

        #region ------------------ commands -----------------------

        private void WindowLoaded() { }

        private void WindowClosing() { }

        private void TriggerBuild()
        {
            MyConsole.WriteLine("----------------------");
            var prolog = new PrologEngine(persistentCommandHistory: false);
            prolog.ConsultFromString(CodeDocument.Text);
            prolog.GetFirstSolution("flight_param(X,Y,Z).");
            foreach (var sol in prolog.GetEnumerator())
            {
                var stringified = sol.VarValuesIterator.Select(val => $"{val.Name}:{val.Value}").StringJoin(", ");
                MyConsole.WriteLine(stringified);
            }
        }

        private void CaretPositionChanged(Caret caret)
        {
            var curCaretOffset = caret.Offset;
            var curCaretLine = CodeDocument.GetLineByNumber(caret.Line);
            var curLineText = CodeDocument.GetText(curCaretLine);

            // NOTE: this is a test output
            //CodeDocumentOutput.Text = $@"
            //curCaretLine: [{curCaretLine.Offset}..{curCaretLine.EndOffset}]
            //curLineText: {curLineText}
            //curCaret: {caret.Offset}
            //          {caret.Line} : {caret.Column}
            //";
        }

        #endregion
    }
}