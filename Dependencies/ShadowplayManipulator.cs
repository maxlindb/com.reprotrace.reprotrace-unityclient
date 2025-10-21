#if !DISABLE_MBUG
using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

public class ShadowplayManipulator : Singleton<ShadowplayManipulator>, IPersistentSelfCreatingSingleton
{
    public string recordingsFolder;    

    new void Awake()
    {
        recordingsFolder = GetDefaultVideosFolder();
        if(SystemInfo.deviceName == "APUSTAJA-PC") {
            recordingsFolder = @"E:\Shadowplay_E";
        }
        if (SystemInfo.deviceName == "KEKW-REBORN") {
            recordingsFolder = @"G:\SHADOWPLOY";
        }
    }

    public static string GetDefaultVideosFolder() => System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "\\Videos";

    public bool isRecording = false;
    public string currentRecordingFile;

    public void StartRecording()
    {
        Debug.Log("ShadowplayManipulator: StartRecording");

        if (isRecording) {
            Debug.LogError("ShadowplayManipulator: Already recording!");
            return;
        }

        isRecording = true;


        var files = GetPotentialVideoFiles();

        PressCaptureButton();

        new Thread(() => CheckingIfRecordingStarts(files)).Start();
    }

    private static void PressCaptureButton()
    {
        WindowsInput.InputSimulator.SimulateKeyDown(WindowsInput.VirtualKeyCode.LMENU);
        WindowsInput.InputSimulator.SimulateKeyPress(WindowsInput.VirtualKeyCode.F9);
        WindowsInput.InputSimulator.SimulateKeyUp(WindowsInput.VirtualKeyCode.LMENU);
    }

    private void CheckingIfRecordingStarts(string[] filesBefore)
    {
        var startCheck = System.DateTime.Now;
        while (true) {
            Thread.Sleep(200);            
            var filesNow = GetPotentialVideoFiles();
            var newFiles = filesNow.Where(x => !filesBefore.Contains(x));
            if(newFiles.Count() > 1) {
                Debug.LogError("ShadowplayManipulator: More than one new file "+MaxinRandomUtils.PrintableList(newFiles));
            }
            var newFile = newFiles.FirstOrDefault();
            if(newFile != null) {
                currentRecordingFile = newFile;
                Debug.Log("ShadowplayManipulator: capture start success, shadowplay is recording to file:"+currentRecordingFile);
                if (!File.Exists(currentRecordingFile)) {
                    Debug.LogError("!File.Exists(currentRecordingFile) this does not make sense (CheckingIfRecordingStarts, after finding)");
                }
                return;
            }
            var spent = System.DateTime.Now - startCheck;
            if(spent.TotalSeconds > 30) {
                Debug.LogError("ShadowplayManipulator Failed to start recording, now new file spotted.");
                isRecording = false;
                return;
            }
        }
    }

    public string StopRecording()
    {
        if (!isRecording) {
            Debug.LogError("ShadowplayManipulator: StopRecording: was not recording!");
            return null;
        }
        if (currentRecordingFile == null) {
            Debug.LogError("ShadowplayManipulator: StopRecording: failed to start recording");
            return null;
        }

        PressCaptureButton();

        Thread.Sleep(1000);

        var fil = currentRecordingFile;
        currentRecordingFile = null;
        isRecording = false;
        Debug.Log("ShadowplayManipulator: StopRecording: assuming a proper recording is now at "+fil);
        if (!File.Exists(fil)) {
            Debug.LogError("!File.Exists(currentRecordingFile) this does not make sense (StopRecording)\nEXPECT:"+fil+"\n\nactually exist:"+ MaxinRandomUtils.PrintableList(GetPotentialVideoFiles()) + "\n\n");
        }
        return fil;
    }

    private string[] GetPotentialVideoFiles(bool dvrOnly = false)
    {
        if (dvrOnly) {
            return Directory.GetFiles(recordingsFolder, "*DVR.mp4", SearchOption.AllDirectories);
        }
        else {
            return Directory.GetFiles(recordingsFolder, "*.mp4", SearchOption.AllDirectories);
        }
    }



    public void AttemptToCaptureFlashback(Action<string> onDone)
    {
        Debug.Log("ShadowplayManipulator AttemptToCaptureFlashback");

        NativeWindowManagementHell.CacheWindowToSetActive();

        WindowsInput.InputSimulator.SimulateKeyDown(WindowsInput.VirtualKeyCode.LMENU);
        WindowsInput.InputSimulator.SimulateKeyPress(WindowsInput.VirtualKeyCode.F10);
        WindowsInput.InputSimulator.SimulateKeyUp(WindowsInput.VirtualKeyCode.LMENU);

        var files = GetPotentialVideoFiles(dvrOnly:true);
        new Thread(() => CheckingForDVRFile(files, onDone)).Start();

        MaxinRandomUtils.DoActionAfterFrames(() => {
            NativeWindowManagementHell.SetActiveWindowThatWasCached();
        }, 2f, TimeType.Seconds, dontDestroyOnLoad:true);
    }

    private void CheckingForDVRFile(string[] filesBefore, Action<string> onDone)
    {
        Debug.Log("ShadowplayManipulator CheckingForDVRFile started");

        var startCheck = System.DateTime.Now;
        while (true) {
            Thread.Sleep(200);
            var filesNow = GetPotentialVideoFiles(dvrOnly:true);
            var newFiles = filesNow.Where(x => !filesBefore.Contains(x));
            if (newFiles.Count() > 1) {
                Debug.LogError("ShadowplayManipulator: More than one new file " + MaxinRandomUtils.PrintableList(newFiles));
            }
            var newFile = newFiles.FirstOrDefault();
            if (newFile != null) {                
                Debug.Log("ShadowplayManipulator: found a new DVR file:" + newFile);
                if (!File.Exists(newFile)) {
                    Debug.LogError("!File.Exists(newFile) this does not make sense (CheckingIfRecordingStarts, after finding)");
                }

                var newFileInfo = new FileInfo(newFile);
                var lastModified = newFileInfo.LastWriteTime;

                int counter = 0;
                while (true) {
                    counter++;
                    if(newFileInfo.LastWriteTime != lastModified) {
                        lastModified = newFileInfo.LastWriteTime;
                        counter = 0;
                    }
                    if(counter > 5) {
                        break;
                    }
                    Thread.Sleep(1000);
                }

                Debug.Log("CheckingForDVRFile considering file settled:" + newFile);
                AsyncRunnerHelper.RunOnMainThread(() => {
                    onDone(newFile);
                });

                return;
            }
            var spent = System.DateTime.Now - startCheck;
            if (spent.TotalSeconds > 30) {
                Debug.LogError("CheckingForDVRFile Failed to capture DVR.");
                onDone("");
                return;
            }
        }
    }
}
#endif