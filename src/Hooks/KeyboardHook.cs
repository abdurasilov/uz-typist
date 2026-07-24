using System;
using System.Runtime.InteropServices;
using UzTypist.Context;

namespace UzTypist.Hooks
{
    public sealed class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_SHIFT = 0x10;
        private const int VK_CAPITAL = 0x14;
        private const int VK_OEM_7 = 0xDE;
        private const int VK_SPACE = 0x20;
        private const int VK_RETURN = 0x0D;
        private const int VK_TAB = 0x09;
        private const int VK_BACK = 0x08;

        private const uint MAGIC_TAG = 0x555A3120;

        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc? _proc;

        private char _lastChar = ' ';

        public void ResetContext()
        {
            _lastChar = ' ';
        }

        private static bool IsModifierKey(int vk)
        {
            return vk is 0x10 or 0xA0 or 0xA1
                       or 0x11 or 0xA2 or 0xA3
                       or 0x12 or 0xA4 or 0xA5
                       or 0x5B or 0x5C
                       or 0x14 or 0x90 or 0x91;
        }

        public void Start()
        {
            _proc = HookCallback;
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            _hookId = SetWindowsHookEx(
                WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);

            if (_hookId == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    "Klaviatura xizmatini ishga tushirib boʻlmadi.");
            }
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
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                if (data.dwExtraInfo.ToUInt32() == MAGIC_TAG)
                {
                    return CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int vk = (int)data.vkCode;

                if (vk == VK_OEM_7)
                {
                    bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                    char replacement;

                    bool uiaOk = CaretContextReader.TryGetCharBeforeCaret(out char? realPrevChar);

                    if (!shift)
                    {
                        char? effective = uiaOk ? realPrevChar : _lastChar;
                        replacement = (effective == 'O' || effective == 'o' ||
                                       effective == 'G' || effective == 'g')
                            ? '\u02BB'
                            : '\u02BC';
                    }
                    else
                    {
                        bool openContext = uiaOk
                            ? (realPrevChar == null || char.IsWhiteSpace(realPrevChar.Value) ||
                               realPrevChar.Value == '(' )
                            : (_lastChar == ' ' || _lastChar == '\0' || _lastChar == '(' ||
                               _lastChar == '\n');
                        replacement = openContext ? '\u201C' : '\u201D';
                    }

                    SendUnicodeChar(replacement);
                    _lastChar = replacement;
                    return (IntPtr)1;
                }

                if (vk >= 0x41 && vk <= 0x5A)
                {
                    bool shift = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                    bool caps = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
                    bool upper = shift ^ caps;
                    _lastChar = upper ? (char)vk : char.ToLowerInvariant((char)vk);
                }
                else if (vk == VK_BACK || IsModifierKey(vk))
                {
                }
                else
                {
                    _lastChar = ' ';
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static void SendUnicodeChar(char ch)
        {
            var inputs = new INPUT[2];

            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = (UIntPtr)MAGIC_TAG
                    }
                }
            };

            inputs[1] = inputs[0];
            inputs[1].U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (sent != inputs.Length)
            {
                int error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"SendInput xatosi: {error}");
            }
        }

        #region Win32 interop

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_UNICODE = 0x0004;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public InputUnion U;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        #endregion
    }
}
