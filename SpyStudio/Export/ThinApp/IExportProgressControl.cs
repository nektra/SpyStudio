namespace SpyStudio.Export.ThinApp
{
    public interface IExportProgressControl
    {
        void LogString(string aString);
        void LogError(string aString);
        void SetProgress(int aPercentage);
        void Start();
    }
}