using System;

namespace Aga.Controls
{
    public class FindEventArgs : EventArgs
    {
        public FindEventArgs(string text, bool matchCase, bool matchWhole, bool searchDown)
        {
            Text = text;
            MatchCase = matchCase;
            MatchWhole = matchWhole;
            SearchDown = searchDown;
        }

        public string Text { get; private set; }

        public bool MatchCase { get; private set; }

        public bool MatchWhole { get; private set; }

        public bool SearchDown { get; private set; }
    }
}