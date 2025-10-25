using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


public class MBugReporter : MUtility.Singleton<MBugReporter>
{
    //added 17.3.2024.
    //version 0 (everything before): various sets of data, but mainly no streaming data
    //version 1 streaming data, with logs and frame data
    public const int VERSION = 1;


    public const string kFakeProjectNameToReplace = "REPLCPRODNAME";

#if UNITY_EDITOR
    static MBugReporter() {
        UnityEditor.EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        isInPlayMode = false;
    }
    private static void EditorApplication_playModeStateChanged(UnityEditor.PlayModeStateChange obj)
    {
        mainThread = Thread.CurrentThread;
        if (obj == UnityEditor.PlayModeStateChange.EnteredPlayMode) isInPlayMode = true;
        if (obj == UnityEditor.PlayModeStateChange.EnteredEditMode) isInPlayMode = false;
    }
#endif

    static bool isInPlayMode = false;
    static Thread mainThread;

    static bool IsInUnityEditor {
        get {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }

    public static string ExecutablesRootPath {
        get {
            if (IsInUnityEditor) {
                return ExecutablesRootPathInEditor;
            }
            else {
                return ExecutablesRootPathInPlayer;
            }
        }
    }

    static bool CanSafelyCallMainThreadAPI {
        get {
            return !isInPlayMode || (IsInUnityEditor && Thread.CurrentThread == mainThread);
        }
    }

    public static string ExecutablesRootPathInEditor
    {
        get {
            if(CanSafelyCallMainThreadAPI) BGVideoCapture.EnsureHavePathsFromMainThread();
            //var editorPath = $"{BGVideoCapture.dataPath}/MBugReporter/MBugReporterConditionalStreamingAssetsAdditions~/MBug";
            var rootPath = Path.GetFullPath("Packages/com.reprotrace.reprotrace-unityclient");
            var editorPath = $"{rootPath}/ReproTraceClientConditionalStreamingAssetsAdditions~/ReproTrace";
            return editorPath;
        }
    }
    public static string ExecutablesRootPathInPlayer
    {
        get {
            if (CanSafelyCallMainThreadAPI) BGVideoCapture.EnsureHavePathsFromMainThread();
            var pathInPlayer = $"{BGVideoCapture.streamingAssetsPath}/ReproTrace";
            return pathInPlayer;
        }
    }


    public bool dontDestroyOnLoad = false;    

    [Space]
    public GameObject toggledRoot;

    public GameObject doneWindow;

    public GameObject uploadInProgressGO;
    public Image progressBar;

    public GameObject trelloPostReminder;
    public Image trelloPostProgressBar;
    public Image holdToPostProgressBar;

    public GameObject postingToTrelloNote;
    public Text insidePostingToTrelloNoteText;

    public GameObject postedToTrelloNote;


    public GameObject postingOldSessionsNote;
    public Text postingOldSessionsText;
    public Text postingOldSessionsInteractivityNote;

    public static bool BugReporterWindowIsOpen => MBugReporter.HasCachedInstance && MBugReporter.Instance.toggledRoot.activeInHierarchy;

    public GameObject systemHamperedReminder;
    public GameObject systemExtraHamperedReminder;


    static string applicationDotTempDataPath;
    static string applicationDotPersistentDataPath = null;


    float showShadowplayWaitTill = -1f;
    public GameObject waitShadowplayPanel;

    float showShadowplayFailureTill = -1f;
    public GameObject shadowplayFailurePanel;
    public Text shadowplayFailureText;
    public Text shadowplayFailureTextLongNote;

    public Text idText;
    public InputField inputField;

    public GameObject badTokenGO;

    public GameObject waitStateObj;
    public Text waitStateText;


    private new void Awake()
    {
        base.Awake();
        if(dontDestroyOnLoad) DontDestroyOnLoad(gameObject);        
    }




    private void Start()
    {
        toggledRoot.SetActive(false);
        trelloPostReminder.SetActive(false);
        postedToTrelloNote.SetActive(false);
        postingToTrelloNote.SetActive(false);
        postingOldSessionsNote.SetActive(false);

        uploadInProgressGO.SetActive(false);

        applicationDotTempDataPath = Application.temporaryCachePath;
        applicationDotPersistentDataPath = Application.persistentDataPath;

        inputField.characterLimit = 200;
    }


#if !DISABLE_MBUG
    public static Action onUpdate;
    public static Action onFixedUpdate;

    private void FixedUpdate()
    {
        try { if (onFixedUpdate != null) onFixedUpdate(); } catch (Exception e) { Debug.LogException(e); }
    }
#endif

    // Update is called once per frame
    void Update()
    {
        if(BGVideoCapture.ShouldSelfDisable) {
            uploadInProgressGO.SetActive(false);
            trelloPostReminder.SetActive(false);
            postedToTrelloNote.SetActive(false);
            postingToTrelloNote.SetActive(false);
            postingOldSessionsNote.SetActive(false);            
            if (Input.GetKeyDown(KeyCode.F8)) {
                Debug.LogError("MBugReporter has been disabled in this configuration.");

                StartCoroutine(KeepSystemDisabledVisibleForShortWhile());                
            }
            return;
        }
        badTokenGO.SetActive(MBugCustomBackEndUploader.systemHaltedDueToMisconfiguration);

#if !DISABLE_MBUG
        try { if (onUpdate != null) onUpdate(); } catch(Exception e) { Debug.LogException(e); }

        UpkeepFlashbackBuffer();
        CheckPendingRecordingsToSend();


        if (isPostingOldSessions)
            return;

        if (Input.GetKeyDown(KeyCode.F8)) {
            ToggleWindow();
        }

        if (toggledRoot.activeSelf)
        {
            var isInHaveToWaitState = MBugCustomBackEndUploader.obtainedProjConfiguration == null;

            waitStateObj.SetActive(isInHaveToWaitState);
            if (isInHaveToWaitState) {
                waitStateText.text = "Early press - Please wait few seconds for project configuration ...";
                if(Time.time > 20f) {
                    waitStateText.text += "\n(Taking too long - Probably failing to communicate with server)";
                }
            }
            

            if (!closeAfter.HasValue) {
                UpdateID();
                if(!isInHaveToWaitState) SubmitCheck();
                OldSessionPostCheck();
            }

            if(closeAfter.HasValue && Time.realtimeSinceStartup > closeAfter) {
                ToggleWindow();
            }

        }

        var submissionInProgress = isUploading;
        if(pendingFinalLocationsForRecords.Count > 0) {
            submissionInProgress = true;
        }
        var noNeedDueToMainPanel = toggledRoot.activeSelf && !doneWindow.activeSelf;
        if (Time.frameCount - frameLastOpened < 10) noNeedDueToMainPanel = true;

        var inProg = submissionInProgress && !noNeedDueToMainPanel;
        if (shadowplaysInProg > 0) inProg = true;
        uploadInProgressGO.SetActive(inProg);
        if (inProg) {
            progressBar.fillAmount = GetSomeProgress();
        }

        waitShadowplayPanel.SetActive(Time.realtimeSinceStartup < showShadowplayWaitTill);
        shadowplayFailurePanel.SetActive(showShadowplayFailureTill > Time.realtimeSinceStartup);
#endif
    }


    private IEnumerator KeepSystemDisabledVisibleForShortWhile()
    {
        systemHamperedReminder.SetActive(true);
        systemExtraHamperedReminder.SetActive(Application.isEditor && PlayerPrefs.GetInt("MBugReporter_EditorBreakWholeSystemOptOutEnabled") == 1);

        yield return new WaitForSeconds(3f);

        systemHamperedReminder.SetActive(false);
    }

#if !DISABLE_MBUG
    float GetSomeProgress()
    {
        if (MDropboxAPI.GetSomeProgress() != 0 && MDropboxAPI.GetSomeProgress() != 1) return MDropboxAPI.GetSomeProgress();
        if (MBugCustomBackEndUploader.someProgress != 0 && MBugCustomBackEndUploader.someProgress != 1) return MBugCustomBackEndUploader.someProgress;
        return 0f;
    }

    int frameLastOpened = -100;

    private void CheckPendingRecordingsToSend()
    {
        var keys = pendingFinalLocationsForRecords.Keys.ToList();

        foreach (var item in keys)
        {
            if (item.BeenFinalized) {
                var value = pendingFinalLocationsForRecords[item];
                if (value != null) {
                    var theItem = item;
                    var theValue = value;
                    AsyncRunnerHelper.RunInSeparateThread(() => {
                        SubmitFile(theItem.FinalizedToPath, theValue);
                        File.Delete(theItem.FinalizedToPath);
                        DeleteDirIfEmpty(new FileInfo(theItem.FinalizedToPath).Directory.FullName);
                    });
                }
                pendingFinalLocationsForRecords.Remove(item);
            }
        }
    }


    public string DropboxReportsRoot => "Projects/"+kFakeProjectNameToReplace+"/MBugReports/" + BGVideoCapture.undyingInstance.GetSessionStartTimeCompartmentDirectory();

    private void CreateSubmitFolder(string fullID)
    {
        var fold = DropboxReportsRoot + "/" + fullID;        
        MBugContentUploader.CreateFolder(fold);

        //var shareLink = MDropboxAPI.GetFolderShareLink(fold);
        
    }

    private void SubmitFile(string diskPath, string reportFolder)
    {
        Log("SubmitFile " + diskPath + " " + reportFolder);
        MBugContentUploader.UploadFileToDepository(diskPath, DropboxReportsRoot + "/" + reportFolder + "/" + new FileInfo(diskPath).Name);
    }


    private BGVideoCapture.ReservedRecording submittingFlashbackRecording;
    BGVideoCapture.ReservedRecording flashbackRecording = null;

    private void UpkeepFlashbackBuffer()
    {
        if (BGVideoCapture.undyingInstance == null)
            return;

        if(flashbackRecording == null) {
            flashbackRecording = BGVideoCapture.StartReservedRecording("bugreporter_flashback");
        }

        int framesToKeep = (int)(BGVideoCapture.undyingInstance.FPS * 60f); //good enough

        var frameStart = BGVideoCapture.TotalFrameNum - framesToKeep;
        if (frameStart < 0) frameStart = 0;
        flashbackRecording.frameStart = frameStart;
        flashbackRecording.frameEnd = null;
    }

    float? closeAfter = null;

    private void SubmitCheck()
    {
        if (Input.GetKeyDown(KeyCode.Return)) {
            Submit();
        }
    }

    public class BugFormData
    {
        public string writtenDescription;
    }

    private void Submit()
    {
        var bugForm = new BugFormData { writtenDescription = inputField.text };
        System.IO.File.WriteAllText(Path.Combine(currentSnapShotPath, "bugForm.txt"), JsonUtility.ToJson(bugForm, true));


        var websiteRoot = MBugCustomBackEndUploader.Domain;
        var urlWithID = websiteRoot + "/bugreportview?bug="+ fullID;
        GUIUtility.systemCopyBuffer = TransformIDToHaveProjectName(urlWithID);

        Log("submitting bug with ID:" + fullID);

        if (MBugCustomBackEndUploader.obtainedProjConfiguration.projectHasTrelloEnabled) {
            StartCoroutine(PostBugTrelloNoting());
        }

        doneWindow.SetActive(true);
        closeAfter = Time.realtimeSinceStartup + 3f;


        AsyncRunnerHelper.RunInSeparateThread(() => {
            SubmitData();
        });
    }

    private IEnumerator PostBugTrelloNoting()
    {
        while(Time.time < closeAfter + 0.5f) {
            yield return null;
        }

        trelloPostReminder.SetActive(true);

        const float requireHoldTime = 1f;
        const float goAwayTime = 10f;

        float goAwayTimer = 0f;
        float holdingTimer = 0f;
        while (true) {
            var holding = Input.GetKey(KeyCode.T);

            if (holding) {
                holdingTimer += Time.deltaTime;
                goAwayTimer = 0f;
            }
            else {
                goAwayTimer += Time.deltaTime;
                holdingTimer = 0f;
            }
            trelloPostProgressBar.fillAmount = goAwayTimer / goAwayTime;
            holdToPostProgressBar.fillAmount = holdingTimer / requireHoldTime;

            if (goAwayTimer > goAwayTime) {
                trelloPostReminder.SetActive(false);
                break;
            }
            if(holdingTimer > requireHoldTime) {
                trelloPostReminder.SetActive(false);

                postingToTrelloNote.SetActive(true);

                //yield return new WaitForSeconds(2f);
                bool done = false;
                bool success = false;
                AsyncRunnerHelper.RunInSeparateThread(() =>
                {
                    try
                    {
                        var addToTrelloEndPoint = MBugCustomBackEndUploader.Domain + "/api/BugReports/AddToTrello";
                        using (var client = new HttpClient())
                        {
                            var fullUrl = addToTrelloEndPoint + "?token=" + MBugCustomBackEndUploader.configurationAPIToken + "&bug=" + TransformIDToHaveProjectName(fullID);
                            var result = client.GetStringAsync(fullUrl).Result;
                            if(!result.Contains("{") || !result.Contains("}") || !result.Contains("idShort")) {
                                success = false;
                            }
                            else success = true;
                            Debug.Log("add to trello result:" + success + " " + result);
                            done = true;
                        }
                    }
                    catch(System.Exception e)
                    {
                        Debug.LogError(e);
                    }
                });

                while (!done) {
                    yield return null;
                }
                

                //var success = UnityEngine.Random.value > 0.5f;
                if (success) {
                    postingToTrelloNote.SetActive(false);
                    postedToTrelloNote.SetActive(true);
                }
                else {
                    insidePostingToTrelloNoteText.text = "Failed posting to Trello!";
                    yield return new WaitForSeconds(2f);
                }                

                yield return new WaitForSeconds(3f);

                postingToTrelloNote.SetActive(false);
                postedToTrelloNote.SetActive(false);
                yield break;
            }

            yield return null;
        }
    }

    private void Log(string v)
    {
        Debug.Log("MBugReporter " + v);
    }

    bool isUploading = false;

    private void SubmitData()
    {
        if (MBugCustomBackEndUploader.systemHaltedDueToMisconfiguration)
            return;

        isUploading = true;

        try {            

            var finalIDToUse = fullID;

            CreateSubmitFolder(fullID);

            //the saving might be ready, or might not be ready, at the time of submitting, so this needs to be super smart and async
            pendingFinalLocationsForRecords[submittingFlashbackRecording] = finalIDToUse;

            baseIDsToFinalIDs.Add(baseID, finalIDToUse);

            var files = new DirectoryInfo(currentSnapShotPath).GetFiles();
            foreach (var item in files) {
                if (item.Name == "flashback.mp4") continue;
                SubmitFile(item.FullName, finalIDToUse);
                File.Delete(item.FullName);
            }
            DeleteDirIfEmpty(currentSnapShotPath);

            var startSegs = BGVideoCapture.fileUploadPassesCompleted;
            BGVideoCapture.undyingInstance.OverrideEditorOptOutforSession();
            while (BGVideoCapture.fileUploadPassesCompleted == startSegs) {
                Thread.Sleep(10);
            }
        }
        finally {
            isUploading = false;
        }
    }

    private void DeleteDirIfEmpty(string dir)
    {
        Debug.Log("DeleteDirIfEmpty "+dir);
        if (Directory.Exists(dir) && Directory.GetFiles(dir).Length == 0) {
            Debug.Log("DeleteDirIfEmpty committing");
            Directory.Delete(dir);
        }
    }

    private void UpdateID()
    {
        fullID = baseID + "_" + UnSpace(inputField.text);
        idText.text = TransformIDToHaveProjectName(fullID);
    }

    

    public static string TransformIDToHaveProjectName(string id)
    {
        var projName = kFakeProjectNameToReplace;
        if (MBugCustomBackEndUploader.obtainedProjConfiguration != null) projName = MBugCustomBackEndUploader.obtainedProjConfiguration.projectName;
        return id.Replace(kFakeProjectNameToReplace, projName);
    }

    public static string UnSpace(string str)
    {
        var charARr = str.ToCharArray();
        for (int i = 0; i < str.Length; i++)
        {
            if (i != str.Length - 1)
            {
                if (charARr[i] == ' ')
                {
                    charARr[i + 1] = charARr[i + 1].ToString().ToUpper()[0];
                }
            }
        }
        str = new string(charARr);
        str = str.Replace(" ", "");
        str = str.Replace(".", "");
        str = RemoveBadCharsBesidesSpaces(str);

        return str;
    }

    public static string RemoveBadCharsBesidesSpaces(string inputString)
    {
        var allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";

        var str = "";
        foreach (var item in inputString)
        {
            if (!allowedChars.Contains(item))
                continue;

            str += item;
        }
        return str;
    }


    public string baseID;
    public string fullID;



    private void ToggleWindow()
    {
        var isOpening = !toggledRoot.activeSelf;
        var isManualClose = !doneWindow.activeSelf;

        doneWindow.SetActive(false);
        closeAfter = null;

        if (isOpening)
        {
            //shadowplaysInProg = 0;

            CreateBaseID();
            CreateSnapshot();
            frameLastOpened = Time.frameCount;

            MaxinRandomUtils.DoActionAfterFrames(() => {
                toggledRoot.SetActive(true);
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(inputField.gameObject);
                inputField.ActivateInputField();
            }, 2f, TimeType.Frames);                       


        }
        else {
            if (isManualClose) {
                Log("Cancelled posting bug " + baseID);
            }

            toggledRoot.SetActive(false);

            if (!baseIDsToFinalIDs.ContainsKey(baseID)) {
                baseIDsToFinalIDs.Add(baseID, null);
            }
        }
    }




    int shadowplaysInProg = 0;

    private void OnShadowplayFlashback(string forBaseID, string videoPath) 
    {
        if (string.IsNullOrEmpty(videoPath)) {
            ShadowplayFailure("Failed to capture / find DVR file", null);
            shadowplaysInProg = 0;
            return;
        }

        AsyncRunnerHelper.RunInSeparateThread(() => {
            while (true) {
                if(!baseIDsToFinalIDs.ContainsKey(forBaseID)) {
                    Thread.Sleep(500);
                    continue;
                }
                var finalID = baseIDsToFinalIDs[forBaseID];
                if(finalID == null) {
                    shadowplaysInProg--;
                    return; //either the report was cancelled (window closed) or the DVR capture failed to capture&find the DVR
                }

                try {
                    if (!string.IsNullOrEmpty(videoPath)) {
                        SubmitFile(videoPath, finalID);
                    }
                }
                catch(System.Exception e) {
                    Debug.LogException(e);
                }

                shadowplaysInProg--;
                return;
            }
        });
    }

    Dictionary<string, string> baseIDsToFinalIDs = new Dictionary<string, string>();

    public string LocalBugReportsRoot => Path.Combine(applicationDotTempDataPath, "MBugReporter");

    string currentSnapShotPath;

    private void CreateSnapshot()
    {
        Log("CreateSnapshot baseID:" + baseID);

        currentSnapShotPath = Path.Combine(LocalBugReportsRoot, baseID);
        Directory.CreateDirectory(currentSnapShotPath);

        ProduceCurrentLogTo(currentSnapShotPath);        
        BGVideoCapture.CaptureScreenShot(Path.Combine(currentSnapShotPath, "screenshot.png"));

        CopyGameSpecificDataToBugReport(currentSnapShotPath);


        if (flashbackRecording != null) {
            flashbackRecording.EndAndSaveReservedRecording(Path.Combine(currentSnapShotPath, "flashback.mp4"));
            submittingFlashbackRecording = flashbackRecording;
            flashbackRecording = null;
        }


        pendingFinalLocationsForRecords[submittingFlashbackRecording] = null;


        if (UseShadowplayFeature) {
            showShadowplayWaitTill = Time.realtimeSinceStartup + 2.1f;
            var tempbaseID = baseID;
            try {
                ShadowplayManipulator.Instance.AttemptToCaptureFlashback((x) => OnShadowplayFlashback(tempbaseID, x));
            }
            catch(System.Exception e) {
                ShadowplayFailure("Init failure", e);
            }
            shadowplaysInProg++;
        }
    }

    private void ShadowplayFailure(string v, Exception e)
    {
        Debug.LogError("ShadowplayFailure " + v + " " + (e != null ? e.ToString() : ""));

        AsyncRunnerHelper.RunOnMainThread(() =>
        {
            showShadowplayFailureTill = Time.realtimeSinceStartup + 15f;
            shadowplayFailureText.text = v;
            shadowplayFailureTextLongNote.text = "The system requires Shadowplay output directory to be " + ShadowplayManipulator.GetDefaultVideosFolder();
        });
    }

    Dictionary<BGVideoCapture.ReservedRecording, string> pendingFinalLocationsForRecords = new Dictionary<BGVideoCapture.ReservedRecording, string>();


    private void ProduceCurrentLogTo(string folder)
    {
        bool isEditor = false;
#if UNITY_EDITOR
        isEditor = true;
#endif

        //If we're in editor, don't produce the entire log file, instead only the part after entering playmode
        if (isEditor)
        {
            var newPath = Path.Combine(folder, "Editor_cut.log");
            GetEditorLogLinesAfterPlaymodeEnterAndCopyTo(newPath);            
        }
        else
        {
            var logPath = GetPlayerLogFilePath(BGVideoCapture.applicationDotCompanyName,BGVideoCapture.applicationDotProductName);
            File.Copy(logPath, Path.Combine(folder, "Player.log"));
        }
    }



    private void CreateBaseID()
    {
        string id;

        if(BGVideoCapture.undyingInstance != null)
        {
            id = "MBUG_" + BGVideoCapture.SessionID + "_" + System.DateTime.UtcNow.ToFileTime() + "_" + TimeStr(BGVideoCapture.undyingInstance.RunningTime);            
        }
        else
        {
            id = "FAILURE_BGVIDEOCAPTURE_NOT_ON";
        }

        inputField.text = "";
        baseID = id;
        UpdateID();
    }

    private string TimeStr(TimeSpan runningTime)
    {
        return Mathf.FloorToInt((float)runningTime.TotalMinutes) + "m" + runningTime.Seconds + "s";
    }



    //[UnityEditor.MenuItem("Tools/TestEditorLogClipping")]
    public static void TestEditorLogClipping()
    {
        GetEditorLogLinesAfterPlaymodeEnterAndCopyTo(@"C:\temp\editorClippedTest.log");
    }

    public static void GetEditorLogLinesAfterPlaymodeEnterAndCopyTo(string copyTo)
    {
        GetTailOfLogAfterLastOccurenceOfGivenLinesAndCopyTo(GetEditorLogFilePath(), copyTo,
            "Initialize engine version:",
            "Entering Playmode",
            "Mono: successfully reloaded assembly");
    }



    private static void GetTailOfLogAfterLastOccurenceOfGivenLinesAndCopyTo(string inputTextFilePath, string outputTextFilePath, params string[] afterLines)
    {
        var size = new FileInfo(inputTextFilePath).Length;
        var startPointInFile = size - 10000000;
        if (startPointInFile < 0) startPointInFile = 0;
        var fs = new FileStream(inputTextFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var sr = new StreamReader(fs);
        sr.BaseStream.Position = startPointInFile;
        long firstLinePoint = 0;        


        int row = 0;
        int takeFromRow = 0;

        while (true)
        {            
            var line = sr.ReadLine();            
            if (firstLinePoint == 0) firstLinePoint = sr.BaseStream.Position;

            if (line == null) {
                break;
            }

            foreach (var item in afterLines)
            {
                if (line.Contains(item)) {
                    takeFromRow = row;                    
                    //Debug.Log(pos+" "+item+" "+ line);                    
                }
            }
            row++;
        }
        sr.Dispose();
        fs.Dispose();
        

        if (File.Exists(outputTextFilePath))
            File.Delete(outputTextFilePath);


        var fs2 = new FileStream(inputTextFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var sr2 = new StreamReader(fs2);
        sr2.BaseStream.Position = startPointInFile;
        row = 0;

        var sw = new StreamWriter(outputTextFilePath);

        while (true)
        {
            var line = sr2.ReadLine();
            if(line == null) {
                break;
            }

            if(row >= takeFromRow) {
                sw.WriteLine(line);
            }
            row++;
        }
        sw.Dispose();
        sr2.Dispose();
        fs2.Dispose();
    }

    public static string GetPlayerLogFilePath(string companyName, string productName) {        
        if(System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
            var appDataRoot = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Parent.FullName;
            var finalPath = @$"{appDataRoot}\LocalLow\{companyName}\{productName}";
            return Path.Combine(finalPath, "Player.log");
        }
        else {
            //right now just assumes Mac
            return $"/Users/{System.Environment.UserName}/Library/Logs/{companyName}/{productName}/Player.log";
        }
    }
    public static string GetEditorLogFilePath() {
        if(System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
            return System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Unity\\Editor\\Editor.log";
        }
        else {
            //right now just assumes Mac
            return $"/Users/{System.Environment.UserName}/Library/Logs/Unity/Editor.log";
        }
    }


    private void OldSessionPostCheck()
    {
        if (Input.GetKey(KeyCode.F10)) {
            StartCoroutine(PostingOldSessions());
        }
    }

    bool isPostingOldSessions = false;

    private IEnumerator PostingOldSessions()
    {
        if (isPostingOldSessions)
            yield break;

        isPostingOldSessions = true;
        postingOldSessionsNote.SetActive(true);

        string uninteractiveNote = "Window can be closed after uploads are complete.";
        

        StartCoroutine(BGVideoCapture.undyingInstance.PostOldSessions( (progressText) =>
        {
            postingOldSessionsText.text = progressText;
        }));
        yield return null;
        yield return null;
        while (true) {
            yield return null;

            if(BGVideoCapture.stillSendingOldSessions) {
                postingOldSessionsInteractivityNote.text = uninteractiveNote;
            }
            else {
                postingOldSessionsInteractivityNote.text = "Uploads complete. Press ESC to close.";
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    postingOldSessionsNote.SetActive(false);
                    toggledRoot.SetActive(false);
                    isPostingOldSessions = false;
                    break;
                }
            }
            postingOldSessionsInteractivityNote.text += $"\n Uploaded {BGVideoCapture.howManyFoldersBeenPosted} of target {BGVideoCapture.howManyOldSessionsToPost}. Press F10 to increase how much to upload.";

            if (Input.GetKeyDown(KeyCode.F10) && postingOldSessionsNote.activeInHierarchy) {
                BGVideoCapture.howManyOldSessionsToPost += 5;
            }
        }
    }

    private void CopyGameSpecificDataToBugReport(string currentSnapShotPath)
    {
        //For reference:
        //Make a custom debug save from this very moment and save it
        //Take last saved save / a few previous saves and save them
        //Copy options / profile files to bug report too
        if (onProvideGameSpecificBugReporterData != null)
        {
            onProvideGameSpecificBugReporterData(currentSnapShotPath);
        }
    }
#endif

    public static bool UseShadowplayFeature
    {
        get
        {
            if (PlayerPrefs.HasKey("UseMBugReporterShadowplayFeature"))
            {
                return PlayerPrefs.GetInt("UseMBugReporterShadowplayFeature") == 1;
            }
            //if (System.Environment.MachineName == "KEKW-REBORN") return true;
            //if (System.Environment.MachineName == "APUSTAJA-PC") return true;
            return false;
        }
        set
        {
            PlayerPrefs.SetInt("UseMBugReporterShadowplayFeature", value ? 1 : 0);
            Debug.Log("UseShadowplayFeature:" + UseShadowplayFeature);
        }
    }

    public static void CopyToOtherFolderIfExists(string filePath, string destinationFolderPath)
    {
        if (!File.Exists(filePath))
            return;

        var destFilePath = Path.Combine(destinationFolderPath, new FileInfo(filePath).Name);
        File.Copy(filePath, destFilePath, true);
    }

    //#################### !!!! GAME - SPECIFIC !!!!!
    //#################### !!!! IMPLEMENT FOR BETTER BUG REPORTS !!!!!
    public delegate void BugReportCustomDataDelegate(string bugReportFolderPath);

    public static BugReportCustomDataDelegate onProvideGameSpecificBugReporterData;

}
