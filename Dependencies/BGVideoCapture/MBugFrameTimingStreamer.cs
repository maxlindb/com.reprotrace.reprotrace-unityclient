using MPerf;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class MBugFrameTimingStreamer : MonoBehaviour
{
    #if !DISABLE_MBUG

    private void Start()
    {
        StartCoroutine(TillEndOfFrameUpdating());
    }

    //frame timing data compilation:
    //first we get stuff from AtEndOfFrame, like recorded times of how much time was spent in Update, FixedUpdates that frame
    //then we wait till next frame to append stuff like Time.unscaledDeltaTime as those will then represent the previous frame, then the data is ready
    private IEnumerator TillEndOfFrameUpdating()
    {
        var waitForEndOfFrame = new WaitForEndOfFrame();
        while (true) {
            yield return waitForEndOfFrame;
            try {
                AtEndOfFrame();
            }
            catch (System.Exception e) {
                Debug.LogException(e);
            }
        }
    }

    UnityFrameData previousFrame = null;

    private void AtEndOfFrame()
    {
        previousFrame = new UnityFrameData();
        previousFrame.frame = Time.frameCount;        
    }

    bool firstFrame = true;

    private void Update()
    {   
        if(firstFrame) { //this only happens once, and previousFrame is null. Nothing to do, stats from this frame will be checked next frame as with any frame.
            firstFrame = false;
            return;
        }

        previousFrame.fullFrameDuration = Time.unscaledDeltaTime;
        previousFrame.behaviourUpdateDuration = PlayerLoopEndMeter.lastUpdateLoopLen;
        previousFrame.fixedUpdatesDuration = PlayerLoopEndMeter.timeSpentInFixedUpdatesLastFrame; //TODO check if this even the right frame
        previousFrame.fixedUpdatesCount = PlayerLoopStartMeter.fixedsPerLastRenderedFrame;

        previousFrame.bgVideoTime = BGVideoCapture.RecordingTime;
        previousFrame.bgVideoFrame = BGVideoCapture.TotalFrameNum;

        frameDataQueue.Enqueue(previousFrame);
    }

    private static ConcurrentQueue<UnityFrameData> frameDataQueue = new ConcurrentQueue<UnityFrameData>();

    public static List<UnityFrameData> PopQueue()
    {
        List<UnityFrameData> popped = new List<UnityFrameData>();
        while(frameDataQueue.TryDequeue(out var entry)) {
            popped.Add(entry);
        }

        return popped;
    }
    #endif
}
