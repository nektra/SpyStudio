using SpyStudio.Tools;

namespace SpyStudio.FileSystem
{
    class NullPathNormalizer : PathNormalizer
    {
        #region Overrides of PathNormalizer

        public override string LocalAppDataPath
        {
            get { throw new System.NotImplementedException(); }
        }

        public override string RoamingAppDataPath
        {
            get { throw new System.NotImplementedException(); }
        }

        public override string RuntimePath
        {
            get { throw new System.NotImplementedException(); }
        }

        public override string ProgramFiles
        {
            get { throw new System.NotImplementedException(); }
        }

        public override string ProgramFiles86
        {
            get { throw new System.NotImplementedException(); }
        }

        public override CallEvent InternalIncludeEvent(CallEvent originalEvent, string fileSystemPath)
        {
            return originalEvent.Clone();
        }

        protected override string InternalNormalize(string aPath)
        {
            throw new System.NotImplementedException();
        }

        public override string Unnormalize(string aPath)
        {
            return aPath;
        }

        #endregion
    }
}