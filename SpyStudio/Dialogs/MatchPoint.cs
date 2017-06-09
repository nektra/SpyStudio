namespace SpyStudio.Dialogs
{
    internal class MatchPoint
    {
        public MatchPoint(int index1, int index2, int contiguousMatches)
        {
            Index1 = index1;
            Index2 = index2;
            ContiguousMatches = contiguousMatches;
        }

        protected MatchPoint()
        {
        }

        public int Index1 { get; set; }
        public int Index2 { get; set; }
        public int ContiguousMatches { get; set; }
    }
}