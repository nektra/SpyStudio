using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SpyStudio.Extensions;

namespace SpyStudio.Tools
{
    public class IconCache
    {
        private Dictionary<string, Image> _byExtension = new Dictionary<string, Image>();
        private Dictionary<string, Image> _byPath = new Dictionary<string, Image>();
        private readonly Image _defaultExeIcon;
        protected IconCache()
        {
            _defaultExeIcon = FileSystemTools.GetIconUncached(".exe", ".exe", true);
        }

        private static readonly IconCache _instance = new IconCache();
        public static IconCache GetInstance()
        {
            return _instance;
        }

        public bool TryGetNormalIcon(string ext, out Image result)
        {
            lock (this)
            {
                if (_byExtension.TryGetValue(ext.ToLower(), out result))
                {
                    result = (Image)result.Clone();
                    return true;
                }
                return false;
            }
        }

        public bool TryGetExeIcon(string path, bool getDefault, out Image result)
        {
            path = path.AsNormalizedPath().ToLower();
            lock (this)
            {
                if (getDefault)
                {
                    result = (Image) _defaultExeIcon.Clone();
                    return true;
                }

                if (_byPath.TryGetValue(path, out result))
                {
                    result = (Image)result.Clone();
                    return true;
                }
                return false;
            }
        }

        public void SetNormalIcon(string ext, Image icon)
        {
            ext = ext.ToLower();
            icon = (Image) icon.Clone();
            lock (this)
            {
                _byExtension[ext] = icon;
            }
        }

        public void SetExeIcon(string path, Image icon)
        {
            path = path.AsNormalizedPath().ToLower();
            icon = (Image)icon.Clone();
            lock (this)
            {
                _byPath[path] = icon;
            }
        }

        public void Clear()
        {
            lock (this)
            {
                _byExtension.Clear();
                _byPath.Clear();
            }
        }

    }
}
