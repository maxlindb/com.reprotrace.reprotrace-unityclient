using MUtility;
using System;
using System.Linq;
using UnityEngine;

namespace MUtility
{
    public abstract class Singleton<T> : MonoBehaviour, ISingleton<T> where T : MonoBehaviour
    {
        static bool DoDebug => SingletonHelper.DoGeneralDebug;

        public static T Instance {
            get {
                if(m_instance == null) {
                    var isSelfCreating = typeof(ISelfCreatingSingleton).IsAssignableFrom(typeof(T));
                    var isPersistentSelfCreating = typeof(IPersistentSelfCreatingSingleton).IsAssignableFrom(typeof(T));
                    var isPrefabSelfCreating = typeof(ISelfCreatingPrefabResourceSingleton).IsAssignableFrom(typeof(T));
                    var isPrefabSelfCreatingNonInstantiating = typeof(ISelfCreatingPrefabNonInstantiatingResourceSingleton).IsAssignableFrom(typeof(T));

                    if (isPersistentSelfCreating || isPrefabSelfCreating || isPrefabSelfCreatingNonInstantiating)
                        isSelfCreating = true;

                    if (!isSelfCreating && Application.isPlaying) Debug.LogWarning("Singleton doing FindObjectOfType " + typeof(T).Name);
                    m_instance = TryFind();

                    if(m_instance == null && typeof(IFindDisabledSingleton).IsAssignableFrom(typeof(T))) {
                        Debug.LogWarning("Singleton doing FindComponentsOfType " + typeof(T).Name);
                        m_instance = MaxinRandomUtils.FindComponentsOfType<T>().FirstOrDefault();
                    }

                    if (m_instance == null) {

                        if (isSelfCreating) {

                            if (MaxinRandomUtils.IsQuitting)
                                throw new System.Exception("Aborting creation of a new singleton instance of type "+typeof(T).Name+" because the game is quitting! Check your code and branch by MaxinRandomUtils.IsQuitting earlier to not hit nullrefs this likely causes");

                            if(SingletonHelper.creatingInstanceOfType == typeof(T)) {
                                Debug.LogError("Potential cyclical singleton .Instance lazy creation detected with "+typeof(T).Name+" (dependency loop)");
                            }
                            SingletonHelper.creatingInstanceOfType = typeof(T);

                            GameObject go = null;
                            if(isPrefabSelfCreating || isPrefabSelfCreatingNonInstantiating) {
                                var prefab = Resources.Load<T>(typeof(T).Name);
                                if(isPrefabSelfCreatingNonInstantiating) {
                                    m_instance = prefab;
                                }
                                else {
                                    m_instance = Instantiate(prefab);
                                    m_instance.name = typeof(T).Name + "_autocreated_fromPrefab" + (isPersistentSelfCreating ? "_persistent" : "");
                                }
                                go = m_instance.gameObject;
                            }
                            else {
                                go = new GameObject(typeof(T).Name + "_autocreated" + (isPersistentSelfCreating ? "_persistent" : ""));
                                m_instance = go.AddComponent<T>();
                            }

                            m_instance.enabled = true; //don't ask (I don't actually exactly even remember this one)

                            if(isPersistentSelfCreating) {
                                if (Application.isPlaying) {
                                    DontDestroyOnLoad(go);
                                }
                                if (DoDebug || !Application.isEditor) Debug.Log("autocreated persistent singleton " + go.transform.GetHieararchyPath(true));
                            }

                            SingletonHelper.creatingInstanceOfType = null;
                        }
                        else Debug.LogError("Singleton fetch fail " + typeof(T));

                    }
                }
                return m_instance;
            }
        }

        private static T TryFind()
        {
            //var found = MaxinRandomUtils.FindComponentsOfType<T>().FirstOrDefault(); //need to be able to find from disabled (GoldPanningUI case) LATER NOTE: disabled versions causes just more headache
            //Debug.Log("Foudn:" + found);
            //return found;
#if UNITY_6000_0_OR_NEWER
			return FindAnyObjectByType<T>();
#else            
            return FindObjectOfType<T>();
#endif
        }

        protected static T m_instance = null;

        protected void Awake() {
            if(m_instance != null && Instance != this) { //max note 17.10.2020, added second check. Should have realized earlier that if something calls Instance, causing it to be searched, and only after that the Awake of this happens, this is actually a valid scenario.
                Debug.LogWarning("Multiple instances of singleton of type " + typeof(T));
                if (!Application.isPlaying) return; //otherwise inheriting classes with ExecuteAlways make shit go boom
                Debug.LogWarning(GetType().Name + ": Destroying singleton instance " + transform.GetHieararchyPath(true) + " in favor of " + m_instance.transform.GetHieararchyPath(true));
                if (this == m_instance) Debug.LogError("What the hell, the instance is the SAME! Was Awake called manually or something????!?"); //max note 11.05.2021: not sure but this might be a cased of subclass overwriting Awake, OnEnable or similar

                if(!(this is ISelfCreatingPrefabResourceSingleton)) {
                    var comps = gameObject.GetComponents<Component>();
                    var otherComps = comps.Where(x => !(x is Transform) && !(x is T)).ToArray();
                    if (otherComps.Length > 0) {
                        Debug.LogError("BAD: Singleton destroying is destroying other components !!!! " + MaxinRandomUtils.PrintableList(otherComps.Select(x => x.GetType().Name), ","));
                    }
                }


                DestroyImmediate(gameObject);
                return;
            }
            m_instance = this as T;
        }

        public static void EnsureInstanceExists()
        {
            var test = Instance;
            if(test == null) {
                Debug.LogError("Singleton EnsureInstanceExists " + typeof(T) + ": does not exist after polling instance. The type needs to be ISelfCreatingSingleton for this to work");
            }
        }

        public static bool HasCachedInstance => m_instance != null; //otherwise some systems might FindObjectOfType all day
    }

    //non-generic
    public class SingletonHelper
    {
        public static Func<bool> overriddenGetDebugIsOnGetter;

        public static System.Type creatingInstanceOfType = null;


        public static bool DoGeneralDebug {
            get {
                if(overriddenGetDebugIsOnGetter != null) {
                    return overriddenGetDebugIsOnGetter();
                }
                return staticDoGeneralDebug;
            }
        }
        private static bool staticDoGeneralDebug = false;
    }

    public interface ISelfCreatingSingleton { }
    public interface IPersistentSelfCreatingSingleton { }
    public interface IFindDisabledSingleton { }

    public interface ISelfCreatingPrefabResourceSingleton { }
    public interface ISelfCreatingPrefabNonInstantiatingResourceSingleton { }


    public interface ISingleton<T> { } //need this annoying thing as all classes can't just inherit from the singleton class but need type safety
}
