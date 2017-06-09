using System.Linq;
using SpyStudio.Tools;
using SpyStudio.Trace;

namespace SpyStudio.Dialogs.Compare
{
    public class MatchInfo
    {
        public int Index;
        public bool IsCaseSensitive;
        public bool IsResult;
        public bool OnlyFilename;
        public string HelpString;

        public static string GetField(CallEvent e, int index)
        {
            return (e.Params == null || index >= e.Params.Count()) ? "" : e.Params[index].Value;
        }

        public string GetMatchStringFor(CallEvent e)
        {
            var matchString = GetField(e, Index);

            if (!IsCaseSensitive)
            {
                matchString = matchString.ToLower();
            }

            if (OnlyFilename)
            {
                matchString = ModulePath.ExtractModuleName(matchString);
            }

            return matchString;
        }

        public bool IsSatisfiedBy(EventInfo event1, EventInfo event2)
        {
            return GetMatchStringFor(event1.Event) == GetMatchStringFor(event2.Event);
        }
    }
}