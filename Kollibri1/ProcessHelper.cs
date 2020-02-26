using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Nokk
{
    public class ProcessWindow
    {
        public string WindowTitle { get; private set; }
        public Process Process { get; private set; }

        public ProcessWindow(string windowTitle, Process process)
        {
            WindowTitle = windowTitle;
            Process = process;
        }
    }

    public static class ProcessHelper
    {
        /// <summary>
        /// Возвращает массив приложений запущенных пользователем
        /// Из результата исклюлючаются текущий процесс и explorer
        /// </summary>
        public static ProcessWindow[] GetRunningApplications()
        {
            var allProccesses = Process.GetProcesses();
            var myPid = Process.GetCurrentProcess().Id;
            var explorerPids = allProccesses.Where(p => "explorer".Equals(p.ProcessName, StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToArray();
            var windows = new List<ProcessWindow>();
            EnumDelegate filter = delegate (IntPtr hWnd, int lParam)
            {
                var sbTitle = new StringBuilder(255);
                GetWindowText(hWnd, sbTitle, sbTitle.Capacity + 1);
                string windowTitle = sbTitle.ToString();

                if (!string.IsNullOrEmpty(windowTitle) && IsWindowVisible(hWnd))
                {
                    int pid;
                    GetWindowThreadProcessId(hWnd, out pid);
                    if (pid != myPid && !explorerPids.Contains(pid))
                    {
                        windows.Add(new ProcessWindow(windowTitle, allProccesses.FirstOrDefault(p => p.Id == pid)));
                    }
                }

                return true;
            };

            EnumDesktopWindows(IntPtr.Zero, filter, IntPtr.Zero);
            return windows.ToArray();
        }

        delegate bool EnumDelegate(IntPtr hWnd, int lParam);

        [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", EntryPoint = "GetWindowText",
        ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
    }
}
