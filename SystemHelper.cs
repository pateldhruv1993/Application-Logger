using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class SystemHelper {

	// System calls
	[DllImport("User32.dll")]
	private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

	[DllImport("Kernel32.dll")]
	private static extern uint GetLastError();

	[DllImport("user32.dll", CharSet=CharSet.Auto)]
	public static extern bool IsWindowVisible(IntPtr hWnd);

	[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
	public static extern IntPtr GetForegroundWindow();

	public static uint GetIdleTime() {
		LASTINPUTINFO lastInPut = new LASTINPUTINFO();
		lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
		GetLastInputInfo(ref lastInPut);

		return ((uint)Environment.TickCount - lastInPut.dwTime);
	}

	public static long GetTickCount() {
		return Environment.TickCount;
	}

	public static long GetLastInputTime() {
		LASTINPUTINFO lastInPut = new LASTINPUTINFO();
		lastInPut.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInPut);
		if (!GetLastInputInfo(ref lastInPut)) {
			throw new Exception(GetLastError().ToString());
		}

		return lastInPut.dwTime;
	}


    public static void GetPowerCfgOutput()
    {
        try
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = "powercfg";
            p.StartInfo.Arguments = "/requests";
            p.Start();

            string output = "";
            while (!p.HasExited)
            {
                output += p.StandardOutput.ReadToEnd();
            }
            p.WaitForExit();
        }
        catch (Exception err)
        {

        }
    }

	internal struct LASTINPUTINFO {
		public uint cbSize;
		public uint dwTime;
	}
}