#if !DISABLE_MBUG
using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack;
#endif
using MUtility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class BGVideoCapture : MonoBehaviour
{
    public static void EnsureHavePathsFromMainThread()
    {
        streamingAssetsPath = Application.streamingAssetsPath;
        temporaryDataPath = Application.temporaryCachePath;
        dataPath = Application.dataPath;
        applicationDotTemporaryCachePath = Application.temporaryCachePath;
        applicationDotProductName = Application.productName;
        applicationDotCompanyName = Application.companyName;
#if !DISABLE_MBUG
        _cached = ReproTraceClientConfiguration.Resource;
        if(_cached == null) {
            Debug.LogError("MBugReporter: No ReproTraceClientConfiguration asset found. Please create one from \"Tools/ReproTrace/Create configuration asset\", and configure it.");
        }
#endif
    }


    public static string streamingAssetsPath;
    public static string persistentDataPath;
    public static string temporaryDataPath;
    public static string dataPath;
    public static string applicationDotTemporaryCachePath;
    public static string applicationDotProductName;
    public static string applicationDotCompanyName;


#if !DISABLE_MBUG

    [System.Serializable]
    public class BGVideoCaptureConfig
    {
        public int frameWidth = 640;
        public float fps = 30;
        public int maxCompressionThreads = 1;
        public int bitrateKbps = 500;
    }

    BGVideoCaptureConfig config = null;


    float segmentTargetLength = 10f;

    static string sessionID;
    int segmentIndex = -1;
    int segmentFrameNum = 0;

    int segmentStartFrame;

    public float FPS => config.fps;
    public static int TotalFrameNum { get; private set; }
    System.DateTime timeRecordingStarted;
    public System.DateTime TimeRecordingStarted => timeRecordingStarted;

    public double GetTimeDeltaFromInitToRecordingStart()
    {
        return (timeRecordingStarted - startTime).TotalSeconds;
    }


    public static string machineIDExtraPostFix = "";

#endif
    public static bool ShouldSelfDisable {
        get {
#if DISABLE_MBUG
            return true;
        }
    }
#else
            if (!m_ShouldSelfDisable.HasValue)
            {
                m_ShouldSelfDisable = Platform != FFMPEGPlatform.Windows_x64 && Platform != FFMPEGPlatform.MacOS_x64;
                //m_ShouldSelfDisable = Platform != FFMPEGPlatform.Windows_x64; //swap to this if this doesn't work on real mac hardware yet

                if (!m_ShouldSelfDisable.Value)
                {
                    var preventFilePath = new System.IO.DirectoryInfo(Application.dataPath).Parent.FullName + "/disablebugreporter.txt";
                    var blackListedMachines = new List<string>();

                    if (Application.isEditor) {
                        //blackListedMachines.Add("APUSTAJA-PC");                                        
                    }

                    m_ShouldSelfDisable = System.IO.File.Exists(preventFilePath) || blackListedMachines.Contains(SystemInfo.deviceName);

                    if(Application.isEditor && PlayerPrefs.GetInt("MBugReporter_EditorBreakWholeSystemOptOutEnabled") == 1) {
                        Debug.LogError("MBugReporter system breaking disable is enabled. Aborting early :|");
                        m_ShouldSelfDisable = true;
                    }
                }
            }
            return m_ShouldSelfDisable.Value;
        }
    }
#endif

