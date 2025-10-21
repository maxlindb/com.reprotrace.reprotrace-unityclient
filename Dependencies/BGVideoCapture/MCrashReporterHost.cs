#if !DISABLE_MBUG
using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

public static class MCrashReporterHost
{   
    public const bool kUseEarlyCrashReporterForPreVideoReports = true;

    public static string CrashReporterExecutablePath => MBugReporter.ExecutablesRootPath +                  "/MCrashReporter" + GetExecutablePlatformFolderPostFix() + "/MCrashReporter" + GetExecutableFilePostFix();
    public static string CrashReporterCopiedRunnableExecutablePath => BGVideoCapture.temporaryDataPath +    "/MCrashReporter" + GetExecutablePlatformFolderPostFix() + "/MCrashReporter_runnable" + GetExecutableFilePostFix();

    public static string GetExecutablePlatformFolderPostFix() {
        if (BGVideoCapture.Platform == BGVideoCapture.FFMPEGPlatform.Windows_x64) return "_windows";
        if (BGVideoCapture.Platform == BGVideoCapture.FFMPEGPlatform.MacOS_x64) return "_mac";
        if (BGVideoCapture.Platform == BGVideoCapture.FFMPEGPlatform.Linux_x64) return "_linux";
        return "UNSUPPORTED_PLATFORM";
    }
    public static string GetExecutableFilePostFix() {
        return (BGVideoCapture.Platform == BGVideoCapture.FFMPEGPlatform.Windows_x64 ? ".exe" : "");
    }

    static System.Diagnostics.Process earlyCrashReporter;
    static int mCrashReporterEditorRunID;
    private static string mCrashReporterActiveFlagPath = "UNSET";

    private static bool ranEarlyStart = false;

    private static string companyName;
    private static string productName;
    private static RuntimePlatform platform;


    public static void EnsureOnAppEarlyStartCalled()
    {
        if (!ranEarlyStart) {
            OnAppEarlyStart();
        }
    }

    static string cachedDeviceInfoString = "NODEVICEINFO";

    public static void OnAppEarlyStart()
    {
        companyName = Application.companyName;
        productName = Application.productName;
        platform = Application.platform;
        BGVideoCapture.EnsureHavePathsFromMainThread();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += (x) => {
            if(x == UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                OnPlaymodeEnd();
            }            
        };
#endif

        if (Application.isEditor) {
            mCrashReporterEditorRunID = new System.Random().Next();
            mCrashReporterActiveFlagPath = Application.persistentDataPath + "/MCrashReporter_STAMP__" + mCrashReporterEditorRunID;
            MUtility.MaxinRandomUtils.StartUndyingCoroutine(CheckingPlaymodeStillActive(),dontDestroyOnLoad:true);
        }
        

        if(!Application.isEditor) { //just skip the whole prestart thing in editor. Otherwise it would have to case playmode exit and that path doesn't have that check
            if(kUseEarlyCrashReporterForPreVideoReports) {
                new Thread(() => {
                    Debug.Log("MCrashReporter_host: Starting early crash reporter");
                    earlyCrashReporter = StartReporter("prestart");
                }).Start();
            }
        }

        try {
            var deviceInfo = new DeviceInfo(true);
            cachedDeviceInfoString = JsonUtility.ToJson(deviceInfo, true);
        }
        catch (System.Exception e) {
            Debug.LogException(e);
        }

        //log some interesting things, mostly relevant to builds only
        if (!Application.isEditor) {
            MPerf.StableFPSCounter.periodicallyLogFps = true;

            new Thread(() => {
                MachineCheck();
            }).Start();

            Debug.Log("##############DEVICE INFO:##############\n" + cachedDeviceInfoString);
        }

        if (additionalMetadata == null) {
            additionalMetadata = new AdditionalBuiltInSessionMetadata();
        }
        additionalMetadata.mbugClientVersion = MBugReporter.VERSION;
        additionalMetadata.startupScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        additionalMetadata.normalStartupSceneForGame = ReproTraceClientConfiguration.Resource.normalStartupSceneForGame;
        if (!Application.isEditor) Debug.Log("Startup scene:" + additionalMetadata.startupScene+" normal:"+additionalMetadata.normalStartupSceneForGame+" is same:"+ additionalMetadata.startupScene == additionalMetadata.normalStartupSceneForGame);

        ranEarlyStart = true;
    }

