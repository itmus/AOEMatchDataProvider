﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AOEMatchDataProvider.Services.Default
{
    //low level global key capture: https://stackoverflow.com/a/604417
    internal sealed class KeyHookService : IKeyHookService, IDisposable
    {
        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;
        static LowLevelKeyboardProc _proc = HookCallback;
        static IntPtr _hookID = IntPtr.Zero;

        static Dictionary<string, KeyHandlerPair> handlers;
        static List<string> tokens;

        ILogService LogService { get; }

        internal KeyHookService(ILogService logService)
        {
            LogService = logService;

            _hookID = SetHook(_proc);

            handlers = new Dictionary<string, KeyHandlerPair>();
            tokens = new List<string>();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Keys key = (Keys)Marshal.ReadInt32(lParam);

                //make copy of original collection
                var handlersCopy = new Dictionary<string, KeyHandlerPair>(handlers);

                //iterate over all owners
                foreach (var kvp in handlersCopy)
                {
                    if (kvp.Value.key == key)
                    {
                        kvp.Value.action.Invoke();
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region extern
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }

        public string Add(Keys keys, Action action)
        {
            var token = RandomString(7);

            if (tokens.Contains(token))
                return Add(keys, action);

            LogService.Trace($"Hooking action key: {keys} ({token})");

            handlers.Add(
                token,
                new KeyHandlerPair()
                {
                    key = keys,
                    action = action
                }
            );

            return token;
        }

        public void Remove(string token)
        {
            if (!handlers.ContainsKey(token))
                return;

            LogService.Trace($"Unhooking action assigned with key: {handlers[token].key} ({token})");

            handlers.Remove(token);
            tokens.Remove(token);
        }

        //https://stackoverflow.com/a/1344242
        static Random random = new Random();
        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public class KeyHandlerPair
        {
            //object owner;
            public Keys key;
            public Action action;
        }
    }
}
