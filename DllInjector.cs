using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace DllInjectorExample
{
    public static class DllInjector
    {
        // 導入 Windows API
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EnumProcessModules(IntPtr hProcess, [Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern uint GetModuleBaseNameA(IntPtr hProcess, IntPtr hModule, StringBuilder lpBaseName, uint nSize);

        // 常數定義
        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;

        /// <summary>
        /// 將 DLL 注入到指定進程 ID
        /// </summary>
        /// <param name="processId">目標進程 ID</param>
        /// <param name="dllPath">DLL 的完整路徑</param>
        /// <param name="logCallback">用於記錄訊息的回呼函數</param>
        /// <returns>成功返回 true，失敗返回 false</returns>
        public static bool InjectDll(int processId, string dllPath, Action<string> logCallback = null)
        {
            logCallback?.Invoke($"開始注入 DLL: {dllPath} 到進程 ID: {processId}");

            // 檢查參數
            if (processId <= 0)
            {
                logCallback?.Invoke("無效的進程 ID");
                return false;
            }
            if (string.IsNullOrEmpty(dllPath) || !System.IO.File.Exists(dllPath))
            {
                logCallback?.Invoke("DLL 路徑無效或檔案不存在");
                return false;
            }

            // 1. 開啟目標進程
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                logCallback?.Invoke($"無法開啟進程，錯誤碼：{Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                // 2. 在目標進程中分配記憶體
                byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath + "\0"); // LoadLibraryW 需要 Unicode
                IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)dllPathBytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
                if (allocMemAddress == IntPtr.Zero)
                {
                    logCallback?.Invoke($"記憶體分配失敗，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 3. 將 DLL 路徑寫入目標進程的記憶體
                if (!WriteProcessMemory(hProcess, allocMemAddress, dllPathBytes, (uint)dllPathBytes.Length, out int bytesWritten))
                {
                    logCallback?.Invoke($"寫入記憶體失敗，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 4. 取得 LoadLibraryW 的位址
                IntPtr kernel32Module = GetModuleHandle("kernel32.dll");
                IntPtr loadLibraryAddr = GetProcAddress(kernel32Module, "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                {
                    logCallback?.Invoke($"無法取得 LoadLibraryW 位址，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 5. 創建遠端執行緒以調用 LoadLibraryW
                IntPtr threadId;
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out threadId);
                if (hThread == IntPtr.Zero)
                {
                    logCallback?.Invoke($"創建遠端執行緒失敗，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 6. 清理遠端執行緒句柄
                CloseHandle(hThread);
                logCallback?.Invoke("DLL 注入成功");
                return true;
            }
            finally
            {
                // 確保關閉進程句柄
                CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// 從指定進程 ID 中卸載 DLL
        /// </summary>
        /// <param name="processId">目標進程 ID</param>
        /// <param name="dllName">DLL 名稱（例如 your.dll）</param>
        /// <param name="logCallback">用於記錄訊息的回呼函數</param>
        /// <returns>成功返回 true，失敗返回 false</returns>
        public static bool EjectDll(int processId, string dllName, Action<string> logCallback = null)
        {
            logCallback?.Invoke($"開始卸載 DLL: {dllName} 從進程 ID: {processId}");

            // 檢查參數
            if (processId <= 0)
            {
                logCallback?.Invoke("無效的進程 ID");
                return false;
            }
            if (string.IsNullOrEmpty(dllName))
            {
                logCallback?.Invoke("DLL 名稱無效");
                return false;
            }

            // 1. 開啟目標進程
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                logCallback?.Invoke($"無法開啟進程，錯誤碼：{Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                // 2. 取得目標進程中的模組列表
                IntPtr[] modules = new IntPtr[1024];
                if (!EnumProcessModules(hProcess, modules, (uint)(modules.Length * IntPtr.Size), out uint bytesNeeded))
                {
                    logCallback?.Invoke($"無法列舉模組，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 3. 尋找目標 DLL 的模組句柄
                IntPtr hModule = IntPtr.Zero;
                uint moduleCount = bytesNeeded / (uint)IntPtr.Size;
                StringBuilder moduleName = new StringBuilder(260);
                for (uint i = 0; i < moduleCount; i++)
                {
                    moduleName.Clear();
                    if (GetModuleBaseNameA(hProcess, modules[i], moduleName, (uint)moduleName.Capacity) > 0)
                    {
                        if (moduleName.ToString().Equals(dllName, StringComparison.OrdinalIgnoreCase))
                        {
                            hModule = modules[i];
                            break;
                        }
                    }
                }

                if (hModule == IntPtr.Zero)
                {
                    logCallback?.Invoke($"未找到 DLL: {dllName} 在進程中");
                    return false;
                }

                // 4. 取得 FreeLibrary 的位址
                IntPtr kernel32Module = GetModuleHandle("kernel32.dll");
                IntPtr freeLibraryAddr = GetProcAddress(kernel32Module, "FreeLibrary");
                if (freeLibraryAddr == IntPtr.Zero)
                {
                    logCallback?.Invoke($"無法取得 FreeLibrary 位址，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 5. 創建遠端執行緒以調用 FreeLibrary
                IntPtr threadId;
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, freeLibraryAddr, hModule, 0, out threadId);
                if (hThread == IntPtr.Zero)
                {
                    logCallback?.Invoke($"創建遠端執行緒失敗，錯誤碼：{Marshal.GetLastWin32Error()}");
                    return false;
                }

                // 6. 清理遠端執行緒句柄
                CloseHandle(hThread);
                logCallback?.Invoke("DLL 卸載成功");
                return true;
            }
            finally
            {
                // 確保關閉進程句柄
                CloseHandle(hProcess);
            }
        }
    }
}