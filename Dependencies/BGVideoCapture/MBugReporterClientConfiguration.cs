#if !DISABLE_MBUG
using MUtility;
using System;
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
        return new UnityEditor.SettingsProvider("Project/MBugReporter", UnityEditor.SettingsScope.Project)
        {            
            label = "MBugReporter",
            guiHandler = _ =>
            {
                var serObj = new UnityEditor.SerializedObject(MBugReporterClientConfiguration.Resource);
                UnityEditor.EditorGUILayout.PropertyField(serObj.FindProperty("projectAPIToken"), GUILayout.Width(500));
                serObj.ApplyModifiedPropertiesWithoutUndo();

                if(GUILayout.Button("Test settings", GUILayout.Width(500))) {
                    TestSettings();
                }
            }
        };
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