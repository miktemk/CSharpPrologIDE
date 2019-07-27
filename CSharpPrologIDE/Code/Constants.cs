using Miktemk.Wpf.Core.Behaviors.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpPrologIDE.Code
{
    public static class Constants
    {
        public static class Resources
        {
            // TODO: find a t4 template to generate this from contents of "Resource" folder
            public const string AvalonSyntaxTemp = "CSharpPrologIDE.Resources.syntax-temp.xshd";
            public const string AvalonSyntaxProlog2 = "CSharpPrologIDE.Resources.syntax-prolog2.xshd";
            public const string Arg1Key = "CSharpPrologIDE.CmdLineArg1";
        }

        public static class Samples
        {
            public const string SampleProlog = @"male(james1).
male(charles1).
male(charles2).
male(james2).
male(george1).

female(catherine).
female(elizabeth).
female(sophia).

parent(charles1, james1).
parent(elizabeth, james1).
parent(charles2, charles1).
parent(catherine, charles1).
parent(james2, charles1).
parent(sophia, elizabeth).
parent(george1, sophia).";
            public const string SampleQuery = "parent(X,charles1).";
        }

        public static class Config
        {
            public static readonly UIElementDragDropConfig DragDropConfigProlog = new UIElementDragDropConfig
            {
                Mode = UIElementDragDropMode.FileBased,
                ValidFileExtensions = new[] { ".pl" },
            };
        }
    }
}
