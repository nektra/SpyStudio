using System.Drawing;
using SpyStudio.Dialogs.ExportWizards;
using SpyStudio.Tools;

namespace SpyStudio.Export.ThinApp
{
    public class ThinAppFileEntry : FileEntry
    {
        public ThinAppFileEntry()
        {
        }
        public ThinAppFileEntry(string path, string fileSystemPath, FileSystemAccess access, bool success, string company, string version,
                         string description, string productName, string originalFileName, Image icon, ThinAppIsolationOption isolation)
            : base(path, fileSystemPath, access, success, company, version, description, productName, originalFileName, icon)
        {
            IsolationRule = isolation;
        }

        public ThinAppFileEntry(FileEntry entry, ThinAppIsolationOption isolation)
            : base(entry)
        {
            IsolationRule = isolation;
        }
        public override FileEntry HalfClone()
        {
            return new ThinAppFileEntry(this, IsolationRule);
        }
        public ThinAppIsolationOption IsolationRule;

        public override FileEntry GetUpdatedEntryFromFileSystem()
        {
            var updatedEntry = ForPath(FileSystemPath, ThinAppPathNormalizer.GetInstance().Normalize(FileSystemPath));

            var updatedThinAppEntry = new ThinAppFileEntry(updatedEntry, IsolationRule);

            return updatedThinAppEntry;
        }
    }
}
