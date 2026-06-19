using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//You can either put this stub to your scene, or call ReproTrace.InitializeReproTrace() from anywhere.
//The system stays alive between scene swithces automatically.
public class ReproTrace : MonoBehaviour
{
    static ReproTrace internalInstance;

    private void Awake()
    {
        internalInstance = this;
    }
    private void Start()
    {        
        InitializeReproTrace();
    }

    //You can initialize the system just by calling this from anywhere, or adding the ReproTrace prefab. Both work.
    public static void InitializeReproTrace()
    {
        if (ReproTraceClientConfiguration.Resource == null) {
            Debug.LogError("ReproTrace configuration is missing. Halting system.");
            MBugCustomBackEndUploader.systemHaltedDueToMisconfiguration = true;
            return;
        }

        var existing = MBugReporter.HasCachedInstance ? MBugReporter.Instance : null;
        if(existing != null) {
            return; //it already existing is a common case, don't need to log
        }
                
        var prefab = Resources.Load<GameObject>("ReproTraceMainCanvas");        
        var copy = Instantiate(prefab, internalInstance?.transform);
        var rootThing = internalInstance != null ? internalInstance.transform : copy.transform;
        rootThing.transform.SetParent(null);
        DontDestroyOnLoad(rootThing.gameObject);
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ReproTrace))]
    public class Inspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            UnityEditor.EditorGUILayout.HelpBox("Put this to your menu or game scene. This will un-parent and stay alive for the entirety of your game. Alternatively, you can just call ReproTrace.InitializeReproTrace()", UnityEditor.MessageType.Info);
        }        
    }
#endif


    //#################### !!!! GAME - SPECIFIC !!!!!
    //#################### !!!! IMPLEMENT FOR BETTER BUG REPORTS !!!!!
    public delegate void BugReportCustomDataDelegate(string bugReportFolderPath);

    public static BugReportCustomDataDelegate onProvideGameSpecificBugReporterData;

    internal static HashSet<string> filesAlreadyAdded = new();

    public static void AddContentToSession(string filePath, bool deleteFileAfter = false, bool supportInstantRewrite = false)
    {
        var pathToUse = filePath;
        if(filesAlreadyAdded.Contains(filePath)) {
            var test = filePath;
            int cnt = 1;
            while (filesAlreadyAdded.Contains(test)) {
                test = filePath+"_REPVER_"+cnt;
                cnt++;
            }
            pathToUse = test;
        }
        filesAlreadyAdded.Add(pathToUse);

        if(pathToUse == filePath && !supportInstantRewrite) {
            MCrashReporterHost.DumpExtraDataToVideoFolder(pathToUse, deleteFileAfter);
        }
        else {
            if(pathToUse != filePath) File.Copy(filePath, pathToUse, true);
            if (supportInstantRewrite) {
                var tempDirForThis = Path.Combine(BGVideoCapture.RootFolder, "fastSyncs");
                Directory.CreateDirectory(tempDirForThis);
                var copyToFast = Path.Combine(tempDirForThis, new FileInfo(pathToUse).Name);
                File.Copy(pathToUse, copyToFast);
                pathToUse = copyToFast;
            }

            var pathIsUnchanged = pathToUse == filePath;
            var doStillDelete = pathIsUnchanged ? deleteFileAfter : true;

            MCrashReporterHost.DumpExtraDataToVideoFolder(pathToUse, doStillDelete);
            if (deleteFileAfter) Debug.LogWarning("will leak " + pathToUse + " because same-named existed already, autodel logic won't work unless you dedup the filenames yourself");
        }
    }
}
