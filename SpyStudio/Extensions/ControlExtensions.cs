using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SpyStudio.Tools;

namespace SpyStudio.Extensions
{
    public static class ControlExtensions
    {
        #region Threading

        public static void ExecuteInUIThreadAsynchronously(this Control aControl, Action anAction)
        {
            if (aControl.InvokeRequired)
                aControl.BeginInvoke(anAction);
            else
                anAction();
        }
        public static void ExecuteInUIThreadSynchronously(this Control aControl, Action anAction)
        {
            if (aControl.InvokeRequired)
            {
                aControl.Invoke(anAction);
                return;
            }

            anAction();
        }

        #endregion

        #region UI State Control

        static readonly HashSet<Control> DisabledControls = new HashSet<Control>();
        private static GlobalUserInputHandler _globalClick;

        public static void DisableUI(this Control aControl)
        {
            if (DisabledControls.Count == 0)
            {
                _globalClick = new GlobalUserInputHandler();
                Application.AddMessageFilter(_globalClick);
            }
            if (!DisabledControls.Contains(aControl))
            {
                DisabledControls.Add(aControl);
                _globalClick.AddNotClickableWindow(aControl.Handle);
            }
        }

        public static void EnableUI(this Control aControl)
        {
            if (DisabledControls.Contains(aControl))
            {
                DisabledControls.Remove(aControl);
                _globalClick.RemoveNotClickableWindow(aControl.Handle);
                if (DisabledControls.Count == 0)
                {
                    Application.RemoveMessageFilter(_globalClick);
                    _globalClick = null;
                }
            }
        }

        private class GlobalUserInputHandler : IMessageFilter
        {
            private const int WmLbuttondown = 0x201;
            private const int WmLbuttonup = 0x202;
            private const int WmKeydown = 0x0100;
            private const int WmKeyup = 0x0101;

            private readonly List<IntPtr> _notClickeableWnd = new List<IntPtr>();

            public void AddNotClickableWindow(IntPtr hWnd)
            {
                _notClickeableWnd.Add(hWnd);
            }

            public void RemoveNotClickableWindow(IntPtr hWnd)
            {
                _notClickeableWnd.Remove(hWnd);
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WmLbuttondown || m.Msg == WmLbuttonup || m.Msg == WmKeydown || m.Msg == WmKeyup)
                {
                    if (_notClickeableWnd.Count > 0)
                    {
                        var hWnd = m.HWnd;
                        while (hWnd != IntPtr.Zero && !_notClickeableWnd.Contains(hWnd))
                        {
                            hWnd = Declarations.GetParent(hWnd);
                        }
                        // filter if it is a child of a window inside the _notClickeableWnd
                        return hWnd != IntPtr.Zero;
                    }
                }
                return false;
            }
        }

        #endregion
    }
}
