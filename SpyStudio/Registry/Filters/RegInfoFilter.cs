using System.Collections.Generic;
using SpyStudio.Registry.Infos;

namespace SpyStudio.Registry.Filters
{
    public abstract class RegInfoFilter
    {
        public void ApplyTo(RegKeyInfo aRegKeyInfo)
        {
            aRegKeyInfo.Path = Convert(aRegKeyInfo.Path);
            foreach (var regValueInfo in aRegKeyInfo.ValuesByName.Values)
                regValueInfo.Path = Convert(regValueInfo.Path);
        }

        public void ApplyTo(RegValueInfo aRegValueInfo)
        {
            aRegValueInfo.Path = Convert(aRegValueInfo.Path);
        }

        public void ApplyTo(ref string path)
        {
            path = Convert(path);
        }

        protected abstract string Convert(string aPath);
    }
}