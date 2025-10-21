#if !DISABLE_MBUG
using MUtility;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class MBugReporterClientConfiguration : ResourceSingleton<MBugReporterClientConfiguration>
{
    public static string kDefaultBackEndURL = "https://reprotrace.com";
    //public static string kDefaultBackEndURL = "http://localhost:7192";    

    public string projectAPIToken;

    //TODO remove specific values  from code before ship
    public string appNameIfDifferentFromPlayerSettings;
    public string normalStartupSceneForGame;


    public bool customBackEndSupportsTrello = false;

    public string GetbackEndURL()
    {
        return kDefaultBackEndURL;
    }

#if UNITY_EDITOR
    [UnityEditor.SettingsProvider]
    public static UnityEditor.SettingsProvider CreateSettingsProvider()
    {
        return new UnityEditor.SettingsProvider("Project/ReproTrace", UnityEditor.SettingsScope.Project)
        {            
            label = "ReproTrace",
            guiHandler = _ =>
            {
                if(MBugReporterClientConfiguration.Resource != null) {
                    var serObj = new UnityEditor.SerializedObject(MBugReporterClientConfiguration.Resource);
                    DrawSettingsUI(serObj);
                }
                else {
                    GUILayout.Label("ReproTrace configuration asset missing! Create?");
                    if (GUILayout.Button("Create")) {
                        CreateConfigurationAsset();
                    }
                }
            }
        };
    }

    [MenuItem("Tools/MBugReporter/Create configuration asset")]
    public static void CreateConfigurationAsset()
    {
        if(Resources.Load<MBugReporterClientConfiguration>("MBugReporterClientConfiguration") != null) {
            Debug.LogError("MBugReporterClientConfiguration asset already exists at "+AssetDatabase.GetAssetPath(MBugReporterClientConfiguration.Resource));
            return;
        }

        var path = "Assets/Resources/MBugReporterClientConfiguration_ProjectSpecific.asset";
        new FileInfo(MaxinRandomUtils.UnityAssetPathToAbsolutePath(path)).Directory.Create();

        var obj = ScriptableObject.CreateInstance<MBugReporterClientConfiguration>();
        AssetDatabase.CreateAsset(obj, path);
        Selection.activeObject = obj;
        AssetDatabase.SaveAssets();
    }    

    public static void PromptConfigCreation()
    {
        if (MBugReporterClientConfiguration.Resource != null)
            return;

        UnityEditor.EditorUtility.DisplayDialog("ReproTrace installation", "ReproTrace needs a configuration asset to function. One was created to Resources/ReproTraceConfiguration. You'll need to set the API token there next.", "OK");        
    }

    public static void DrawSettingsUI(SerializedObject serObj)
    {
        var wid = 400;
        UnityEditor.EditorGUILayout.PropertyField(serObj.FindProperty("projectAPIToken"), GUILayout.MaxWidth(wid));
        serObj.ApplyModifiedPropertiesWithoutUndo();

        if (GUILayout.Button("Test settings", GUILayout.MaxWidth(wid))) {
            TestSettings();
        }
    }

    [UnityEditor.CustomEditor(typeof(MBugReporterClientConfiguration))]
    public class ReproTraceSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI() {
            DrawSettingsUI(serializedObject);
        }
    }

    private static void TestSettings()
    {
        MBugCustomBackEndUploader.TestSettings(out var settsOK, out var error);
        if (settsOK) {
            UnityEditor.EditorUtility.DisplayDialog("Configuration is OK", error, "OK");
        }
        else {
            UnityEditor.EditorUtility.DisplayDialog("Configuration has issues", error, "OK");
        }
    }
#endif
}
#endif