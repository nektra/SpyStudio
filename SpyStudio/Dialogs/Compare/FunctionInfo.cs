using System.Collections.Generic;
using System.Drawing;

namespace SpyStudio.Dialogs.Compare
{
    public class FunctionInfo
    {
        public readonly List<MatchInfo> MatchInfos = new List<MatchInfo>();
        public int Priority;
        public Color Color;
        public bool MatchFunctionResult;
    }
}