using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpPrologIDE.Code
{
    public class MyAppState
    {
        public string LastFilename { get; set; }
        public string LastQueryText { get; set; }
        public bool IsResultsPanelTextWrappingEnabled { get; set; }
    }
}
