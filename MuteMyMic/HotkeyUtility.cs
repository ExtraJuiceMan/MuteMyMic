using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace MuteMyMic
{
    public static class HotkeyUtility
    {
        public static string FormatHotkeys(int[] hotkeys) =>
            String.Join(" + ", hotkeys.Select(x => {
                if (x == 0x05)
                    return "XBUTTON1";
                if (x == 0x06)
                    return "XBUTTON2";
                return KeyInterop.KeyFromVirtualKey(x).ToString();
                }));
    }
}