#if !DISABLE_MBUG

    private static bool? m_ShouldSelfDisable = null;

    public static FFMPEGPlatform Platform {
        get {
            if (m_Platform == FFMPEGPlatform.UNSET) {
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer) m_Platform = FFMPEGPlatform.Windows_x64;
                else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer) m_Platform = FFMPEGPlatform.MacOS_x64;
                else if (Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxServer) m_Platform = FFMPEGPlatform.MacOS_x64;
                else m_Platform = FFMPEGPlatform.Unsupported;

            }
            return m_Platform;
        }

    }

    static FFMPEGPlatform m_Platform = FFMPEGPlatform.UNSET;

    public enum FFMPEGPlatform { UNSET = 0, Unsupported = 1, Windows_x64 = 10, MacOS_x64 = 20, Linux_x64 = 30 }

    public static string FFMpegPath {
        get {
            if(m_FFMpegPath == null) {
                m_FFMpegPath = EvaluateFFMPEGPath();
            }
            return m_FFMpegPath;
        }
    }
    static string m_FFMpegPath = null;

    static string EvaluateFFMPEGPath()
    {
        if (undyingInstance == null) EnsureHavePathsFromMainThread();
        var executablesPath = MBugReporter.ExecutablesRootPath;

        if (Platform == FFMPEGPlatform.Windows_x64)
        {
            return Path.Combine(executablesPath, "FFMPEG", "ffmpeg_windows.exe");
        }
        else if (Platform == FFMPEGPlatform.MacOS_x64)
        {
            return Path.Combine(executablesPath, "FFMPEG", "ffmpeg_mac");
        }
        else if (Platform == FFMPEGPlatform.Linux_x64)
        {
            return Path.Combine(executablesPath, "FFMPEG", "ffmpeg_linux"); //not included currently
        }
        else throw new System.Exception("BGVideoCapture: unsupported platform");
    }


    private void EarlyInit()
    {
        //no domain relaod stuff
        recordingTime = 0f;
		systemTurningOff = false;
		foldersCreated.Clear();
		haveNewSendable = false;
		//msgPackSerializerRegistered = false; //dunno
		sendableAttemptStarted = 0;
		sendableAttemptFinished = 0;
		fileUploadPassesCompleted = 0;
		howManyOldSessionsToPost = 0;
		stillSendingOldSessions = false;
		howManyFoldersBeenPosted = 0;
		
		MBugCustomBackEndUploader.someProgress = 0f;
		MBugCustomBackEndUploader.totalReqFails = 0;
		MBugCustomBackEndUploader.systemHaltedDueToMisconfiguration = false;
        MBugCustomBackEndUploader.obtainedProjConfiguration = null;
		
		MCrashReporterHost.earlyCrashReporter = null;
		MCrashReporterHost.ranEarlyStart = false;


        if (ShouldSelfDisable) {
            DestroyImmediate(this);
            return;
        }

        undyingInstance = this;

        InitUploader();

        streamingAssetsPath = Application.streamingAssetsPath;
        persistentDataPath = Application.persistentDataPath;
        dataPath = Application.dataPath;
        startTime = System.DateTime.UtcNow;
        extension = "." + imageFormatToUse.ToString().ToLower();

        MCrashReporterHost.EnsureOnAppEarlyStartCalled();
        if(Application.isEditor && PlayerPrefs.GetInt("MBugReporter_EditorPassiveOptOutEnabled") == 1 && !MCrashReporterHost.GetDidGameStartfromNormalStartupScene()) { //PlayerPref because it is project-specific
            editorOptOutInEffect = true;
            Debug.Log("BGVideoCapture Editor opt out in effect :(");
        }

        LoadConfig();

        new Thread( () => SendablesUploaderThread() ).Start();
    }

    public static bool EditorOptOutInEffect => editorOptOutInEffect;
    private static bool editorOptOutInEffect = false;

    public void OverrideEditorOptOutforSession() {
        editorOptOutInEffect = false;
    }


    DateTime startTime;
    string extension;

    public static void InitUploader()
    {
        MBugContentUploader.InitializeIfNotInitializedYet();        
    }

    private void CleanupScratchFolder()
    {
        var dir = new DirectoryInfo(RootFolder);
        if (dir.Exists) {
            var subDirs = dir.GetDirectories().Where(x => x.Name != "Uploadable");
            foreach (var item in subDirs) {
                item.Delete(true);
            }
            var fils = dir.GetFiles();
            foreach (var item in fils) {
                item.Delete();
            }
        }
    }

    private void TrimOldSessionsFolder()
    {
        var oldSessionsFolder = new DirectoryInfo(RootFolder + "/Uploadable");

        //var userIsDev = new FileInfo(MBugReporter.GetEditorLogFilePath()).Directory.Exists || System.Environment.OSVersion.Platform == PlatformID.Unix; //other condition: builds are currently only on windows so a bit of a guess

        if (oldSessionsFolder.Exists) {
            var dirs = oldSessionsFolder.GetDirectories().OrderBy(x => x.CreationTimeUtc);

            var allowedSpace = 1000 * 1000 * 1000; //1 gig

            long totalSize = 0;
            foreach (var item in dirs)
            { 
                var siz = CalculateFolderSize(item.FullName);
                totalSize += siz;
            }
            if (!Application.isEditor) Debug.Log("TrimOldSessionsFolder: total size is " + MaxinRandomUtils.ByteLenghtToHumanReadable(totalSize) + ", limit:" + MaxinRandomUtils.ByteLenghtToHumanReadable(allowedSpace));

            if(totalSize >= allowedSpace)
            {
                foreach (var item in dirs) {
                    if (totalSize <= allowedSpace)
                        break;

                    totalSize -= CalculateFolderSize(item.FullName);
                    item.Delete(true);
                }

                Debug.Log("TrimOldSessionsFolder: after trimming total size is" + MaxinRandomUtils.ByteLenghtToHumanReadable(totalSize) + ", below limit " + MaxinRandomUtils.ByteLenghtToHumanReadable(allowedSpace));
            }
            
            //var tooOld = dirs.Where(x => (System.DateTime.UtcNow - x.CreationTimeUtc).TotalDays )

        }
    }
    static long CalculateFolderSize(string folder)
    {
        var fils = new DirectoryInfo(folder).GetFiles("*", SearchOption.AllDirectories);
        return fils.Sum(x => x.Length);
    }

    private void LoadConfig()
    {
        var configPath = Application.streamingAssetsPath + "/BGVideoCaptureConfig.txt";
        try {
            config = JsonUtility.FromJson<BGVideoCaptureConfig>(File.ReadAllText(configPath));
        }
        catch (System.Exception e) {
            Log("BGVideoCapture: no config or error, setting to default config\n\n" + e);
            config = new BGVideoCaptureConfig();
        }
    }

    private void Start()
    {
        if(!Application.isEditor) Debug.Log("BGVideoCapture start");
        EarlyInit();

        if (autoCleanupAtStart) {
            CleanupScratchFolder();
        }
        TrimOldSessionsFolder();

        if (!Application.isEditor) Debug.Log("BGVideoCapture Start: querying username");
        var userName = System.Environment.UserName;
        if (!Application.isEditor) Debug.Log("BGVideoCapture Start: successfully queried username:" + userName);

        var machineID = $"{RemoveWeirdChars(System.Environment.MachineName)}_{RemoveWeirdChars(userName)}_" + RemoveWeirdChars(machineIDExtraPostFix);
        //var editorOrNot = Application.isEditor ? "EDITOR" : "BUILD";        
        //sessionID = "SES_" + FileNameFriendlyAppProductName + "_" + startTime.ToFileTime().ToString()+"_"+machineID+"__"+editorOrNot;
        sessionID = "SES_" + MBugReporter.kFakeProjectNameToReplace + "_" + startTime.ToFileTime().ToString() + "_" + machineID + "__" + Application.platform;

        string csStr = "CSFAIL-TOP";
        try {
            csStr = GetShortBuildAndEnvTypeIdentifyingString();
        }
        catch (System.Exception e) {
            Debug.LogError(e);
        }
        sessionID += "_" + csStr;

        SetNewSegment();

        //just to avoid nulls
        bigRT = new RenderTexture(2, 2, 0);
        bigRT.name = "BGVideoCapture_bigRT";
        smallRT = new RenderTexture(2, 2, 0);
        smallRT.name = "BGVideoCapture_smallRt";


        //appears that windows players and macs (DirectX and Metal) need the flip. A virtualized software-rendered Mac doesn't. Assuming OpenGL is the differentiator.
        var isOpenGL = SystemInfo.graphicsDeviceType.ToString().Contains("OpenGL");
        var textureneedsFlipping = !isOpenGL;

        if (textureneedsFlipping) {
            flipBlitScale = new Vector2(1f, -1f);
            flipBlitOffset = new Vector2(0f, 1f);
        }
        else {
            flipBlitScale = new Vector2(1f, 1f);
            flipBlitOffset = new Vector2(0f, 0f);
        }
    }


    private static string RemoveWeirdChars(string inputText)
    {
        return inputText.Replace("_", "").Replace(" ", ""); //TODO make safer for filenames
    }

    public string GetShortBuildAndEnvTypeIdentifyingString()
    {
        return MChangesetIdentification.GetShortChangesetHash();
        //return "afc8f6e"; //example
        //return "putChangeSetHere";
        //return "cs1312"; //example
    }


    public static string SessionID => sessionID;
    public TimeSpan RunningTime => System.DateTime.UtcNow - startTime;

    string currentCaptureID = "NOTSET";
    string currentCaptureFolder;
    static string RootFolder => applicationDotTemporaryCachePath + "/BGGameVideoCapture" + machineIDExtraPostFix + "/";

    private void SetNewSegment() {
        segmentIndex++;
        segmentStartFrame = TotalFrameNum;
        segmentFrameNum = 0;
        //segmentTimer = 0f;
        segmentTimer -= segmentTargetLength; //next segment will be shorter if previous overshot
        if (segmentTimer < 0f) segmentTimer = 0f; //otherwise goes negative at start!
        currentCaptureID = sessionID + "_segm" + segmentIndex;
        currentCaptureFolder = RootFolder + currentCaptureID;
        Directory.CreateDirectory(currentCaptureFolder);
    }


    int disabledTillFrame = -100;

    public static bool DisableForAFewFrames()
    {
        if (undyingInstance == null) return false;
        undyingInstance.disabledTillFrame = Time.frameCount + 10;
        return true;
    }


    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount <= disabledTillFrame)
            return;

        if (Time.frameCount < 20)
            return;


        NewGameFrame();
        SegmentCheck();
    }

    private void SegmentCheck()
    {
        if (segmentTimer > segmentTargetLength) {
            var prevSegmentFolder = currentCaptureFolder;
            var prevSegmentStartFrame = segmentStartFrame;
            SetNewSegment();
            ProcessDoneSegment(segmentIndex - 1, prevSegmentStartFrame, TotalFrameNum, prevSegmentFolder);
        }
    }





    float TargetFrameTime => 1f / config.fps;
    float frameTimer = 0f;
    float segmentTimer = 0f;
    static double recordingTime;

    public static double RecordingTime => recordingTime;


    string screenshotToPathPending = null;

    public static void CaptureScreenShot(string outPath)
    {
        if (undyingInstance == null || !undyingInstance.enabled) {
            Debug.LogError("Cannot capture screenshot with BGVideoCapture because recording is disabled");
            return;
        }
        if (undyingInstance.screenshotToPathPending != null) {
            Debug.LogError("BGVideoCapture CaptureScreenShot: screenshot already pending, cannot take another one!\nPending:\t" + undyingInstance.screenshotToPathPending + "\nTried:\t" + outPath);
            return;
        }
        undyingInstance.screenshotToPathPending = outPath;
    }

    private void NewGameFrame()
    {
        if (TotalFrameNum == 0) {
            OnRecordingStart();
        }

        frameTimer += Time.unscaledDeltaTime;

        CountTick("gameframe");

        if (frameTimer > TargetFrameTime || screenshotToPathPending != null) {
            CountTick("earlyCapture");
            Capture();


            var maxAccumulation = 0.5f;
            if (frameTimer > maxAccumulation) {
                frameTimer = maxAccumulation;
            }


            frameTimer -= TargetFrameTime;
        }

        segmentTimer += Time.unscaledDeltaTime;
        recordingTime += Time.unscaledDeltaTime;
    }

    //float extraAccumulationToAddToFrame = 0f;


    private void OnRecordingStart()
    {
        timeRecordingStarted = System.DateTime.UtcNow;
    }

    private void Capture()
    {
        StartCoroutine(CapAtEndOfFrame());
    }

#endif
    public bool autoCleanupAtStart = true;
    public bool autoCleanUpAsDataIsProcessed = true;

    public RenderTexture bigRT = null;
    public RenderTexture smallRT;
    public RenderTexture flippedSmallRT;

