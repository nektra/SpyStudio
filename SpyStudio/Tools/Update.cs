using System;

namespace SpyStudio.Tools
{
    internal class UpdateClass
    {
        public static void Check(string page)
        {
            try
            {
                GoogleAnalyticsDotNet.GoogleAnalyticsDotNet.SendTrackingRequest("UA-562204-2", page, null);
            }
            catch (Exception)
            {
            }
        }
    }
}