    public static bool GetDidGameStartfromNormalStartupScene()
    {
        return additionalMetadata.startupScene == additionalMetadata.normalStartupSceneForGame;
    }


    public static void OnPlaymodeEnd()
    {
        playmodeActiveStamp = System.DateTime.MinValue;        
    }

    private static IEnumerator CheckingPlaymodeStillActive()
    {
        playmodeActiveStamp = System.DateTime.UtcNow;
        var t = new Thread(PlaymodeSessionActiveReporter);
        t.Name = "PlaymodeSessionActiveReporter";
        t.Start();

        var yielder = new WaitForSecondsRealtime(1f);
        while (true) {
            yield return yielder;
            playmodeActiveStamp = System.DateTime.UtcNow;            
        }
    }

    public static bool dataAxedInMiddle = false;


    private static void PlaymodeSessionActiveReporter()
    {
        while (true)
        {            
            var active = (System.DateTime.UtcNow - playmodeActiveStamp).TotalSeconds < 10 && !dataAxedInMiddle;
            if (active) {
                //Debug.Log("writing to " + mCrashReporterActiveFlagPath);
                try { File.WriteAllText(mCrashReporterActiveFlagPath, System.DateTime.UtcNow.Ticks.ToString() + "_" + BGVideoCapture.EditorOptOutInEffect ); }
                catch {
                    //Debug.Log(e); //sharing is problematic so this failing is OK even if slightly rare
                }                 
            }
            else {
                if (File.Exists(mCrashReporterActiveFlagPath)) {
                    File.Delete(mCrashReporterActiveFlagPath);
                }
                return;
            }
            Thread.Sleep(100);
        }
    }

    private static void MachineCheck()
    {
        int cnt = 0;
        while (true) {
            try {
                Debug.Log("running MachineCheck");
                var resp = System.Net.WebRequest.Create("https://api.ipify.org/").GetResponse();
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                Debug.Log("MachineCheck: Got IP:" + responseFromServer);
                break;
            }

            catch (System.Exception e) {
                Debug.LogWarning("MachineCheck fail, retrying later. "+e);
                cnt++;
                Thread.Sleep((int)System.Math.Pow(16, 2 + (cnt * 0.3f)));
            }
        }
    }

    static bool copiedOnce = false;

    public enum CrashReporterVisibilityMode { Auto = 0, Visible = 1, Hidden = 2, Minimized = 3 }

