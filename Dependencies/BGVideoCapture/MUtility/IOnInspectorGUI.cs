using UnityEngine;
using System.Collections;

public interface IOnInspectorGUI 
{
	#if UNITY_EDITOR
	void OnInspectorGUI();
	#endif
}

public interface IOnOnspectorSuppressDrawDefaultInspector { }

public static class IOnInspectorGUIHelper
{
    //exists so that monobehaviours can call draw default inspector.
#if UNITY_EDITOR
    public static UnityEditor.Editor currentlyDrawingEditor = null;
#endif

    public static System.Action ToDrawToSceneGUINow {
        set {
            m_ToDrawToSceneGUINow = value;
            toDrawToSceneGuiSetFrame = Time.frameCount;
        }
        get { return m_ToDrawToSceneGUINow; }
    }
    private static System.Action m_ToDrawToSceneGUINow = null;

    static int toDrawToSceneGuiSetFrame = -1;

    public static bool OnSceneGUIWasRegisteredThisFrame => toDrawToSceneGuiSetFrame < Time.frameCount + 2;
}


public interface IOnSceneGUI
{
    #if UNITY_EDITOR
    void OnSceneGUI();
    #endif
}