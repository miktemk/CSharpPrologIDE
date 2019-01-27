using CSharpPrologIDE.Code;
using Miktemk.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpPrologIDE.Services
{
    public class MyAppStateService : LocalAppDataSingleFileService<MyAppState>
    {
        protected override string ProjName => "CSharpPrologIDE";
        protected override string Filename => "app-state.json";
    }
}