    public static System.Diagnostics.Process StartReporter(string bgVideoCapFolder = "", string sessionLocalFolder = null)
    {
        //crazy uncle AV Avast shouting at clouds check
        //(it will toss a security popup and delete the executable automatically. Abort in case it's installed, till Avast gets its stuff together. Why does it trust the game if not the companion executable?!)
        var avastPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "/Avast Software";
        var avastMightBeInUse = Directory.Exists(avastPath);

        bool isEditor = false;
#if UNITY_EDITOR
        isEditor = true;
#endif
        if(!isEditor && avastMightBeInUse) {
            Debug.LogError("Overly sensitive AV might be in use. Not launching crash reporter. Logs won't be available from this machine.");
            var tempPath = System.IO.Path.GetTempPath()+"/overly_sensitive_AV_Abort.txt";
            File.WriteAllText(tempPath,"avast");
            DumpExtraDataToVideoFolder(tempPath, deleteFileAfter:true);
            return null;
        }


        var startInfo = new System.Diagnostics.ProcessStartInfo();

        var doCopyingTrick = true;

        if (BGVideoCapture.Platform != BGVideoCapture.FFMPEGPlatform.Windows_x64)
            doCopyingTrick = false;

        var noCopyPath = new DirectoryInfo(BGVideoCapture.dataPath).Parent.FullName + "/noCopy.txt";
        if (File.Exists(noCopyPath))
            doCopyingTrick = false;

        if (doCopyingTrick)
        {
            startInfo.FileName = CrashReporterCopiedRunnableExecutablePath;
            try {
                //if this is not done, user will get security popup which which is really unwanted
                if (!copiedOnce) {
                    copiedOnce = true;
                    //Debug.Log("Copying MCrashReporter_runnable.exe to a runnable location");
                    new FileInfo(CrashReporterCopiedRunnableExecutablePath).Directory.Create();
                    File.WriteAllBytes(CrashReporterCopiedRunnableExecutablePath, File.ReadAllBytes(CrashReporterExecutablePath));
                }
            }
            //sharing violation very likely means another client, or editor is already running the copied version which is fine (in all cases but the actual reporter being different version ... )
            catch(System.IO.IOException ieE) {
                if(!ieE.Message.Contains("Sharing violation")) {
                    throw ieE;
                }
            }
        }
        else {
            startInfo.FileName = CrashReporterExecutablePath;
        }

        CrashReporterVisibilityMode vizMode = CrashReporterVisibilityMode.Hidden;        
        
        
        var visibleReporterFlagPath = new DirectoryInfo(BGVideoCapture.dataPath).Parent.FullName + "/visibleReporter.txt";
        if(File.Exists(visibleReporterFlagPath)) {
            vizMode = CrashReporterVisibilityMode.Visible;
        }
        
        //vizMode = CrashReporterVisibilityMode.Visible;


        Log("MCrashReporterHost: vizMode:"+ vizMode);


        if (vizMode == CrashReporterVisibilityMode.Visible)
        {            
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            startInfo.CreateNoWindow = false;
        }
        else if (vizMode == CrashReporterVisibilityMode.Hidden)
        {            
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
        }
        else if (vizMode == CrashReporterVisibilityMode.Minimized)
        {
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
            startInfo.CreateNoWindow = false;
        }

        var thisProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;

        startInfo.Arguments =
              "\"" + thisProcessID + "\" "
            + "\"" + bgVideoCapFolder + "\" "
            + "\"" + mCrashReporterActiveFlagPath + "\" "
            //+ (Application.isEditor ? "UnityEditor " : "Build ")
            + "\"" + platform.ToString() + "\" "
            + "\"" + companyName + "\" "
            + "\"" + productName + "\" "
            + "\"" + MChangesetIdentification.GetShortChangesetHash() + "\" "
            + "\"" + vizMode.ToString() + "\" "

            + "\"" + true + "\" "
            + "\"" + false + "\" "
            + "\"" + false + "\" "
            + "\"" + MBugCustomBackEndUploader.Domain + "\" "
            + "\"" + BGVideoCapture.FileNameFriendlyAppProductName + "\" "
            + "\"" + GetAuthBlobBase64() + "\" "
            + "\"" + sessionLocalFolder + "\"";
        


        Log("firing executable with args:" + startInfo.Arguments);
        Log("said executble path:" + startInfo.FileName);
        var proc = System.Diagnostics.Process.Start(startInfo);
        return proc;
    }

