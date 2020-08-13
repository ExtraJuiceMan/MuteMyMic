using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;

namespace MuteMyMic
{
    public static class KeyboardHook
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;

        private static IntPtr hookIdKeyboard = IntPtr.Zero;
        private static IntPtr hookIdMouse = IntPtr.Zero;

        private static bool hasHooked = false;

        // this is required to prevent our delegate passed
        // to SetWindowsHookEx from being GC'd
        private static readonly LowLevelProc procDelegateKeyboard = HookCallbackKeyboard;
        private static readonly LowLevelProc procDelegateMouse = HookCallbackMouse;

        public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static event OnKeyDown OnKeyDownEvent;
        public static event OnKeyUp OnKeyUpEvent;

        public delegate void OnKeyDown(int key);
        public delegate void OnKeyUp(int key);

        public static void Hook()
        {
            if (hasHooked)
                return;

            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule;

            hookIdKeyboard = SetWindowsHookEx(WH_KEYBOARD_LL, procDelegateKeyboard, GetModuleHandle(curModule.ModuleName), 0);
            hookIdMouse = SetWindowsHookEx(WH_MOUSE_LL, procDelegateMouse, GetModuleHandle(curModule.ModuleName), 0);
        }

        public static void Unhook()
        {
            if (hasHooked)
            {
                UnhookWindowsHookEx(hookIdKeyboard);
                UnhookWindowsHookEx(hookIdMouse);
            }

            hookIdKeyboard = IntPtr.Zero;
            hookIdMouse = IntPtr.Zero;
        }

        private static IntPtr HookCallbackKeyboard(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // keycode is at start of lParam struct so just read an int32
                var key = Marshal.ReadInt32(lParam);

                switch ((int)wParam)
                {
                    case WM_KEYDOWN:
                        OnKeyDownEvent?.Invoke(key);
                        break;
                    case WM_KEYUP:
                        OnKeyUpEvent?.Invoke(key);
                        break;
                    default:
                        break;
                }
            }

            return CallNextHookEx(hookIdKeyboard, nCode, wParam, lParam);
        }

        private static IntPtr HookCallbackMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // read hiword from mouseData field in MSLLHOOKSTRUCT 
                var mouseKey = Marshal.ReadInt32(lParam + 8) >> 16;

                switch ((int)wParam)
                {
                    case WM_XBUTTONDOWN:
                        OnKeyDownEvent?.Invoke(XButtonToVK(mouseKey));
                        break;
                    case WM_XBUTTONUP:
                        OnKeyUpEvent?.Invoke(XButtonToVK(mouseKey));
                        break;
                    default:
                        break;
                }
            }

            return CallNextHookEx(hookIdMouse, nCode, wParam, lParam);
        }

        // keep keycodes consistent
        private static int XButtonToVK(int xbutton)
        {
            if (xbutton == 1)
                return 0x05;
            if (xbutton == 2)
                return 0x06;

            return -1;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
