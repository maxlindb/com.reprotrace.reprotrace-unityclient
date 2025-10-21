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

    public static void InitializeReproTrace()
    {
        if (MBugReporterClientConfiguration.Resource == null) {
            Debug.LogError("ReproTrace configuration is missing. Halting system.");
            MBugCustomBackEndUploader.systemHaltedDueToMisconfiguration = true;
            return;
        }

        var existing = MBugReporter.HasCachedInstance ? MBugReporter.Instance : null;
        if(existing != null) {
            return; //it already existing is a common case, don't need to log
        }
                
        var prefab = Resources.Load<GameObject>("ReproTraceMainCanvas");        
        Instantiate(prefab, internalInstance.transform);
    }
}