    private static string GetAuthBlobBase64()
    {
        List<string> auths = new List<string>
        {
            //TODO
            ReproTraceClientConfiguration.Resource.projectAPIToken
        };

        var pass = "76m3fmzbrb77sd3pnpde1x9jny89bsy1";

        var combined = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.Join("_", auths)));

        var blob = MaxinRandomUtils.EncryptSomething(combined, pass);

        DebugBlob(blob);
        
        return blob;
    }

    private static void DebugBlob(string blob)
    {
        var things = SeeAuth(blob);
        //Debug.Log("DEBUG BLOB:\nblob:\n"+blob+"\n\n" + MaxinRandomUtils.PrintableList(things));
    }

    private static string[] SeeAuth(string blob)
    {
        var iss = MaxinRandomUtils.DecryptSomething(blob, "76m3fmzbrb77sd3pnpde1x9jny89bsy1");
        return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(iss)).Split("_");
    }

    private static void Log(string v)
    {        
#if !UNITY_EDITOR
        Debug.Log(v);
#endif
    }

    private static void KillEarlyCrashReporter() {
        if (earlyCrashReporter != null) {
            Log("MCrashReporter_host: Killing early crash reporter");
            earlyCrashReporter.Kill();
        }
        else {
            Log("MCrashReporter_host: no early crash reporter to kill");
        }
    }

    public static string currentDropboxFolder;
    private static System.DateTime playmodeActiveStamp;

    private static void StartNormalReporter(string videoCaptureFolder, string sessionLocalFolder)
    {
        Log("MCrashReporter_host: Starting normal (after-mainscene-loaded) crash reporter");
        StartReporter(videoCaptureFolder, sessionLocalFolder);
    }


    public static void OnVideoCaptureStarted(string sessionDepoFolder, string sessionLocalFolder)
    {
        if (!ranEarlyStart) {
            Debug.LogError("MCrashReporterHost: OnVideoCaptureStarted called before OnEarlyStart. Please call OnEarlyStart earlier. Errors might occur.");
        }

        currentDropboxFolder = sessionDepoFolder;
        
        new Thread(() => {
            KillEarlyCrashReporter();
            StartNormalReporter(sessionDepoFolder, sessionLocalFolder);
        }).Start();


        additionalMetadata.timeDeltaFromInitToRecordingStart = BGVideoCapture.undyingInstance.GetTimeDeltaFromInitToRecordingStart();
        additionalMetadata.exactVideoRecordingStartTimeUTCTicks = BGVideoCapture.undyingInstance.TimeRecordingStarted.Ticks;

        DumpDeviceInfoToVideoFolder(sessionDepoFolder);
        SendCurrentExtraMetadata(); //has startup scene cached

        InitStreamedDataSending();        
    }
    

    private static void DumpDeviceInfoToVideoFolder(string videoCaptureFolder)
    {
        var tempPath = Path.GetTempPath() + "/" + new DirectoryInfo(videoCaptureFolder).Name + "_DeviceInfo.txt";
        File.WriteAllText(tempPath, cachedDeviceInfoString);

        DumpExtraDataToVideoFolder(tempPath, true);
    }

    static AdditionalBuiltInSessionMetadata additionalMetadata = null;    

    private static void SendCurrentExtraMetadata()
    {
        if (additionalMetadata == null)
            return;

        var tempPath = Path.GetTempPath() + "/additionalBuiltInMetadata.txt";
        File.WriteAllText(tempPath, JsonUtility.ToJson(additionalMetadata, true));

        DumpExtraDataToVideoFolder(tempPath);
    }

    public static void DumpExtraDataToVideoFolder(string extraFilePath, bool deleteFileAfter = false)
    {
        if (string.IsNullOrEmpty(currentDropboxFolder))
            return;

        new Thread(() => {
            BGVideoCapture.undyingInstance.SetSessionContentReadyToUpload(extraFilePath, deleteFileAfter);
        }).Start();
    }


    
    private static void InitStreamedDataSending()
    {
        dataFlushTimer = new Timer(FlushStreamedData, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(2d)); //delay first send a bit too due to order of exec causing the folder possibly not even existing otherwise
    }

    static int batchCount = -1;
    private static Timer dataFlushTimer;

    private static void FlushStreamedData(object state)
    {
        try
        {
            batchCount++;

            var dataBatch = new MBugStreamedDataBatch();
            dataBatch.batchNumber = batchCount;
            dataBatch.logEntries = MBugLogStreamer.PopQueue();
            dataBatch.framesData = MBugFrameTimingStreamer.PopQueue();

            BGVideoCapture.undyingInstance.SetSessionContentReadyToUnloadStreamedDataBatch(dataBatch);
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    public static void OnDying()
    {
        if(dataFlushTimer != null) {
            dataFlushTimer.Change(Timeout.Infinite, Timeout.Infinite);
            var t = new Thread(() =>
            {
                FlushStreamedData(null);
                BGVideoCapture.WaitTillNextSendablesAttempt();
            });
            t.Start();
            if (!t.Join(200)) Debug.LogWarning("Did not get verification of flushing streamed data in 200ms, releasing");
        }
    }


    //This is crude and should be revisited. Best option would probably be maybe similar to how this works in editor
    public static void StopInMiddle()
    {
        Debug.LogWarning("MBug: data axed in middle of session");
        PlayerPrefs.DeleteKey("MBugReporterConsent");
        BGVideoCapture.undyingInstance.enabled = false;
        dataAxedInMiddle = true;
    }
}
#endif