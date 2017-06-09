using System;
using System.Collections.Generic;
using System.Text;
using SpyStudio.Tools;

namespace SpyStudio.Windows.Controls
{
    public class WindowClassNames
    {
        readonly Dictionary<UIntPtr, string> _classNames = new Dictionary<UIntPtr, string>();
        readonly object _dataLock = new object();
        
        public void AddWindow(UIntPtr hWnd, string className)
        {
            lock (_dataLock)
            {
                _classNames[hWnd] = className;
            }
        }
        public string GetClassName(UIntPtr hWnd)
        {
            string className;
            lock (_dataLock)
            {
                if (!_classNames.TryGetValue(hWnd, out className))
                {
                    var classNameStr = new StringBuilder(100);
                    if (Declarations.GetClassName(hWnd, classNameStr, 100) != 0)
                    {
                        className = classNameStr.ToString();
                        AddWindow(hWnd, className);
                    }
                    else
                        className = "";
                }
            }
            return className;
        }
        public void Clear()
        {
            lock(_dataLock)
            {
                _classNames.Clear();
            }
        }
    }
}