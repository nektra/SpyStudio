using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SpyStudio.Tools
{
    public static class EntryColors
    {
        // Non-Compare Colors
        public static Color SimpleSuccess = Color.Black;
        public static Color SimpleError = Color.Red;
        public static Color NonCaptured = Color.Blue;

        // Compare Colors
        public static Color File1Color = Color.LightSkyBlue;
        public static Color File2Color = Color.MediumSpringGreen;

        public static Color NoMatchSuccessColor = Color.DarkSlateGray;
        public static Color NoMatchErrorColor = Color.OrangeRed;

        public static Color MatchSuccessColor = Color.Black;
        public static Color MatchErrorColor = Color.Red;

        public static Color ExactMatchSuccessColor = Color.DarkGray;
        public static Color ExactMatchErrorColor = Color.Salmon;

        public static Color MatchResultMismatchColor = Color.DarkMagenta;

        public static Color SuccessColor = Color.Black;
        public static Color ErrorColor = Color.Red;

        public static Color StandardColor = Color.Black;

        public static readonly Color ValidPathColor = Color.DarkGray;
        public static readonly Color InvalidPathColor = Color.Tomato;
        public static readonly Color WarningMajorVersionColor = Color.DarkGoldenrod;
        public static readonly Color WarningMinorVersionColor = Color.Goldenrod;

        public static readonly Color[] SuccessColorByPrioritySummary =
            {
                Color.FromArgb(20, 20, 20),
                Color.FromArgb(115, 115, 115),
                Color.FromArgb(160, 160, 160),
                Color.FromArgb(200, 200, 200),
                Color.FromArgb(250, 250, 250)
            };

        public static readonly Color[] ErrorColorByPrioritySummary =
            {
                Color.FromArgb(255, 0, 0),
                Color.FromArgb(255, 51, 51),
                Color.FromArgb(255, 102, 102),
                Color.FromArgb(255, 204, 204),
                Color.FromArgb(255, 234, 234)
            };

        public static readonly Color[] SuccessColorByPriority =
            {
                Color.FromArgb(0, 0, 0),
                Color.FromArgb(25, 25, 25),
                Color.FromArgb(55, 55, 55),
                Color.FromArgb(75, 75, 75),
                Color.FromArgb(100, 100, 100)
            };

        public static readonly Color[] ErrorColorByPriority =
            {
                Color.FromArgb(255, 0, 0),
                Color.FromArgb(255, 25, 25),
                Color.FromArgb(255, 55, 55),
                Color.FromArgb(255, 75, 75),
                Color.FromArgb(255, 100, 100)
            };

        //public static Color[] Trace1ColorByPriority =
        //    {
        //        Color.FromArgb(30, 23, 159),
        //        Color.FromArgb(40, 31, 211),
        //        Color.FromArgb(104, 98, 232),
        //        Color.FromArgb(140, 135, 237),
        //        Color.FromArgb(184, 181, 244)
        //    };

        //public static Color[] Trace2ColorByPriority =
        //    {
        //        Color.FromArgb(30, 159, 23),
        //        Color.FromArgb(40, 211, 31),
        //        Color.FromArgb(104, 232, 98),
        //        Color.FromArgb(140, 237, 135),
        //        Color.FromArgb(184, 244, 181)
        //    };

        public static Color GetColor(bool success, bool critical, int priority)
        {
            Debug.Assert(priority <= 5 && priority > 0);
            if (critical)
                return CriticalColor;
            return success
                       ? SuccessColorByPriority[priority - 1]
                       : ErrorColorByPriority[priority - 1];

        }
        public static Color GetColorSummary(bool success, bool critical, int priority)
        {
            Debug.Assert(priority <= 5 && priority > 0);
            if (critical)
                return CriticalColor;
            return success
                       ? SuccessColorByPrioritySummary[priority - 1]
                       : ErrorColorByPrioritySummary[priority - 1];

        }

        public static Color CriticalColor = Color.FromArgb(255, 0, 0);

        //public static Color Lighten(Color inColor, double inAmount)
        //{
        //    return Color.FromArgb(
        //      inColor.A,
        //      (int)Math.Min(255, inColor.R + 255 * inAmount),
        //      (int)Math.Min(255, inColor.G + 255 * inAmount),
        //      (int)Math.Min(255, inColor.B + 255 * inAmount));
        //}
    }
}
