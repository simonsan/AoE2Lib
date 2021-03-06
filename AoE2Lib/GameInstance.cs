﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AoE2Lib
{
    public abstract class GameInstance : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, uint lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        public bool HasExited => Process.HasExited;
        public bool IsGameRunning => GetGoals(1) != null;

        private readonly Process Process;
        private readonly IntPtr ProcessHandle;
        private bool DisposedValue;

        public GameInstance(Process process)
        {
            Process = process;
            ProcessHandle = OpenProcess(0x001F0FFF, false, Process.Id);

            if (ProcessHandle == IntPtr.Zero)
            {
                throw new IOException("Failed to get handle to process.");
            }
        }

        public abstract int[] GetGoals(int player);
        public abstract bool SetGoal(int player, int index, int value);
        public abstract bool SetGoals(int player, int start_index, params int[] values);

        public abstract int[] GetStrategicNumbers(int player);
        public abstract bool SetStrategicNumber(int player, int index, int value);
        public abstract bool SetStrategicNumbers(int player, int start_index, params int[] values);

        public bool IsPlayerInGame(int player)
        {
            return GetGoals(player) != null;
        }

        protected byte[] ReadByteArray(IntPtr addr, uint size)
        {
            if (HasExited)
            {
                throw new IOException("Process has exited.");
            }

            VirtualProtectEx(ProcessHandle, addr, (UIntPtr)size, 0x40 /* rw */, out uint protect);

            byte[] array = new byte[size];
            ReadProcessMemory(ProcessHandle, addr, array, size, 0u);

            VirtualProtectEx(ProcessHandle, addr, (UIntPtr)size, protect, out _);

            return array;
        }

        protected bool WriteByteArray(IntPtr addr, byte[] bytes)
        {
            if (HasExited)
            {
                throw new IOException("Process has exited.");
            }

            VirtualProtectEx(ProcessHandle, addr, (UIntPtr)bytes.Length, 0x40 /* rw */, out uint protect);

            bool flag = WriteProcessMemory(ProcessHandle, addr, bytes, (uint)bytes.Length, 0u);

            VirtualProtectEx(ProcessHandle, addr, (UIntPtr)bytes.Length, protect, out _);

            return flag;
        }

        protected int ReadInt32(IntPtr addr)
        {
            var bytes = ReadByteArray(addr, sizeof(int));

            return BitConverter.ToInt32(bytes, 0);
        }

        protected bool WriteInt32(IntPtr addr, int value)
        {
            var bytes = BitConverter.GetBytes(value);

            return WriteByteArray(addr, bytes);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                DisposedValue = true;

                CloseHandle(ProcessHandle);
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~GameInstance()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
