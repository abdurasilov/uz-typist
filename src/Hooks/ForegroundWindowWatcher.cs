using System;
using System.Runtime.InteropServices;

namespace UzTypist.Hooks
{
    public sealed class ForegroundWindowWatcher
    {
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        private readonly Action _onWindowChanged;
        private IntPtr _hookId = IntPtr.Zero;
        private WinEventDelegate? _proc;

        public ForegroundWindowWatcher(Action onWindowChanged)
        {
            _onWindowChanged = onWindowChanged;
        }

        public void Start()
        {
            _proc = WinEventCallback;
            _hookId = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, _proc, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWinEvent(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private void WinEventCallback(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            _onWindowChanged();
        }

        #region Win32 interop

        private delegate void WinEventDelegate(
            IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(
            uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        #endregion
    }
}
