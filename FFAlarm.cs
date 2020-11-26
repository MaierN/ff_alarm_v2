using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Media;

namespace ff_alarm
{
    class ProcessMemoryReadWriteException : Exception {}

    class FFAlarm
    {

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        static bool DEBUG = false;

        static string MESSAGE_ENEMY_FF = "<font color=\"#FFA500\">Enemy team agreed to a surrender";

        static Process leagueProcess;

        static byte[] readMemory(long address, int size)
        {
            byte[] resBuffer = new byte[size];
            ReadProcessMemory(leagueProcess.Handle, (IntPtr)address, resBuffer, size, out IntPtr readSize);
            if ((int)readSize == size) return resBuffer;
            else throw new ProcessMemoryReadWriteException();
        }

        static void writeMemory(long address, byte[] buffer)
        {
            WriteProcessMemory(leagueProcess.Handle, (IntPtr)address, buffer, buffer.Length, out IntPtr wroteSize);
            if ((int)wroteSize == buffer.Length) return;
            else throw new ProcessMemoryReadWriteException();
        }

        static string readString(long address)
        {
            string res = "";
            for (int i = 0; ; i++)
            {
                byte currChar = readMemory(address + i, 1)[0];
                if (currChar == 0) break;
                else res += (char)currChar;
            }
            return res;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== FF alarm ===");

            for (; ; )
            {
                try
                {
                    Process[] processes = new Process[0];

                    while (processes.Length == 0)
                    {
                        processes = Process.GetProcessesByName("League of Legends");

                        if (processes.Length == 0)
                        {
                            Console.WriteLine("Could not find process \"League of Legends\", retrying in 5 second");
                            Thread.Sleep(5000);
                        }
                    }

                    leagueProcess = processes[0];
                    Console.WriteLine("League of Legends pid: " + leagueProcess.Id);

                    int leagueBaseAddress = leagueProcess.MainModule.BaseAddress.ToInt32();
                    if (DEBUG) Console.WriteLine("Base address: 0x" + leagueBaseAddress.ToString("X"));

                    int leagueClientChatPtr = leagueBaseAddress + 0x1C67F30;
                    if (DEBUG) Console.WriteLine("ClientChatPtr: 0x" + leagueClientChatPtr.ToString("X"));

                    int leagueClientChat = BitConverter.ToInt32(readMemory(leagueClientChatPtr, 4), 0);
                    if (DEBUG) Console.WriteLine("leagueClientChat: 0x" + leagueClientChat.ToString("X"));

                    int leagueClientChatUiPtr = leagueClientChat + 0x4;
                    if (DEBUG) Console.WriteLine("leagueClientChatUiPtr: 0x" + leagueClientChatUiPtr.ToString("X"));

                    int leagueClientChatUi = BitConverter.ToInt32(readMemory(leagueClientChatUiPtr, 4), 0);
                    if (DEBUG) Console.WriteLine("leagueClientChatUi: 0x" + leagueClientChatUi.ToString("X"));

                    int leagueNumberMessages = leagueClientChatUi + 0xB4;
                    if (DEBUG) Console.WriteLine("leagueNumberMessages: 0x" + leagueNumberMessages.ToString("X"));

                    int leagueNextMessageId = leagueClientChatUi + 0xB0;
                    if (DEBUG) Console.WriteLine("leagueNextMessageId: 0x" + leagueNextMessageId.ToString("X"));

                    int leagueIsChatOpen = leagueClientChatUi + 0x62C;
                    if (DEBUG) Console.WriteLine("leagueIsChatOpen: 0x" + leagueIsChatOpen.ToString("X"));

                    int leagueIsChatFocused = leagueClientChatUi + 0x645;
                    if (DEBUG) Console.WriteLine("leagueIsChatFocused: 0x" + leagueIsChatFocused.ToString("X"));

                    int leagueChatInputText = leagueClientChatUi + 0x5E4;
                    if (DEBUG) Console.WriteLine("leagueChatInputText: 0x" + leagueChatInputText.ToString("X"));

                    int leagueMessageList = leagueClientChatUi + 0xB8;

                    int oldNextMessageId = 0;

                    for (int j = 0; ; j++)
                    {

                        int numberMessages = BitConverter.ToInt32(readMemory(leagueNumberMessages, 4), 0);
                        if (DEBUG) Console.WriteLine("numberMessages: " + numberMessages);

                        int nextMessageId = BitConverter.ToInt32(readMemory(leagueNextMessageId, 4), 0);
                        if (DEBUG) Console.WriteLine("nextMessageId: " + nextMessageId);

                        for (int i = 0; i < numberMessages; i++)
                        {
                            int leagueMessagePtr = leagueMessageList + i * 0xC;
                            int leagueMessage = BitConverter.ToInt32(readMemory(leagueMessagePtr, 4), 0);
                            string message = readString(leagueMessage);
                            if (DEBUG) Console.WriteLine(message);

                            if (i == oldNextMessageId && oldNextMessageId != nextMessageId)
                            {
                                Console.Write("Last message (" + oldNextMessageId + "): ");
                                Console.WriteLine(message);
                                oldNextMessageId += 1;
                                oldNextMessageId %= 100;
                            }

                            if (message.Contains(MESSAGE_ENEMY_FF))
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    Console.WriteLine("FF ALARM!");
                                    SoundPlayer simpleSound = new SoundPlayer(@"alarm_sound.wav");
                                    simpleSound.Play();

                                    SetForegroundWindow(leagueProcess.MainWindowHandle);
                                    Thread.Sleep(200);
                                    while (!BitConverter.ToBoolean(readMemory(leagueIsChatOpen, 1), 0))
                                    {
                                        Keyboard.sendKey(0x1C);
                                    }

                                    writeMemory(leagueChatInputText, new byte[] {
                                        0x2F, 0x00, 0x66, 0x00, // '/' 'f'
                                        0x66, 0x00, 0x00, 0x00, // 'f' NULL
                                        0x00, 0x00, 0x00, 0x00, // NULL NULL
                                        0x00, 0x00, 0x00, 0x00, // NULL NULL
                                        0x00, 0x00, 0x00, 0x00, // NULL NULL
                                        0x07, 0x00, 0x00, 0x00, // 7
                                    });
                                    Thread.Sleep(100);
                                    
                                    Keyboard.sendKey(0x1C);

                                    Thread.Sleep(400);
                                }

                                Console.WriteLine("Press enter to continue");
                                Console.ReadLine();
                            }
                        }

                        Thread.Sleep(10);
                    }
                }
                catch (ProcessMemoryReadWriteException)
                {
                    Console.WriteLine("Process memory read/write exception, restarting in 5 second");
                    Thread.Sleep(5000);
                }
            }
        }
    }
}
