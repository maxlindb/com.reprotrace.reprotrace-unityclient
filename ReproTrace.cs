using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//You can either put this stub to your scene, or call ReproTrace.InitializeReproTrace() from anywhere.
//The system stays alive between scene swithces automatically.
public class ReproTrace : MonoBehaviour
{
    static ReproTrace internalInstance;

    private void Start()
    {
        internalInstance = this;
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
        var copy = Instantiate(prefab, internalInstance.transform);
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
}
