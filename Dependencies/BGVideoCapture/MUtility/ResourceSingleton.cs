using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace MUtility
{
    public class ResourceSingleton<T> : ScriptableObject where T : ResourceSingleton<T> {

    public static T Resource {
        get {
            if (m_Resource == null) {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                var type = typeof(T);

                var loadWithName = GetOverrideAssetName();
                var loaded = Resources.Load<T>(loadWithName);

                if (loaded == null) {
                    loaded = Resources.Load<T>(type.Name);
                    if (loaded == null) {
                        if(typeof(T).Name == "ReproTraceClientConfiguration") {
                            Debug.LogError("MBugReporter: No ReproTraceClientConfiguration asset found. Please create one from \"Tools/ReproTrace/Create configuration asset\", and configure it.");
                            return null;
                        }
                        else throw new System.Exception("ResourceSingleton of type " + type.Name + " cannot be loaded! Be sure there exists an asset named exactly the type (\"" + type.Name + "\") directly under a folder named Resources, or one named "+type.Name+"_ProjectSpecific !");                        
                    }
                }


                if(loaded.InstantiateOnLoad && Application.isEditor && Application.isPlaying) {
                    m_Resource = Instantiate(loaded); //instantiating is for editor playmode UX. Otherwise editing the .Resource's fields might lead to persistent changes (!)
                }
                else {
                    m_Resource = loaded;
                }

                if(m_Resource == null) {
                    var err = "ResourceSingleton of type " + type.Name + " could not be loaded!";
                    UnityEngine.Debug.LogError(err);
                }
                else {
                    beenLoaded = true;
                }
                if(timer.ElapsedMilliseconds > 50) UnityEngine.Debug.LogWarning("loading ResourceSingleton of type " + type + " took:" + timer.ElapsedMilliseconds + " ms");
            }

            return m_Resource;
        }
    }

    public static string GetOverrideAssetName() {
        return typeof(T).Name + "_ProjectSpecific";
    }

    protected static T m_Resource = null;

    public static bool beenLoaded = false;    


    public virtual bool InstantiateOnLoad {
        get {
            return true;
        }
    }
}

}

