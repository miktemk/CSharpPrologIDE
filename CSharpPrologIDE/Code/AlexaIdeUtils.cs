using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CSharpPrologIDE.Code
{
    public class AlexaIdeUtils
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
    }
}
