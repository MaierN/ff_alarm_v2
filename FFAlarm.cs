using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace ff_alarm
{
    class FFAlarm
    {

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);
        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        static bool DEBUG = false;

        static long MIN_ADDRESS = 0x0;
        static long MAX_ADDRESS = 0x7FFFFFFF;

        static string MESSAGE_ENEMY_FF = "Enemy team agreed to a surrender";

        static Process leagueProcess;

        static string byteArrayToString(byte[] bs) {
            string r = "";
            foreach (byte b in bs) {
                r += b.ToString("X2") + " ";
            }
            return r;
        }

        static byte[] readMemory(long address, int size) {
            byte[] resBuffer = new byte[size];
            ReadProcessMemory(leagueProcess.Handle, (IntPtr)address, resBuffer, size, out IntPtr readSize);
            return (int)readSize == size ? resBuffer : null;
        }

        static string readString(long address) {
            string res = "";
            for (int i = 0; ; i++) {
                byte currChar = readMemory(address + i, 1)[0];
                if (currChar == 0) break;
                else res += (char)currChar;
            }
            return res;
        }

        static void scanForBytes(byte[] search, int alignOn, int alignStart) {
            for (long i = alignStart; i <= MAX_ADDRESS; i += alignOn) {
                if (i % 0x1000000 == 0) {
                    Console.WriteLine("Scanning... (0x" + i.ToString("X8") + "/0x" + MAX_ADDRESS.ToString("X8") + ")");
                }
                byte[] res = readMemory(i, search.Length);
                if (res == null) continue;
                bool isEqual = true;
                for (int j = 0; j < search.Length; j++) {
                    if (search[j] != res[j]) {
                        isEqual = false;
                        break;
                    }
                }
                if (isEqual) {
                    string resJoined = "";
                    for (int j = 0; j < res.Length; j++) {
                        resJoined += "0x" + res[j].ToString("X2") + " ";
                    }
                    Console.WriteLine("Found at address 0x" + i.ToString("X") + ": " + resJoined);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("- Go");

            Process[] processes = Process.GetProcessesByName("League of Legends");

            if (processes.Length == 0)
            {
                Console.WriteLine("Could not find process \"League of Legends\"");
                Console.ReadLine();
                return;
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

            int leagueMessageList = leagueClientChatUi + 0xB8;
            
            for (int j = 0;; j++) {

                int numberMessages = BitConverter.ToInt32(readMemory(leagueNumberMessages, 4), 0);
                Console.WriteLine(j + ": numberMessages: " + numberMessages);
                
                for (int i = 0; i < numberMessages; i++) {
                    int leagueMessagePtr = leagueMessageList + i*0xC;
                    int leagueMessage = BitConverter.ToInt32(readMemory(leagueMessagePtr, 4), 0);
                    string message = readString(leagueMessage);
                    if (DEBUG) Console.WriteLine(message);

                    if (message.Contains(MESSAGE_ENEMY_FF)) {
                        Console.WriteLine("FF FF FF!!!");

                        Console.ReadLine();
                        return;
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
