using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CSharpPrologIDE.Behaviors
{
    public static class AEBehaviors
    {
        #region ---------------------- CaretPositionChanged ---------------------------

        public static ICommand GetCaretPositionChanged(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CaretPositionChangedProperty);
        }

        public static void SetCaretPositionChanged(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CaretPositionChangedProperty, value);
        }

        // Using a DependencyProperty as the backing store for CaretPositionChanged.  This enables animation, styling, binding, etc…
        public static readonly DependencyProperty CaretPositionChangedProperty =
            DependencyProperty.RegisterAttached("CaretPositionChanged", typeof(ICommand), typeof(AEBehaviors), new PropertyMetadata(OnCaretPositionChangedChanged));

        private static void OnCaretPositionChangedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextEditor tEdit = d as TextEditor;
            ICommand command = e.NewValue as ICommand;
            tEdit.TextArea.Caret.PositionChanged += (object sender, EventArgs args) =>
            {
                // TODO: consider passing more parameters
                command.Execute(tEdit.TextArea.Caret);
            };
        }

        #endregion

    }
}
