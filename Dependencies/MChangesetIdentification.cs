using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//Gets and retains changeset (commit) information both in build and editor. Know what you run.
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class MChangesetIdentification
#if UNITY_EDITOR
    : UnityEditor.Build.IPostprocessBuildWithReport
#endif
{
    static MChangesetIdentification()
    {
        applicationDotDatapath = Application.dataPath;
        applicationDotStreamingAssetsPath = Application.streamingAssetsPath;
        isEditor = Application.isEditor;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += (x) =>
        {
            if (x == UnityEditor.PlayModeStateChange.ExitingEditMode) {
                UpdateCachedChangesetNumber();
            }
        };
#endif
    }

    public static string applicationDotDatapath;
    public static string applicationDotStreamingAssetsPath;
    public static bool isEditor;


    public static string GetShortChangesetHash()
    {
        return GetChangesetHash().Substring(0, 7);
    }

    public static string GetChangesetHash()
    {
        if(cachedChangesetNumber == "CHANGESET-UNSET") {
            UpdateCachedChangesetNumber();
        }
        return cachedChangesetNumber;
    }

    static string cachedChangesetNumber = "CHANGESET-UNSET";

    static string ChangesetFilePathInStreamingAssets => Path.Combine(applicationDotStreamingAssetsPath, "changeset.txt");

    

    private static void UpdateCachedChangesetNumber()
    {
        if (isEditor)
        {
            cachedChangesetNumber = EvaluateChangeSetInEditor();        
        }
        else
        {
            cachedChangesetNumber = EvaluateChangesetInBuild();
        }
    }

    private static string EvaluateChangesetInBuild()
    {
        if (File.Exists(ChangesetFilePathInStreamingAssets))
        {
            return File.ReadAllText(ChangesetFilePathInStreamingAssets);
        }
        else
        {
            return "CSFAIL-NOFILEINBUILD";
        }
    }




#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/TestEvaluateChangeSetInEditor")]
#endif
    public static void TestEvaluateChangeSetInEditor()
    {
        Debug.Log(EvaluateChangeSetInEditor());
    }

    public static string EvaluateChangeSetInEditor()
    {
        var dir = new DirectoryInfo(applicationDotDatapath);

        while (true)
        {
            dir = dir.Parent;
            if(dir == null) {
                Debug.LogWarning("EvaluateChangeSetInEditor: Failed, git folder not found");
                return "CSFAIL-NOTFOUND";
            }
            var dirs = dir.GetDirectories();
            var gitDir = dirs.FirstOrDefault(x => x.Name == ".git");
            if(gitDir != null) {
                return GetCommitHashFromGitDir(gitDir.FullName);
            }
        }
    }

    private static string GetCommitHashFromGitDir(string gitDir)
    {
        try
        {
            var headContent = File.ReadAllText(Path.Combine(gitDir, "HEAD"));
            var refText = "ref: ";
            if (headContent.StartsWith(refText))
            {
                var pointedPath = headContent.Substring(refText.Length);
                pointedPath = pointedPath.TrimEnd('\n');
                var contentPath = Path.Combine(gitDir, pointedPath);
                var content = File.ReadAllText(contentPath);
                content = content.TrimEnd('\n');
                return content;
            }
            else
            {
                return headContent;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("EvaluateChangeSetInEditor: Failed "+e.Message);
            return "CSFAIL-EXC";
        }
    }

#if UNITY_EDITOR
    public int callbackOrder => 0;

    public void OnPostprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        OutputChangesetInfoFileToBuildFolder(report);
    }

    private static void OutputChangesetInfoFileToBuildFolder(UnityEditor.Build.Reporting.BuildReport report)
    {
        //don't confuse this with Application.streamingAssetsPath, because this is the BUILD's path, which we're not currently running!
        var buildDir = new FileInfo(report.summary.outputPath).Directory;
        var dataFolder = buildDir.GetDirectories().SingleOrDefault(x => x.Name.EndsWith("_Data"));
        string dataFolderPath;

        if (dataFolder == null)  {
            //return; //mac or some other platform
            var appBundleFolder = report.summary.outputPath;
            if (!Directory.Exists(appBundleFolder)) {
                Console.WriteLine("MChangesetIdentification OutputChangesetInfoFileToBuildFolder: build folder doesn't exist, attempting to add \".app\" to the end of it");
                appBundleFolder = appBundleFolder + ".app";
            }
            dataFolderPath = Path.Combine(appBundleFolder, "Contents", "Resources", "Data");            
        }
        else dataFolderPath = dataFolder.FullName;

        
        var buildStreamingAssetsPath = Path.Combine(dataFolderPath, "StreamingAssets");
        Directory.CreateDirectory(buildStreamingAssetsPath);
        var changesetFilePath = Path.Combine(buildStreamingAssetsPath, "changeset.txt");

        var changeset = GetChangesetHash();
        File.WriteAllText(changesetFilePath, changeset);
    }
#endif
}

