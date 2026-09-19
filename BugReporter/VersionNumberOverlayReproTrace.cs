using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VersionNumberOverlayReproTrace : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.Label("Commit:"+MChangesetIdentificationReproTrace.GetShortChangesetHash());
    }
}
