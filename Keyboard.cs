using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ff_alarm
{
    // codes available here: https://www.win.tue.nl/~aeb/linux/kbd/scancodes-1.html
    public class Keyboard
    {
        public static void pressKeyScan(ushort scanCode)
        {
            INPUT input = new INPUT();
            input.Type = 1;
            input.Data.Keyboard.Vk = 0;
            input.Data.Keyboard.Scan = scanCode;
            input.Data.Keyboard.Flags = 8;

            SendInput(1, new INPUT[] {input}, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void releaseKeyScan(ushort scanCode)
        {
            INPUT input = new INPUT();
            input.Type = 1;
            input.Data.Keyboard.Vk = 0;
            input.Data.Keyboard.Scan = scanCode;
            input.Data.Keyboard.Flags = 0x8 | 0x2;

            SendInput(1, new INPUT[] {input}, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void sendKey(ushort keyID, int waitTime=60)
        {
            pressKeyScan(keyID);
            Thread.Sleep(waitTime);
            releaseKeyScan(keyID);
            Thread.Sleep(waitTime);
        }

        public static void sendKeyWithModifier(ushort keyID, ushort modifierID, int waitTime=60)
        {
            pressKeyScan(modifierID);
            Thread.Sleep(waitTime);
            sendKey(keyID, waitTime);
            releaseKeyScan(modifierID);
            Thread.Sleep(waitTime);
        }

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT
        {
            public uint Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }
        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct HARDWAREINPUT
        {
            public uint Msg;
            public ushort ParamL;
            public ushort ParamH;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            public ushort Vk;
            public ushort Scan;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }
    }
}