#if !DISABLE_MBUG


    bool sizePending = false;

    bool captureAtEndOfFramePending = false;


    Vector2 flipBlitScale;
    Vector2 flipBlitOffset;

        
    public static bool doNiceDownscaling = false;


    private IEnumerator CapAtEndOfFrame()
    {
        if (captureAtEndOfFramePending) //happens when screen isn't visible; all of these hang
            yield break;

        if (screenShotInProgress)
            yield break; //if it takes multiple frames or something, don't touch the large RT's inbetween

        captureAtEndOfFramePending = true;
        yield return new WaitForEndOfFrame();
        captureAtEndOfFramePending = false;

        var needAScreenShotToPath = screenshotToPathPending;
        screenshotToPathPending = null;


        if (currentCaptureFolder == null) {
            LogErrorAntiRepeat("currentCaptureFolderNull");
            yield break;
        }


        var outPath = Path.Combine(currentCaptureFolder, segmentFrameNum.ToString("0000000000") + extension);


        int frameHeight = (int)(config.frameWidth / (Screen.width / (float)Screen.height));
        frameHeight = MakeSureIsDivisibleByTwo(frameHeight);


        var sizesChanged = false;

        if (smallRT == null) {
            Debug.LogError("BGVideoCapture: fatal fail, smallRT is null! Disabling system.");
            gameObject.SetActive(false);
            yield break;
        }

        if (!CheckRenderTextureIsSize(smallRT, config.frameWidth, frameHeight)) {
            sizesChanged = true;
        }
        if (!CheckRenderTextureIsSize(bigRT, Screen.width, Screen.height)) {
            sizesChanged = true;
        }

        if (sizesChanged) {
            if (!sizePending) {
                Log("sizePending to TRUE");
                sizePending = true;
            }
        }


        if (sizePending) {
            if (requestsInAir > 0) {
                Log("delaying rendertextures size change, requests still in-air");
                yield break;
            }
            else if (compressionsRunning > 0) {
                Log("delaying rendertextures size change, compressionsRunning still running");
                yield break;
            }
            else {
                sizePending = false;
                Log("executing resize");

                if (smallRT != null) DestroyImmediate(smallRT);
                if (flippedSmallRT != null) DestroyImmediate(flippedSmallRT);
                smallRT = new RenderTexture(config.frameWidth, frameHeight, 16, RenderTextureFormat.ARGB32);
                smallRT.name = "BGVideoCapture_smallRt";
                flippedSmallRT = new RenderTexture(config.frameWidth, frameHeight, 16, RenderTextureFormat.ARGB32);
                flippedSmallRT.name = "BGVideoCapture_flippedSmallRT";

                if (bigRT != null) DestroyImmediate(bigRT);
                bigRT = new RenderTexture(Screen.width, Screen.height, 16);
                bigRT.name = "BGVideoCapture_bigRT";

                InvalidateAllPooledArrs();
            }
        }

        if (!pixelArrSizeActuallyDetermined && requestsInAir > 0) {
            Log("!pixelArrSizeActuallyDetermined, and a requests is in air, waiting");
        }



        var maxConcurrentCompressions = 10;
        if (compressionsRunning > maxConcurrentCompressions) {
            Debug.LogWarning($"compressionsRunning({compressionsRunning}) >= maxConcurrentCompressions({maxConcurrentCompressions}), skipping frame");
            yield break;
        }


        bool runDownscale = false;
        if (doNiceDownscaling) {
            var scaleRatio = (float)smallRT.width / bigRT.width;
            runDownscale = scaleRatio < 0.4f;
        }


        ScreenCapture.CaptureScreenshotIntoRenderTexture(bigRT);
        if (runDownscale) {
            Graphics.Blit(bigRT, smallRT, GetDownScalingMaterial());
        }        
        else {
            Graphics.Blit(bigRT, smallRT);
        }
        Graphics.Blit(smallRT, flippedSmallRT, flipBlitScale, flipBlitOffset);


        determinedPixelArrSize = CalculateSizeForRT(flippedSmallRT);
        var refBytes = GetPooledArr();

        if (!IsRightSize(flippedSmallRT, refBytes.Length, pixelArrSizeActuallyDetermined))
            yield break;


        if (reqDebug) Debug.Log("req for " + TotalFrameNum + " seq:" + segmentIndex + " " + segmentFrameNum + " " + outPath);

        requestsInAir++;
        Log("PING " + Time.frameCount + " RequestIntoNativeArray");
        var totalFrameNumCopy = TotalFrameNum;
        var timeFrameCountCopy = Time.frameCount;
        var segmentFrameNUmCopy = segmentFrameNum;
        var segmentIndexCopy = segmentIndex;
        var stamp = System.DateTime.UtcNow - timeRecordingStarted;
        CountTick("request");
        AsyncGPUReadback.RequestIntoNativeArray(ref refBytes, flippedSmallRT, 0, flippedSmallRT.graphicsFormat, (req) => OnGotFromGPU(req, outPath, flippedSmallRT, refBytes, timeFrameCountCopy, segmentFrameNUmCopy, segmentIndexCopy, totalFrameNumCopy, stamp));

        if (needAScreenShotToPath != null) {
            Debug.Log("BGVideoCapture: taking screenshot");
            screenShotInProgress = true;

            if (flippedBigRTForScreenShots == null || !CheckRenderTextureIsSize(flippedBigRTForScreenShots, bigRT.width, bigRT.height)) {
                if (flippedBigRTForScreenShots != null) DestroyImmediate(flippedBigRTForScreenShots);
                flippedBigRTForScreenShots = new RenderTexture(bigRT.width, bigRT.height, 16, RenderTextureFormat.ARGB32);
                flippedBigRTForScreenShots2 = new RenderTexture(bigRT.width, bigRT.height, 16, RenderTextureFormat.ARGB32);
            }
            Graphics.Blit(bigRT, flippedBigRTForScreenShots, flipBlitScale, flipBlitOffset);
            Graphics.Blit(flippedBigRTForScreenShots, flippedBigRTForScreenShots2, Resources.Load<Material>("RemoveAlphaMaterial"), 0);

            var bigRTDataSize = CalculateSizeForRT(flippedBigRTForScreenShots2, true);
            if (bigScreenShotBytes.Length != bigRTDataSize) {
                if (bigScreenShotBytes.Length > 0) bigScreenShotBytes.Dispose();
                Log("bigRTDataSize:" + bigRTDataSize);
                bigScreenShotBytes = new NativeArray<byte>(bigRTDataSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            }
            Debug.Log("BGVideoCapture: taking screenshot: issuing RequestIntoNativeArray for " + needAScreenShotToPath);
            if (screenShotInAir) {
                Debug.LogError("BGVideoCapture: screenShotInAir true, so a request is in-air already. If this hangs, change to abort in this case!");
            }
            screenShotInAir = true;
            AsyncGPUReadback.RequestIntoNativeArray(ref bigScreenShotBytes, flippedBigRTForScreenShots2, 0, flippedBigRTForScreenShots2.graphicsFormat, (req) => OnGotScreenShotData(req, needAScreenShotToPath));
            Debug.Log("BGVideoCapture: taking screenshot: issued RequestIntoNativeArray for" + needAScreenShotToPath);
        }

        segmentFrameNum++;
        TotalFrameNum++;
    }

    private static Material boxDownScalingMaterial = null;

    public static Material GetDownScalingMaterial()
    {
        if(boxDownScalingMaterial == null) {
            boxDownScalingMaterial = Resources.Load<Material>("DownScalingMaterial");
        }
        return boxDownScalingMaterial;
    }

    bool screenShotInAir = false;


    Dictionary<string, int> tickCounts = new Dictionary<string, int>();
    Dictionary<string, int> tickCountsLastSnap = new Dictionary<string, int>();

    private void CountTick(string v)
    {
        if (!tickCounts.ContainsKey(v)) {
            tickCounts.Add(v, 0);
        }
        tickCounts[v] = tickCounts[v] + 1;

        var snapTime = (int)Time.realtimeSinceStartup;
        if (snapTime != lastSec) {
            tickCountsLastSnap = MaxinRandomUtils.CloneDictionary(tickCounts);
            tickCounts.Clear();
            lastSec = snapTime;
        }
    }
    int lastSec = -1;

    float timeLastTookScreen = -100f;

    static bool reqDebug = false;

    enum ImageFormatTouse { PNG = 1, JPG = 2}

    const ImageFormatTouse imageFormatToUse = ImageFormatTouse.JPG;


    private void OnGotScreenShotData(AsyncGPUReadbackRequest req, string needAScreenShotToPath)
    {
        Debug.Log("BGVideoCapture: taking screenshot: RequestIntoNativeArray returned for " + needAScreenShotToPath);
        if (req.hasError) {
            Log("OnGotScreenShotData finished with error, ignoring");
            return;
        }

        var format = flippedBigRTForScreenShots.graphicsFormat;
        var w = flippedBigRTForScreenShots.width;
        var h = flippedBigRTForScreenShots.height;

        if (bigScreenShotManagedBytes == null || bigScreenShotManagedBytes.Length != bigScreenShotBytes.Length) {
            bigScreenShotManagedBytes = new byte[bigScreenShotBytes.Length];
        }
        bigScreenShotBytes.CopyTo(bigScreenShotManagedBytes);

        AsyncRunnerHelper.RunInSeparateThread(() => {
            byte[] imageFileBytes;            
            imageFileBytes = ImageConversion.EncodeArrayToPNG(bigScreenShotManagedBytes, format, (uint)w, (uint)h);
            File.WriteAllBytes(needAScreenShotToPath, imageFileBytes);
            Debug.Log("BGVideoCapture: taking screenshot: RequestIntoNativeArray returned and processing finished for " + needAScreenShotToPath);
            screenShotInProgress = false;
        });

        timeLastTookScreen = Time.realtimeSinceStartup;

        screenShotInAir = false;
    }

    RenderTexture flippedBigRTForScreenShots = null;
    RenderTexture flippedBigRTForScreenShots2 = null;

    NativeArray<byte> bigScreenShotBytes;
    byte[] bigScreenShotManagedBytes = null;

    bool screenShotInProgress = false;

    private bool IsRightSize(RenderTexture flippedSmallRT, int length, bool requireExact)
    {
        var calculated = CalculateSizeForRT(flippedSmallRT);

        bool fits;
        if (requireExact) {
            fits = calculated == length;
        }
        else {
            fits = calculated <= length;
        }

        if (!fits) {
            Debug.LogError("IsRightSize FAIL: calculated:" + calculated + " given:" + length);
        }

        return fits;
    }


    public static int CalculateSizeForRT(RenderTexture rtToCheck, bool allowWeirdFormat = true) //possibly set allowWeirdFormat back to false after mac experiment
    {
        var pixelCount = rtToCheck.width * rtToCheck.height;

        if (!allowWeirdFormat && rtToCheck.format != RenderTextureFormat.ARGB32) throw new System.Exception("rtToCheck.format != RenderTextureFormat.ARGB32 " + rtToCheck.format);

        var actualRTDepth = 4;
        var size = pixelCount * actualRTDepth;
        return size;
    }

    private bool CheckRenderTextureIsSize(RenderTexture rtToCheck, int frameWidth, int frameHeight)
    {
        if (rtToCheck == null) {
            gameObject.SetActive(false);
            throw new System.Exception("BGVideoCapture: fatal fail, a rendertexture is null! Disabling system.");
        }

        var isChanged = rtToCheck.width != frameWidth || rtToCheck.height != frameHeight; ;
        if (isChanged) {
            Log(rtToCheck.name + " size is going to be changed from " + RezToStr(rtToCheck.width, rtToCheck.height) + " to " + RezToStr(frameWidth, frameHeight));
        }
        return !isChanged;
    }

    private string RezToStr(int width, int height)
    {
        return width + "x" + height;
    }

    private void Log(string toLog)
    {
        //Debug.Log("BGVid " + Time.frameCount + " " + toLog);
    }

    public static int MakeSureIsDivisibleByTwo(int frameHeight) {
        frameHeight = (int)Math.Round((decimal)(frameHeight / 2)) * 2;
        return frameHeight;
    }

    int requestsInAir = 0;


    byte[] cachedBytes = new byte[0];

    private void OnGotFromGPU(AsyncGPUReadbackRequest req, string outPath, RenderTexture requestedRT, NativeArray<byte> reffedArr, int requestFiredOnFrame, int frameNum, int segmentIndex, int requestedTotalFrameNum, TimeSpan timestamp)
    {
        CountTick("requestDone");
        if (reqDebug) Debug.Log("req back for -1 " + requestedTotalFrameNum + " (at curr:" + TotalFrameNum + ")");

        if (this == null || systemTurningOff)
            return;

        Log("PONG " + Time.frameCount + " OnGotFromGPU (fired:" + requestFiredOnFrame + "), requestsInAir:" + requestsInAir);
        requestsInAir--;

        if (reqDebug) Debug.Log("req back for 0 " + requestedTotalFrameNum + " (at curr:" + TotalFrameNum + ")");

        if (req.hasError) {
            ReturnPooledArr(reffedArr);
            Log("OnGotFromGPU finished with error, ignoring");
            return;
        }

        if (sizePending) {
            ReturnPooledArr(reffedArr);
            return;
        }

        var gottenDataSize = req.layerCount * req.layerDataSize;

        if (reffedArr.Length != gottenDataSize) {
            Debug.LogError("reffedArr.Length != gottenDataSize");
            return;
        }



        var data = reffedArr;

        if (data.Length != cachedBytes.Length) {
            cachedBytes = data.ToArray();
        }
        else {
            data.CopyTo(cachedBytes);
        }

        ReturnPooledArr(reffedArr);

        var w = (uint)requestedRT.width;
        var h = (uint)requestedRT.height;

        var fromFormat = requestedRT.graphicsFormat;

        if (reqDebug) Debug.Log("req back for A " + requestedTotalFrameNum + " (at curr:" + TotalFrameNum + ")");



        Interlocked.Increment(ref compressionsRunning);

        AsyncRunnerHelper.RunInSeparateThreadThenRunFinisher(() =>
        {
            try {
                if (reqDebug) Debug.Log("req back for B " + requestedTotalFrameNum + " (at curr:" + TotalFrameNum + ")");
                byte[] imageFileBytes;
                //var timer = System.Diagnostics.Stopwatch.StartNew();
#pragma warning disable CS0162 // Unreachable code detected
                if (imageFormatToUse == ImageFormatTouse.PNG) {
                    imageFileBytes = ImageConversion.EncodeArrayToPNG(cachedBytes, fromFormat, (uint)w, (uint)h);
                }
                else {
                    imageFileBytes = ImageConversion.EncodeArrayToJPG(cachedBytes, fromFormat, (uint)w, (uint)h);
                }
#pragma warning restore CS0162 // Unreachable code detected
                //Debug.Log(timer.ElapsedMilliseconds + " ms");
                if (reqDebug) Debug.Log("WRITE:" + outPath);
                if (new FileInfo(outPath).Directory.Exists) {
                    File.WriteAllBytes(outPath, imageFileBytes);
                }
                else {
                    LogErrorAntiRepeat("Could not write JPG, directory missing!");
                }

                //Debug.Log(frameNum + " " + (doOnFirstFrameInSequence != null).ToString());

                if (reqDebug) Debug.Log("req back for C " + requestedTotalFrameNum + " (at curr:" + TotalFrameNum + ")");
            }
            finally {
                Interlocked.Decrement(ref compressionsRunning);
            }
        },
        () => {
            outPath = outPath.Replace("\\", "/");
            framePaths[requestedTotalFrameNum] = outPath;
            framePathsBackwards[outPath] = requestedTotalFrameNum;
            frameTimeStamps[requestedTotalFrameNum] = timestamp;

            if (reqDebug) Debug.Log("req back for D " + requestedTotalFrameNum + " (at curr:" + TotalFrameNum + ")");
            CountTick("requestDoneCompr");

            if (frameNum == 1 && doOnFirstFrameInSequence != null) {
                AsyncRunnerHelper.RunInSeparateThread(() => doOnFirstFrameInSequence(outPath, segmentIndex));
            }
        },
        AsyncRunnerHelper.FinisherCallingThreadMode.UnityMainThread);
    }

    string lastRepeat;
    int repeatCount = 0;

    private void LogErrorAntiRepeat(string v)
    {
        if (v != lastRepeat) {
            lastRepeat = v;
            Debug.LogWarning(v);
        }
        else {
            repeatCount++;
            if (repeatCount % 200 == 0) {
                Debug.LogWarning("LogErrorAntiRepeat:" + lastRepeat + " times:" + repeatCount);
            }
        }
    }

    ConcurrentDictionary<int, string> framePaths = new ConcurrentDictionary<int, string>();
    ConcurrentDictionary<string, int> framePathsBackwards = new ConcurrentDictionary<string, int>();
    ConcurrentDictionary<int, System.TimeSpan> frameTimeStamps = new ConcurrentDictionary<int, System.TimeSpan>();

    public static Action<string, int> doOnFirstFrameInSequence = null;

    int compressionsRunning = 0;

    //uint firstCompressedWidth = 0;
    //uint firstCompressedHeight = 0;

    int determinedPixelArrSize = 1024 * 1024 * 1024;
    bool pixelArrSizeActuallyDetermined = false;

    List<NativeArray<byte>> pooledFreeArrs = new List<NativeArray<byte>>();
    List<NativeArray<byte>> pooledArrs = new List<NativeArray<byte>>();

    public NativeArray<byte> GetPooledArr() {
        if (pooledFreeArrs.Count == 0) {
            var arr = new NativeArray<byte>(determinedPixelArrSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            pooledFreeArrs.Add(arr);
            pooledArrs.Add(arr);
        }
        var toRet = pooledFreeArrs[0];
        pooledFreeArrs.RemoveAt(0);
        return toRet;
    }
    public void ReturnPooledArr(NativeArray<byte> arr) {
        pooledFreeArrs.Add(arr);
    }

    public void InvalidateAllPooledArrs()
    {
        foreach (var item in pooledArrs) {
            if(item.IsCreated)item.Dispose();
        }
        pooledFreeArrs.Clear();
        pooledArrs.Clear();
    }

    //public uint rowBytes = 3;

    static ReproTraceClientConfiguration _cached;



    private void ProcessDoneSegment(int processingSegmentIndex, int fromFrame, int toFrame, string processingSegmentFolderMaybe)
    {
        streamingAssetsPath = Application.streamingAssetsPath;

        if(toFrame - fromFrame < 2) {
            if(!Application.isEditor)Debug.LogWarning("BGVideoCapture: small amount ("+ (toFrame - fromFrame) + ") of frames in segment, just ignoring the whole thing so it doesn't cause trouble");
            return;
        }

        AsyncRunnerHelper.RunInSeparateThread(() =>
        {
            try
            {
                Thread.Sleep(2000); //just wait a few seconds if there's stuff in progress?

                
                var compressedVidPath = CompressFolderInThread(processingSegmentFolderMaybe, fromFrame, toFrame);                

                if (autoCleanUpAsDataIsProcessed)
                { //TODO: figure out why DeleteNonReservedFrames is not working right in every situation
                    try { //one of these fails in some unspecified condition. Need to trycatch to avoid losing the entire segment if next step isn't run at all. This data will be cleared eventualy anyway
                        DeleteNonReservedFrames(processingSegmentFolderMaybe);
                        if (folderDeletionsToReCheck.Count > 0) {
                            DeleteNonReservedFrames(folderDeletionsToReCheck[0]); //only check one folder at a time to avoid IO flood
                        }
                        RemoveEmptyFolders();
                    }
                    catch(System.Exception e) {
                        Debug.LogException(e);
                    }
                }
                
                try {                        
                    SetSessionContentReadyToUpload(compressedVidPath, true);
                }
                catch (System.Threading.ThreadAbortException) {
                    Debug.Log("Ignore the following ThreadAbortException, it's normal");                        
                    return;
                }
                catch (System.Exception e) {
                    CauseAllowVerboseDebugLoggigForWhile("ffmpeg WAS in trouble?!:" + e.ToString());
                    Debug.LogException(e);
                }

                if(processingSegmentIndex == 0) {
                    MCrashReporterHost.OnVideoCaptureStarted(GetSessionFolderDepoPath(SessionID), GetSendablesPathThisSession());
                }
            }
            catch(ThreadAbortException) {
                Debug.Log("Ignore the following ThreadAbortException, it's normal");                
                return; //annoyingly the exception is still logged by Unity. Ran out of ideas how to hide it
            }
        });
    }


    private void RemoveEmptyFolders()
    {
        var folds = new DirectoryInfo(RootFolder).GetDirectories("*_segm*");
        foreach (var item in folds) {
            var enumm = item.EnumerateFileSystemInfos().GetEnumerator();
            var isEmpty = !enumm.MoveNext();
            if (isEmpty) {
                item.Delete();
            }
        }
    }

    private void DeleteNonReservedFrames(string folderToProcess)
    {
        if (!Directory.Exists(folderToProcess)) {
            Debug.LogWarning("DeleteNonReservedFrames: folder doesn't exist!:"+folderToProcess);
            return;
        }

        //Directory.Delete(folderToProcess, true);
        var filesToDelete = Directory.GetFiles(folderToProcess).Select(x => x.Replace(@"\","/")).ToList();        
        var deferredCount = filesToDelete.RemoveAll(x => framePathsBackwards.ContainsKey(x) && FrameIsReserved(framePathsBackwards[x]));

        foreach (var item in filesToDelete) {
            File.Delete(item);
        }

        if(deferredCount > 0) {
            if (!folderDeletionsToReCheck.Contains(folderToProcess)) {
                folderDeletionsToReCheck.Add(folderToProcess);
            }
        }
        else {
            if (folderDeletionsToReCheck.Contains(folderToProcess)) {
                folderDeletionsToReCheck.Remove(folderToProcess);
            }
        }
    }

    List<string> folderDeletionsToReCheck = new List<string>();

    private bool FrameIsReserved(int frame)
    {
        var isReserved = allPendingReservedRecordings.Any(x => frame >= x.frameStart && (!x.frameEnd.HasValue || x.frameEnd.Value >= frame));
        return isReserved;
    }

    private string CompressFolderInThread(string folderToProcess, int fromFrame, int toFrame)
    {
        var dir = new DirectoryInfo(folderToProcess);
        var saveToPath = Path.Combine(dir.Parent.FullName, dir.Name + ".mp4");

        var timer = System.Diagnostics.Stopwatch.StartNew();
        SaveReservedRecording(new ReservedRecording { recordingName = "segment" + dir.Name, isSegment = true, frameStart = fromFrame, frameEnd = toFrame }, saveToPath);
        if(!Application.isEditor)Debug.Log("[MBUG] Took "+timer.Elapsed.TotalSeconds+ " s to compress a 10s segment maxCompressionThreads"+ config.maxCompressionThreads+ " fps:"+config.fps+" frames:"+(toFrame-fromFrame));
        if (timer.Elapsed.TotalSeconds > 11) {
            if(config.maxCompressionThreads == 2) {
                if (!Application.isEditor) Debug.Log("Took too long to compress a segment, and max threads is already at auto-increased 2, setting framerate to 15 if not set there already");
                config.fps = 15;
            }
            else {
                if(config.maxCompressionThreads == 1) {
                    if (!Application.isEditor) Debug.Log("Took too long to compress a segment, increasing max threads from 1 to 2");
                    config.maxCompressionThreads = 2;   
                }
                else if (!Application.isEditor) Debug.Log("Took too long to compress a segment, but max threads is not at default 1 or auto-increased 2, doing nothing");
            }
        }

        return saveToPath;
    }

    private System.Diagnostics.Process LaunchFFMPEG(string input, string saveToPath, bool isManual)
    {
        var maxThreads = config.maxCompressionThreads;
        int bitrateKbps = config.bitrateKbps;
        string preset = "veryfast";
        if(isManual) {
            maxThreads = System.Environment.ProcessorCount;
            bitrateKbps = 1000;
            preset = "medium";
        }
        var log = /*isManual*/false;
                
        var doFastStart = true;
        string fastStartPart = doFastStart ? " -movflags faststart " : "";

        var arguments = " -f concat -safe 0 -y -i " + input + $" -r {(FPS > 30.1f ? 60 : 30)} -filter:v \"showinfo\"" + $" -b:v {bitrateKbps}k -c:v libx264 -preset {preset} -threads {maxThreads} -pix_fmt yuv420p -profile:v main {fastStartPart} " + " \"" + saveToPath + "\"";
        var proc = ProcessRunningUtils.RunExe(FFMpegPath, new[] { arguments }, out string ignore, out string errput, false, OnOutput, (x) => OnErrPut(x,log, isManual), logStdErrorToConsole: false, logStdToConsole: false, debugLogStarting: log || !Application.isEditor);
        return proc;
    }



    bool complainedErr = false;
    int allowErrorsPrintCount = 0;

    private void OnOutput(string obj) {
        Debug.LogWarning("BGVideoCapture ffmpeg OnOutput:" + obj);

        if(obj.Contains("failed")) {
            if(!complainedErr) Debug.LogError("BGVideoCapture ffmpeg in trouble! "+obj);
            complainedErr = true;
        }
    }

    private void CauseAllowVerboseDebugLoggigForWhile(string obj) {
        if (!complainedErr) {
            Debug.LogError("BGVideoCapture ffmpeg in trouble! " + obj);
            allowErrorsPrintCount = 100;
            complainedErr = true;
        }
    }
    private void OnErrPut(string obj, bool overrideLog, bool isManual)
    {
        if(overrideLog)
            Debug.Log("BGVideoCapture OnErrPut:" + obj);

        if (obj.Contains("failed")) {
            CauseAllowVerboseDebugLoggigForWhile(obj);
        }
        if (allowErrorsPrintCount > 0) {
            allowErrorsPrintCount--;
            Debug.Log("BGVideoCapture (allowing error printing temporarily, "+allowErrorsPrintCount+" allows left) OnErrPut:" + obj);
        }

        if (isManual) {
            ParsePotentialProgressInfo(obj);
        }
    }

    int lastCompressedFrame = 0;

    private void ParsePotentialProgressInfo(string logged)
    {
        if (logged.Contains(" n:")) {
            var indx = logged.IndexOf("n:") + "n:".Length;

            string numBuf = "";
            int numscanner = indx;
            while (true) {
                if (logged[numscanner] == ' ') {
                    //do nothing
                }
                else if (int.TryParse(logged[numscanner].ToString(), out int ignore)) {
                    numBuf = numBuf + logged[numscanner];
                }
                else break;

                numscanner++;
            }
            //Debug.Log("FRAME from showinfo:" + numBuf);
            lastCompressedFrame = int.Parse(numBuf);
        }
    }

    static bool systemTurningOff = false;

    private void OnDestroy()
    {
        MCrashReporterHost.OnDying();

        systemTurningOff = true;
        if (!Application.isEditor) Debug.Log("BGVIdeoCapture: AsyncGPUReadback.WaitAllRequests() (will log again when completed)");
        AsyncGPUReadback.WaitAllRequests();
        foreach (var item in pooledArrs) {
            item.Dispose();
        }
        if(bigScreenShotBytes.IsCreated) {
            bigScreenShotBytes.Dispose();
        }
        if (!Application.isEditor) Debug.Log("BGVIdeoCapture: AsyncGPUReadback.WaitAllRequests() DONE");
    }


    static HashSet<string> foldersCreated = new HashSet<string>();

    

    static bool haveNewSendable = false;

    static object sendablesLock = new object();

    public void SetSessionContentReadyToUpload(string sessionRelatedFilePath, bool deleteFileAfter)
    {
        lock (sendablesLock)
        {
            var sendablesPath = GetSendablesPathThisSession();
            Directory.CreateDirectory(sendablesPath);
            var toPath = sendablesPath + "/" + new FileInfo(sessionRelatedFilePath).Name;
            if (deleteFileAfter)
            {
                File.Move(sessionRelatedFilePath, toPath);
            }
            else
            {
                File.Copy(sessionRelatedFilePath, toPath);
            }
            haveNewSendable = true;
        }
    }

    static bool msgPackSerializerRegistered = false;

    static void InitializeMSGPACK()
    {
        if (!msgPackSerializerRegistered)
        {
            StaticCompositeResolver.Instance.Register(
                 MessagePack.Resolvers.GeneratedResolver.Instance,
                 MessagePack.Resolvers.StandardResolver.Instance
            );

            var option = MessagePackSerializerOptions.Standard.WithResolver(StaticCompositeResolver.Instance);

            MessagePackSerializer.DefaultOptions = option;
            msgPackSerializerRegistered = true;
        }
    }

    public void SetSessionContentReadyToUnloadStreamedDataBatch(MBugStreamedDataBatch dataBatch)
    {
        InitializeMSGPACK();
        var msgPackBytes = MessagePack.MessagePackSerializer.Serialize(dataBatch);

        lock (sendablesLock)
        {
            //var json = JsonUtility.ToJson(dataBatch, false);
            //Debug.Log("[MBugStreamedDataBatch.FlushStreamedData]" + json);

            var sendablesPath = GetSendablesPathThisSession();
            Directory.CreateDirectory(sendablesPath);
            var toPath = sendablesPath + "/" + new FileInfo("streamedDataMsgPackMiniBatch_" + dataBatch.batchNumber).Name;

            File.WriteAllBytes(toPath, msgPackBytes);

            haveNewSendable = true;
        }
    }

    public static void WaitTillNextSendablesAttempt()
    {
        var waitStart = sendableAttemptStarted;
        while(waitStart != sendableAttemptStarted) {
            Thread.Sleep(1);            
        }
        while (sendableAttemptFinished != sendableAttemptStarted) {
            Thread.Sleep(1);
        }
    }

    static int sendableAttemptStarted;
    static int sendableAttemptFinished;

    private static void SendablesUploaderThread()
    {
        bool lastFailed = false;
        while (true) {
            try {
                sendableAttemptStarted++;
                if (!editorOptOutInEffect) {
                    UploadSendables(lastFailed);
                }

                sendableAttemptFinished++;
                if (systemTurningOff)
                    return;

                lastFailed = false;
                Thread.Sleep(100);
            }
            catch(System.Exception e)
            {
                sendableAttemptFinished++;

                Debug.LogException(e);
                lastFailed = true;
                if (systemTurningOff)
                    return;

                Thread.Sleep(2000);
            }
        }
    }

    public static int fileUploadPassesCompleted = 0;

    private static void UploadSendables(bool lastFailed)
    {
        if (!haveNewSendable && !lastFailed)
            return;

        var sendablesPathThisSession = GetSendablesPathThisSession();

        FileInfo[] files;
        lock (sendablesLock)
        {
            files = new DirectoryInfo(sendablesPathThisSession).GetFiles();
            haveNewSendable = false;
            //if(files.Length > 0) Debug.Log(files.Length + " new files to upload");
        }

        //we do the actual sending outside the lock, avoids too long locks and this way the list only ever contains full files
        foreach (var item in files)
        {
            if (UploadSessionFolderContent(item)) { //on fail, well, we just try again next frame, the file isn't dleted
                File.Delete(item.FullName); 
            }
        }

        fileUploadPassesCompleted++;
        //if(files.Length > 0) Debug.Log("all sent");
    }

    public static bool UploadSessionFolderContent(FileInfo file)
    {
        EnsureSessionFolderExists(file, out var finalFolder);

        /*if (file.Name.StartsWith("streamedDataMsgPackMiniBatch_")) {
            return UploadSessionDataBatch(file);                
        }
        else {
            return UploadSessionFolderContentRawFile(file, finalFolder);
        }*/

        return UploadSessionFolderContentRawFile(file, finalFolder);
    }

    /*private static bool UploadSessionDataBatch(FileInfo item)
    {
        var ses = item.Directory.Name;
        return MBugCustomBackEndUploader.UploadStreamedDataMSGPack(File.ReadAllBytes(item.FullName), ses, ParseMiniPartIDX(item.Name));        
    }*/

    static int ParseMiniPartIDX(string partName)
    {
        return int.Parse(partName.Split("_")[1]);
    }

    private static bool UploadSessionFolderContentRawFile(FileInfo fInfo, string finalFolder)
    {
        return MBugContentUploader.UploadFileToDepository(fInfo.FullName, finalFolder + "/" + fInfo.Name, neverDropBox:fInfo.Name.StartsWith("streamedDataMsgPackMiniBatch_"));
    }

    public static string GetSendablesPathThisSession()
    {
        var sendablesPath = RootFolder + "/Uploadable";
        var sendablesThisSession = sendablesPath + "/" + SessionID;

        return sendablesThisSession;
    }

    public static string GetSessionFolderDepoPath(string sessionName)
    {
        var parsed = BGVideoSessionInfo.GetFromString(sessionName);

        var finalFolder = "Projects/"+MBugReporter.kFakeProjectNameToReplace+"/FullSessions" + "/" + GetTimeCompartmentDirectory(parsed.startTime) + "/" + sessionName;
        return finalFolder;
    }



    private static void EnsureSessionFolderExists(FileInfo fInfo, out string finalFolder)
    {   
        finalFolder = GetSessionFolderDepoPath(fInfo.Directory.Name);
        if (!foldersCreated.Contains(finalFolder))
        {
            //Debug.Log("CreateFolder" +finalFolder);
            MBugContentUploader.CreateFolder(finalFolder);
            foldersCreated.Add(finalFolder);
        }
    }

    public static BGVideoCapture undyingInstance = null;


    public List<ReservedRecording> currentReservedRecordings = new List<ReservedRecording>();
    public List<ReservedRecording> savingReservedRecordings = new List<ReservedRecording>();
    public List<ReservedRecording> allPendingReservedRecordings = new List<ReservedRecording>();

    public static ReservedRecording StartReservedRecording(string recordingName)
    {
        /*if (ATTBigAndSmallBuildTests.possiblyRunningTests)
            return null; //TEMP !!!!*/

        return undyingInstance.StartReservedRecordingInstance(recordingName);
    }

    private ReservedRecording StartReservedRecordingInstance(string recordingName)
    {
        Log("BGVideoCapture StartReservedRecording "+ recordingName);

        var reserved = new ReservedRecording { recordingName = recordingName, frameStart = TotalFrameNum };
        currentReservedRecordings.Add(reserved);
        allPendingReservedRecordings.Add(reserved);
        return reserved;
    }
    private void EndAndSaveReservedRecording(ReservedRecording res, string outPath)
    {
        Log("BGVideoCapture EndAndSaveReservedRecording " + res.recordingName +" "+outPath);

        if (!currentReservedRecordings.Contains(res)) {
            Debug.LogError("EndAndSaveReservedRecording is not in currentReservedRecordings");
            return;
        }

        res.frameEnd = TotalFrameNum;

        savingReservedRecordings.Add(res);
        currentReservedRecordings.Remove(res);

        /*AsyncRunnerHelper.RunInSeparateThreadThenRunFinisher( () => SaveReservedRecording(res, outPath), () => {
            if (!res.isSegment) Debug.Log("BGVideoCapture SaveReservedRecording DONE FINISHER " + res.recordingName + " " + outPath);
            savingReservedRecordings.Remove(res);
            allPendingReservedRecordings.Remove(res);
        }, AsyncRunnerHelper.FinisherCallingThreadMode.UnityMainThread);*/

        AsyncRunnerHelper.RunInSeparateThreadThenRunFinisher( () => SaveReservedRecording(res, outPath), () => MarkDoneOnmainThread(res,outPath), AsyncRunnerHelper.FinisherCallingThreadMode.UnityMainThread);
    }

    void MarkDoneOnmainThread(ReservedRecording res, string outPath)
    {
        if (!res.isSegment) Debug.Log("BGVideoCapture SaveReservedRecording DONE FINISHER " + res.recordingName + " " + outPath);
        savingReservedRecordings.Remove(res);
        allPendingReservedRecordings.Remove(res);
        res.SetFinalizedToPath(outPath);
    }

    int? currentCompressionprocessTotalFrames = null;

    private void SaveReservedRecording(ReservedRecording res, string outPath)
    {
        if(!res.isSegment) Debug.Log("BGVideoCapture SaveReservedRecording " + res.recordingName + " (frames "+res.frameStart+" -> "+res.frameEnd.Value+") " + outPath);

        List<string> framesToIncludePaths = new List<string>();
        var frameCounter = res.frameStart;
        if (res.frameStart >= res.frameEnd.Value) throw new System.Exception("res.frameStart >= res.frameEnd.Value " + res.frameStart + " >= " + res.frameEnd.Value);
        System.TimeSpan? firstFrameTime = null;

        while(true) {
            if (framePaths.ContainsKey(frameCounter)) {
                var path = framePaths[frameCounter];
                var finfo = new FileInfo(path);
                if(!finfo.Exists) {
                    //Debug.LogWarning("framePaths: " + path + " does not exists !");
                }
                else if(finfo.Length == 0) {
                    Debug.LogWarning("framePaths: " + path + " is zero lenght !");
                }
                else {
                    framesToIncludePaths.Add(path);
                    if(!firstFrameTime.HasValue) {
                        firstFrameTime = frameTimeStamps[frameCounter];
                    }
                }
            }
            else if(reqDebug) Debug.LogWarning("no path for frame " + frameCounter);
            if (frameCounter == res.frameEnd.Value) break;
            frameCounter++;
        }
        var wantedTotal = (res.frameEnd.Value - res.frameStart);
        if (!res.isSegment || (wantedTotal - framesToIncludePaths.Count > 20))
            Log("paths for:" + framesToIncludePaths.Count + "/" + wantedTotal );

        if(!res.isSegment)
            currentCompressionprocessTotalFrames = framesToIncludePaths.Count;

        var seqfilePath = RootFolder + "seqFile" + new System.Random().Next() + ".txt";
        if (File.Exists(seqfilePath))
            File.Delete(seqfilePath);

        using (var fill = new StreamWriter(seqfilePath)) {
            int cnt = 0;
            double singleFrameDuration = 0.1;
            foreach (var item in framesToIncludePaths)
            {                
                if (cnt != framesToIncludePaths.Count - 1) {
                    var nextFrame = framesToIncludePaths[cnt+1];
                    singleFrameDuration = (frameTimeStamps[framePathsBackwards[nextFrame]] - frameTimeStamps[framePathsBackwards[item]]).TotalSeconds;                    
                }                    


                fill.WriteLine("file 'file:" + item + "'");
                fill.WriteLine("duration " + singleFrameDuration.ToString("0.000000").Replace(',','.'));
                cnt++;
            }
            fill.Flush();            
        }

        Thread.Sleep(300);

        var input = "\"" + seqfilePath + "\"";

        var proc = LaunchFFMPEG(input, outPath, !res.isSegment);
        var tim = System.Diagnostics.Stopwatch.StartNew();

        while (!proc.HasExited)
            Thread.Sleep(100);

        if (!Application.isEditor && !res.isSegment) Debug.Log("[MBUG] Took " + tim.Elapsed.TotalSeconds + " s to compress the bug report flashback video");

        if (!res.isSegment) {
            currentCompressionprocessTotalFrames = null;
        }

        Thread.Sleep(100);

        if (!res.isSegment) Debug.Log("BGVideoCapture SaveReservedRecording DONE " + res.recordingName + " " + outPath);
    }    


    public static bool doDebugTicks = false;

    private void OnGUI()
    {
        GUILayout.Space(160f);
        if(biosStle == null) {
            biosStle = new GUIStyle("Label");
            biosStle.font = Resources.Load<Font>("bioslikefont");
        }

        bool showReservedRecordings = false;

        if (currentCompressionprocessTotalFrames.HasValue) {
            GUILayout.Label("Compressing recording ...\n" + MaxinRandomUtils.GetASCIIProgressBar(lastCompressedFrame / (float)currentCompressionprocessTotalFrames.Value, 40));
        }

        if (showReservedRecordings)
        {
            if (allPendingReservedRecordings.Count > 0) {
                foreach (var item in allPendingReservedRecordings) {
                    GUILayout.Label("ReservedRecording:" + item.recordingName + " (" + item.frameStart + " -> " + (item.frameEnd.HasValue ? item.frameEnd.Value.ToString() : "?") + ")");
                }
            }
        }
        

        if (doDebugTicks) {
            GUILayout.Label("TICK counts");
            var keys = tickCountsLastSnap.Keys.OrderBy(x => x);
            foreach (var item in keys) {
                var now = tickCounts.GetOrDefault(item);
                var last = tickCountsLastSnap.GetOrDefault(item);
                GUILayout.Label(item.PadRight(20) + now.ToString().PadRight(6) + last, biosStle);
            }
            GUILayout.Space(5f);
        }

        bool showScreenShots = false;        

        if (showScreenShots) {
            if (Time.realtimeSinceStartup - timeLastTookScreen < 6f) {
                GUILayout.Label(flippedBigRTForScreenShots2, GUILayout.Width(flippedBigRTForScreenShots.width / 2), GUILayout.Height(flippedBigRTForScreenShots.height / 2));
            }
        }

    }

    GUIStyle biosStle = null;


    public class ReservedRecording
    {
        public string recordingName;
        public int frameStart = 0;
        public int? frameEnd;

        public bool isSegment = false;

        public bool BeenFinalized => !BGVideoCapture.undyingInstance.allPendingReservedRecordings.Contains(this);
        public string FinalizedToPath {
            get {
                if (!BeenFinalized) throw new System.Exception("NOT FINALIZED");
                return finalizedToPath;
            }
        }
        string finalizedToPath;

        public void EndAndSaveReservedRecording(string outPath)
        {
            BGVideoCapture.undyingInstance.EndAndSaveReservedRecording(this, outPath);
        }

        public void SetFinalizedToPath(string outPath)
        {
            finalizedToPath = outPath;
        }
    }


    public string GetSessionStartTimeCompartmentDirectory()
    {
        return GetTimeCompartmentDirectory(startTime);
    }

    public static string GetTimeCompartmentDirectory(DateTime timestamp)
    {
        return timestamp.ToString("dd_MMMM_yyyy", System.Globalization.CultureInfo.InvariantCulture);
    }

    public void PostingSessions(Action<string> onProgressUpdate)
    {
        StartCoroutine(PostOldSessions(onProgressUpdate));
    }


    public static int howManyOldSessionsToPost = 5;
    bool alreadyStartedPostingOld = false;

    public IEnumerator PostOldSessions(Action<string> onProgressUpdate)
    {
        if (alreadyStartedPostingOld)
            yield break;

        alreadyStartedPostingOld = true;

        string txt = "";
        OverrideEditorOptOutforSession();
        AsyncRunnerHelper.RunInSeparateThread(() =>
        {
            PostOldSessionsInThread(ref txt);
        });

        while (true) {
            onProgressUpdate(txt);
            yield return null;
        }
    }

    object addLock = new object();
    public static bool stillSendingOldSessions = false;
    public static int howManyFoldersBeenPosted = 0;

    private void PostOldSessionsInThread(ref string txt)
    {
        var oldSessionsFolder = new DirectoryInfo(RootFolder + "/Uploadable");

        //var userIsDev = new FileInfo(MBugReporter.GetEditorLogFilePath()).Directory.Exists || System.Environment.OSVersion.Platform == PlatformID.Unix; //other condition: builds are currently only on windows so a bit of a guess
        txt = "Starting ...";
        stillSendingOldSessions = true;

        var mainTxt = "";
        var smallTxt = "";

        

        HashSet<string> sentFolders = new HashSet<string>();
        HashSet<string> finalizedFolders = new HashSet<string>();
        List<DirectoryInfo> sendingFolders = new List<DirectoryInfo>();

        while (true)
        {
            if (howManyOldSessionsToPost != sendingFolders.Count) {
                var dirs = oldSessionsFolder.GetDirectories().Where(x => x.Name != SessionID) .OrderByDescending(x => x.CreationTimeUtc);
                var dirsCount = dirs.Count();


                if(howManyOldSessionsToPost > dirsCount) {
                    howManyOldSessionsToPost = dirsCount;
                }
                mainTxt = "See " + dirsCount + " old sessions to send. Sending up to " + howManyOldSessionsToPost;

                var nowPostingDirs = dirs.Take(howManyOldSessionsToPost);

                lock (addLock)
                {
                    sendingFolders.Clear();
                    foreach (var item in nowPostingDirs) {                   
                        sendingFolders.Add(item);                        
                    }
                }
            }


            var unsent = sendingFolders.Where(x => !sentFolders.Contains(x.FullName));
            if (unsent.Any())
            {
                var toSend = unsent.ToList();

                lock (addLock) {
                    foreach (var item in toSend) {
                        sentFolders.Add(item.FullName);
                    }
                }

                AsyncRunnerHelper.RunInSeparateThread(() =>
                {
                    foreach (var item in toSend)
                    {
                        var files = item.GetFiles();

                        foreach (var file in files)
                        {
                            smallTxt = "Uploading " + file.Name;
                            //Thread.Sleep(5000);
                            while (true) {
                                if(UploadSessionFolderContent(file)) { //retry on fail
                                    file.Delete();
                                    break;
                                }
                                Thread.Sleep(2000);
                            }
                            smallTxt = "";
                        }
                        finalizedFolders.Add(item.FullName);
                        howManyFoldersBeenPosted++;
                    }
                });
            }            

            txt = mainTxt + "\n\n" + smallTxt;

            stillSendingOldSessions = finalizedFolders.Count < sendingFolders.Count;

            Thread.Sleep(16);
        }        
    }

#endif
}


#if !DISABLE_MBUG

//info contained in a session folder name
//like SES_133514760420152303_APUSTAJA-PC_Max___BUILD_4c8b4f6
public class BGVideoSessionInfo
{
    //public enum EditorOrBuild { UNSET = 0, Editor = 1, Build = 2 }

    public string fullID;
    public string productName;
    public string startTimeAsID;
    public DateTime startTime;
    public string machineName;
    public string userName;
    public string machineIDExtraPostFix; //unused currently, but previously used for running multiple builds at once on the same machine
    public string platform;
    public string changesetID;

    private static BGVideoSessionInfo ParseFrom(string seshIdToParse)
    {
        var inf = new BGVideoSessionInfo();
        var split = seshIdToParse.Split("_");

        inf.fullID = seshIdToParse;
        inf.productName = split[1];
        inf.startTimeAsID = split[2];
        inf.startTime = DateTime.FromFileTimeUtc(long.Parse(inf.startTimeAsID));
        inf.machineName = split[3];
        inf.userName = split[4];
        inf.machineIDExtraPostFix = split[5];
        inf.platform = split[7];
        inf.changesetID = split[8];

        if (inf.platform == "EDITOR") inf.platform = "WindowsEditor";
        if (inf.platform == "BUILD") inf.platform = "WindowsPlayer";

        return inf;
    }
    private BGVideoSessionInfo() { }

    /*
    public string GetLinkToSession(bool absoluteURL = false)
    {
        var linkToSession = "/fullSessionview?ses=" + fullID;        
        if (absoluteURL) linkToSession = MBugUtility.GetDomain() + linkToSession;

        return linkToSession;
    }    

    public string GetPathInDepository()
    {
        var sessionPath = "/" + productName + "/FullSessions/" + MBugUtility.GetTimeCompartmentDirectory(startTime) + "/" + fullID;
        return sessionPath;
    }
    */


    private static Dictionary<string, BGVideoSessionInfo> preParsed = new Dictionary<string, BGVideoSessionInfo>();

    public static BGVideoSessionInfo GetFromString(string inStr)
    {
        if(!preParsed.TryGetValue(inStr, out var obj)) {
            obj = ParseFrom(inStr);
            preParsed[inStr] = obj;
        }
        return obj;
    }
}
#endif


#if UNITY_EDITOR
public class MBugReporterPreferencesProvider : SettingsProvider
{
    bool badOptionsVisible = false;

    public MBugReporterPreferencesProvider(string path, SettingsScope scope = SettingsScope.User)
    : base(path, scope) { }

    public override void OnGUI(string searchContext)
    {
        EditorGUI.indentLevel++;

        GUILayout.Space(10f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("NOTE: For disabling this system from a published build add the DISABLE_MBUG scripting define symbol.");
        if (GUILayout.Button("Copy to clipboard")) EditorGUIUtility.systemCopyBuffer = "DISABLE_MBUG";
        GUILayout.EndHorizontal();
        GUILayout.Space(10f);

        var prefKey = "MBugReporter_EditorPassiveOptOutEnabled";
        EditorGUIUtility.labelWidth = 400;
        var prev = PlayerPrefs.GetInt(prefKey) == 1;
        var selected = EditorGUILayout.Toggle("Opt out of proactive session recording in dev scenes", prev);


        if (selected && !prev) {
            var header = "Are you entirely sure?";
            var shortBody = "Turning this off will have an impact in debugging long-term behavior and errors.";
            var longBody = "Turning this off will have an impact in debugging long-term behavior and errors for the team as not as diverse, recent data of general game execution will be available.";
            for (int i = 0; i < 100; i++)
            {
                if(UnityEditor.EditorUtility.DisplayCancelableProgressBar(header, shortBody, i / 100f)) {
                    selected = false;
                    break;
                }
                Thread.Sleep(30);
            }
            if (selected) {
                if (!UnityEditor.EditorUtility.DisplayDialog(header, longBody, "Confirm", "Cancel")) {
                    selected = false;
                }

                if (selected) {
                    UnityEditor.EditorUtility.DisplayDialog("Note", "Sending data for reporting bugs might be notably slower this way.\n\nOlder session data will be cached locally, and will be uploadable by pressing F10 in the Bug reporter screen.","OK");
                }
            }
        }
        PlayerPrefs.SetInt(prefKey, selected ? 1 : 0);
        UnityEditor.EditorUtility.ClearProgressBar();


        GUILayout.Space(5f);

        badOptionsVisible = EditorGUILayout.BeginFoldoutHeaderGroup(badOptionsVisible, "Show bad options");

        EditorGUI.indentLevel++;

        if (badOptionsVisible)
        {
            prefKey = "MBugReporter_EditorBreakWholeSystemOptOutEnabled";
            EditorGUIUtility.labelWidth = 400;
            prev = PlayerPrefs.GetInt(prefKey) == 1;
            selected = EditorGUILayout.Toggle("Paralyze system entirely", prev);

            if (selected && !prev) {   
                if (selected) {
                    var sane = false;
                    if (UnityEditor.EditorUtility.DisplayDialog("This is probably not what you want", "Do you want to instead save stuff locally, and optionally send it through user interaction?", "Yes", "No")) {
                        sane = true;                        
                    }
                    else {
                        for (int i = 0; i < 100; i++) {
                            if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Remorse time", " ", i / 100f)) {
                                selected = false;
                                return;
                            }
                            Thread.Sleep(100);
                        }

                        if (UnityEditor.EditorUtility.DisplayDialog("Sure?", "This will hamper all capability of the system, including normal interactive bug reporting as the required data won't be available.\n\nThis still won't disable things like Unity's own local logging, which the normal opt-out is analogous to, but with richer data without a calculable performance impact.", "Abort", "Proceed regardless"))
                        {
                            sane = true;
                        }
                    }


                    if (sane) {
                        UnityEditor.EditorUtility.DisplayDialog("Above option", "You can select \"Opt out of proactive session recording in dev scenes\" instead.\n\nThis will preserve the interactivity-based F8 dialog, and will cache sessions started from dev scenes locally.\n\nNote that this still has some downsides and friction compared to the normal proactive mode.", "OK");
                        selected = false;
                    }

                    if (selected) {
                        UnityEditor.EditorUtility.DisplayDialog("Note", "This is not an option intended for any common development use, and has no known technically-based reasoning. Please do inform you selected this, and why.", "OK");
                    }
                }
            }
            PlayerPrefs.SetInt(prefKey, selected ? 1 : 0);
        }
        UnityEditor.EditorUtility.ClearProgressBar();



        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;

        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    
    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        return new MBugReporterPreferencesProvider("Preferences/MBugReporter");
    }
}
#endif