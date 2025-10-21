#if !DISABLE_MBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class NativeWindowManagementHell
{
    public class AWindow
    {
        public IntPtr hWnd;
        public string windowText;
    }

    public static List<AWindow> winds = new List<AWindow>();

    const int MAXTITLE = 255;

    private delegate bool EnumDelegate(IntPtr hWnd, int lParam);

    [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool _EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int _GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);

    private static bool EnumWindowsProc(IntPtr hWnd, int lParam) {
        winds.Add(new AWindow { hWnd = hWnd, windowText = GetWindowText(hWnd) });
        return true;
    }

    public static string GetWindowText(IntPtr hWnd) {
        StringBuilder title = new StringBuilder(MAXTITLE);
        int titleLength = _GetWindowText(hWnd, title, title.Capacity + 1);
        title.Length = titleLength;

        return title.ToString();
    }

    public static List<AWindow> GetDesktopWindows()
    {
        winds = new List<AWindow>();
        EnumDelegate enumfunc = new EnumDelegate(EnumWindowsProc);
        IntPtr hDesktop = IntPtr.Zero; // current desktop
        bool success = _EnumDesktopWindows(hDesktop, enumfunc, IntPtr.Zero);

        if (success) {
            return winds.ToList();
        }
        else {
            // Get the last Win32 error code
            int errorCode = Marshal.GetLastWin32Error();

            string errorMessage = String.Format(
            "EnumDesktopWindows failed with code {0}.", errorCode);
            throw new Exception(errorMessage);
        }
    }




    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SetActiveWindow(IntPtr hWnd);



    static IntPtr windowToSetActive = IntPtr.Zero;

    public static void CacheWindowToSetActive()
    {
        windowToSetActive = GetForegroundWindow();
        if(windowToSetActive == IntPtr.Zero) {
            UnityEngine.Debug.LogError("Failed to get active window");
        }
        else {
            var str = GetWindowText(windowToSetActive);
            UnityEngine.Debug.Log("Cached active window:" + str);
        }
    }
    public static void SetActiveWindowThatWasCached()
    {
        if (windowToSetActive == IntPtr.Zero) {
            UnityEngine.Debug.LogError("No cached active window, so failing SetActiveWindowThatWasCached");
            return;
        }

        SetForegroundWindow(windowToSetActive);
        //Thread.Sleep(500);
        SetActiveWindow(windowToSetActive);
        //Thread.Sleep(500);
        ShowWindow(windowToSetActive, SW_MINIMIZE);
        //Thread.Sleep(500);
        MaximizeWindow(windowToSetActive);
    }

    public static void TryBringGameToFront(bool extraEffort = true, string overrideContains = null, bool failureIsFatal = true)
    {
        var wins = GetDesktopWindows();

        Console.WriteLine("OPEN WINDOWS:");
        foreach (var win in wins) {
            Console.WriteLine(win.hWnd + " " + win.windowText);
        }
        Console.WriteLine("END OPEN WINDOWS");


        var windowStartsWith = "Among";
        if(overrideContains != null) {
            windowStartsWith = overrideContains;
        }
        var gameWindow = wins.FirstOrDefault(x => x.windowText.StartsWith(windowStartsWith));
        if (gameWindow == null) {
            var str = "TryBringGameToFront: CANNOT FIND WINDOW with this in name:" + windowStartsWith;
            if (failureIsFatal) { 
                throw new System.Exception(str);
            }
            else {
                UnityEngine.Debug.LogWarning(str);
            }
            return;
        }

        Console.WriteLine("Trying to bring window to front:" + gameWindow.hWnd + " " + gameWindow.windowText);

       

        if (extraEffort) {
            SetForegroundWindow(gameWindow.hWnd);
            Thread.Sleep(500);
            SetActiveWindow(gameWindow.hWnd);
            Thread.Sleep(500);
            ShowWindow(gameWindow.hWnd, SW_MINIMIZE);
            Thread.Sleep(500);
            MaximizeWindow(gameWindow.hWnd);
            Thread.Sleep(500);

            UnityEngine.Screen.fullScreen = true;
            UnityEngine.Screen.SetResolution(1920, 1080, UnityEngine.FullScreenMode.FullScreenWindow, new UnityEngine.RefreshRate { numerator = 60, denominator = 1 });
        }
        else {
            SetForegroundWindow(gameWindow.hWnd);
            SetActiveWindow(gameWindow.hWnd);
        }
    }


    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string className, string windowName);


    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();


    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void MaximizeWindow(IntPtr windHandle) {
        ShowWindow(windHandle, SW_MAXIMIZE);
    }
}

#endif