using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VersionNumberOverlay : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.Label("Commit:"+MChangesetIdentification.GetShortChangesetHash());
    }
}
