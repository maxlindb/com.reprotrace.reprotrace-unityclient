using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReservedRecordingTest : MonoBehaviour
{
    #if !DISABLE_MBUG

    private BGVideoCapture.ReservedRecording manualRec;

    [InspectorButton]
    public void StartManualRecording()
    {
        manualRec = BGVideoCapture.StartReservedRecording("manualRec");
    }

    [InspectorButton]
    public void StopManualRecording()
    {
        manualRec.EndAndSaveReservedRecording(@"C:\Temp\manualrec.mp4");
        manualRec = null;
    }

    public bool flashbackOn = false;
    private BGVideoCapture.ReservedRecording flashbackRec;

    private void Update()
    {
        if(flashbackOn && flashbackRec == null) {
            flashbackRec = BGVideoCapture.StartReservedRecording("flashback");
        }
        flashbackRec.frameStart = Mathf.Clamp(BGVideoCapture.TotalFrameNum - 100, 0, 999999999);
    }

    [InspectorButton]
    public void CaptureFlashback() {
        flashbackRec.EndAndSaveReservedRecording(@"C:\Temp\flashbackrec.mp4");
        flashbackRec = null;
    }
    #endif
}