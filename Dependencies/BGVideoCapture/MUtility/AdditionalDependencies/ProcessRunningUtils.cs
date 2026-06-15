using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;


public class ProcessRunningUtils
{
    public static string RunExeGetOutput(string exePath, out string errOutput, string[] args, bool killMainSoDontWait = false)
    {
        string output;
        RunExe(exePath, args, out output, out errOutput, waitForExit: !killMainSoDontWait);
        return output;
    }

    public static string RunExeGetOutput(string exePath, string[] args)
    {
        string output;
        RunExe(exePath, args, out output, out string errOutput);
        return output;
    }

    public static Process RunExe(string exePath, string[] args, out string output, out string errorOutput, bool waitForExit = true, Action<string> onOutputDataReceived = null, Action<string> onErrorDataReceived = null, Action<string, string, string> onEndGeneralCheck = null, bool logStdToConsole = true, bool logStdErrorToConsole = true, bool debugLogStarting = true, string workingDir = null)
    {
        if (exePath.StartsWith("//"))
        {
            exePath = exePath.Substring(2);
            exePath = @"\\" + exePath;
        }

        Process process = new Process();
        process.StartInfo.FileName = exePath;
        var argsOnOneLine = ArgListToSingleLine(args);
        process.StartInfo.Arguments = argsOnOneLine;
        var procName = new FileInfo(exePath).Name;

        if (debugLogStarting)
            Log("RunExe " + procName + " " + process.StartInfo.Arguments + "\nfull exe path:" + exePath);

        process.StartInfo.CreateNoWindow = true;

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        if (workingDir != null)
        {
            process.StartInfo.WorkingDirectory = workingDir;
        }

        process.StartInfo.UseShellExecute = false;

        var outputDataSB = new StringBuilder();
        var errorDataSB = new StringBuilder();

        process.OutputDataReceived += (sender, e) => {
            try
            {
                if (e == null || e.Data == null) return;
                if (logStdToConsole) Log(procName + " OutputDataReceived:" + e.Data);
                if (e.Data != null) outputDataSB.AppendLine(e.Data);
                if (onOutputDataReceived != null) onOutputDataReceived(e.Data);
            }
            catch (System.Exception exception)
            {
                throw exception;
            }
        };
        process.ErrorDataReceived += (sender, e) => {
            try
            {
                if (e == null || e.Data == null) return;
                if (logStdErrorToConsole) Log(procName + " ErrorDataReceived:" + e.Data);
                if (e.Data != null) errorDataSB.AppendLine(e.Data);
                if (onErrorDataReceived != null) onErrorDataReceived(e.Data);
            }
            catch (System.Exception exception)
            {
                throw exception;
            }
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        if (waitForExit)
            process.WaitForExit();

        output = outputDataSB.ToString();
        errorOutput = errorDataSB.ToString();

        if (onEndGeneralCheck != null)
        {
            onEndGeneralCheck(procName, argsOnOneLine, errorOutput);
        }

        return process;
    }

    private static string ArgListToSingleLine(string[] args)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < args.Length; i++)
        {
            sb.Append(args[i]);
            if (i != args.Length - 1) sb.Append(" ");
        }
        return sb.ToString();
    }


    public static void RegisterOnLog(Action<string> inOnLog)
    {
        onLog = inOnLog;
    }

    private static Action<string> onLog = null;

    private static void Log(string v)
    {
        if (onLog != null)
        {
            onLog(v);
        }
        else
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(v);
#else
            Console.WriteLine(v);
#endif
        }
    }

    public static void RunDotNetProject(string dotNetProjectPath, string args, out string output, out string errOutput, bool waitForExit)
    {
        string command = "dotnet run" + " --project " + dotNetProjectPath + " " + args;
        output = RunExeGetOutput("cmd.exe", out errOutput, new[] { "/C \"" + command + "\"" }, !waitForExit);
    }
}

