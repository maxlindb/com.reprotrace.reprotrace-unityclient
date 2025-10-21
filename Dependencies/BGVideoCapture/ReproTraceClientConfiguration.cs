#if !DISABLE_MBUG
using MUtility;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu]
public class ReproTraceClientConfiguration : ResourceSingleton<ReproTraceClientConfiguration>
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
                if(ReproTraceClientConfiguration.Resource != null) {
                    var serObj = new UnityEditor.SerializedObject(ReproTraceClientConfiguration.Resource);
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

    [MenuItem("Tools/ReproTrace/Create configuration asset")]
    public static void CreateConfigurationAsset()
    {
        if(Resources.Load<ReproTraceClientConfiguration>("ReproTraceClientConfiguration") != null) {
            Debug.LogError("ReproTraceClientConfiguration asset already exists at " + AssetDatabase.GetAssetPath(ReproTraceClientConfiguration.Resource));
            return;
        }

        var path = "Assets/Resources/ReproTraceClientConfiguration_ProjectSpecific.asset";
        new FileInfo(MaxinRandomUtils.UnityAssetPathToAbsolutePath(path)).Directory.Create();

        var obj = ScriptableObject.CreateInstance<ReproTraceClientConfiguration>();
        AssetDatabase.CreateAsset(obj, path);
        Selection.activeObject = obj;
        AssetDatabase.SaveAssets();
    }    

    public static void PromptConfigCreation()
    {
        Debug.Log("PromptConfigCreation");
        if (ReproTraceClientConfiguration.Resource != null)
            return;

        CreateConfigurationAsset();
        UnityEditor.EditorUtility.DisplayDialog("ReproTrace installation", "ReproTrace needs a configuration asset to function. One was created to Resources/ReproTraceConfiguration. You'll need to set the API token there next.", "OK");        
    }

    public static void DrawSettingsUI(SerializedObject serObj)
    {
        var wid = 400;

        if (string.IsNullOrEmpty(ReproTraceClientConfiguration.Resource.projectAPIToken)) {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MaxWidth(wid));
            EditorGUILayout.HelpBox("Create a project at ReproTrace.com and assign its token here!", MessageType.Info);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();            
            //GUILayout.Label("Create a project at ReproTrace.com and assign its token here!", GUILayout.MaxWidth(wid));
        }

        if (GUILayout.Button("Open ReproTrace.com", GUILayout.MaxWidth(wid))) Application.OpenURL((ReproTraceClientConfiguration.Resource.GetbackEndURL()));
        GUILayout.Space(20);

        UnityEditor.EditorGUILayout.PropertyField(serObj.FindProperty("projectAPIToken"), GUILayout.MaxWidth(wid));
        serObj.ApplyModifiedPropertiesWithoutUndo();

        if (GUILayout.Button("Test settings", GUILayout.MaxWidth(wid))) {
            TestSettings();
        }
    }

    [UnityEditor.CustomEditor(typeof(ReproTraceClientConfiguration))]
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