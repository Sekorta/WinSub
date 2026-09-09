using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinSub
{
    public static class GlobalMediaHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const int VK_MEDIA_NEXT_TRACK = 0xB0;
        private const int VK_MEDIA_PREV_TRACK = 0xB1;
        private const int VK_MEDIA_STOP = 0xB2;

        private static IntPtr _hookId = IntPtr.Zero;
        private static NativeHookProc _hookProc;

        public static event Action MediaPlayPause;
        public static event Action MediaNext;
        public static event Action MediaPrev;
        public static event Action MediaStop;

        private delegate IntPtr NativeHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, NativeHookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static void Start()
        {
            if (_hookId != IntPtr.Zero) return;
            _hookProc = HookCallback;
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                int vk = Marshal.ReadInt32(lParam);

                bool isKeyDown = (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN);
                bool isKeyUp = (msg == WM_KEYUP || msg == WM_SYSKEYUP);

                if (isKeyDown || isKeyUp)
                {
                    switch (vk)
                    {
                        case VK_MEDIA_PLAY_PAUSE:
                            if (isKeyDown && MediaPlayPause != null) MediaPlayPause();
                            return (IntPtr)1;
                        case VK_MEDIA_NEXT_TRACK:
                            if (isKeyDown && MediaNext != null) MediaNext();
                            return (IntPtr)1;
                        case VK_MEDIA_PREV_TRACK:
                            if (isKeyDown && MediaPrev != null) MediaPrev();
                            return (IntPtr)1;
                        case VK_MEDIA_STOP:
                            if (isKeyDown && MediaStop != null) MediaStop();
                            return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }
    }
}
