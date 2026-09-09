using System.Runtime.InteropServices;
using AutoTyper.Core.Input;

namespace AutoTyper.Harness;

/// <summary>
/// Test-only helper: synthesizes a modifier+key combo press via SendInput so
/// the Phase 4 hotkey test can simulate a user pressing a global hotkey
/// without any real input device. Separate from <see cref="WinInputKeySender"/>,
/// which sends literal Unicode characters rather than virtual-key combos.
/// </summary>
internal static class HotkeySimulator
{
    private const int InputKeyboard = 1;
    private const uint KeyEventFKeyUp = 0x0002;

    private const ushort VkControl = 0x11;
    private const ushort VkMenu = 0x12;
    private const ushort VkShift = 0x10;
    private const ushort VkLeftWin = 0x5B;

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
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
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void PressCombo(HotkeyCombo combo)
    {
        var vkKeys = new List<ushort>();
        if (combo.Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            vkKeys.Add(VkControl);
        }

        if (combo.Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            vkKeys.Add(VkMenu);
        }

        if (combo.Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            vkKeys.Add(VkShift);
        }

        if (combo.Modifiers.HasFlag(HotkeyModifiers.Meta))
        {
            vkKeys.Add(VkLeftWin);
        }

        if (!WindowsVirtualKeyMap.TryGetVirtualKey(combo.Key, out ushort virtualKey))
        {
            throw new InvalidOperationException($"{combo.Key} has no Windows virtual-key equivalent.");
        }

        vkKeys.Add(virtualKey);

        INPUT[] downs = vkKeys.Select(vk => MakeInput(vk, keyUp: false)).ToArray();
        INPUT[] ups = ((IEnumerable<ushort>)vkKeys).Reverse().Select(vk => MakeInput(vk, keyUp: true)).ToArray();
        INPUT[] all = downs.Concat(ups).ToArray();

        int size = Marshal.SizeOf<INPUT>();
        uint sent = SendInput((uint)all.Length, all, size);
        if (sent != all.Length)
        {
            int error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"SendInput sent {sent}/{all.Length} events. Win32 error: {error}");
        }
    }

    private static INPUT MakeInput(ushort vk, bool keyUp) => new()
    {
        type = InputKeyboard,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                wScan = 0,
                dwFlags = keyUp ? KeyEventFKeyUp : 0,
                time = 0,
                dwExtraInfo = IntPtr.Zero,
            },
        },
    };
}
