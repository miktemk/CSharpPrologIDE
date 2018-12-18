using Miktemk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpPrologIDE.Code
{
    public class PrologSyntaxError
    {
        //public WordHighlight Position { get; set; }
        public int? Line { get; set; }
        public int? Pos { get; set; }
        public string Message { get; set; }
    }
}
