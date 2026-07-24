using System;
using System.Runtime.InteropServices;

namespace UzTypist.Hooks
{
    public sealed class MouseClickWatcher
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;

        private readonly Action _onClick;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelMouseProc? _proc;

        public MouseClickWatcher(Action onClick)
        {
            _onClick = onClick;
        }

        public void Start()
        {
            _proc = HookCallback;
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                if (msg == WM_LBUTTONDOWN || msg == WM_RBUTTONDOWN ||
                    msg == WM_MBUTTONDOWN || msg == WM_XBUTTONDOWN)
                {
                    _onClick();
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        #region Win32 interop

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
