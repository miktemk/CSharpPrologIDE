using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Miktemk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Miktemk;
using Prolog;

namespace CSharpPrologIDE.Code
{
    public class MyPrologUtils
    {
        public static IHighlightingDefinition LoadSyntaxHighlightingFromResource(string resourceName)
        {
            // set view data (synatx highlighting, code, etc)
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (XmlTextReader xshd_reader = new XmlTextReader(stream))
            {
                return HighlightingLoader.Load(xshd_reader, HighlightingManager.Instance);
            }
        }

        public static PrologSyntaxError TryToGetErrorFromException(Exception ex)
        {
            // LINK: https://regex101.com/r/mpM520/2
            var match1 = Regex.Match(ex.Message, @"\*\*\* error in line (\d+) at position (\d+): (.*)");
            if (match1.Success)
                return new PrologSyntaxError
                {
                    Line = match1.Groups[1].Value.ParseIntOrNull(),
                    Pos = match1.Groups[2].Value.ParseIntOrNull(),
                    Message = match1.Groups[3].Value,
                };
            return new PrologSyntaxError
            {
                Message = ex.Message,
            };
        }

        public static PrologSyntaxError TryToGetErrorFromPrologSolution(PrologEngine.ISolution sol)
        {
            var solStr = sol.ToString(); // NOTE: msg property is not publicly accessible, by ToString() gives it away (for now)

            // LINK: https://regex101.com/r/9acgAX/2
            var match1 = Regex.Match(solStr, @"\*\*\* (Unexpected symbol.*|error.*)");
            if (match1.Success)
                return new PrologSyntaxError
                {
                    Message = match1.Groups[1].Value,
                };
            return null;
        }
    }
}
