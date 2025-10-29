#if !DISABLE_MBUG

using MPerf;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;




#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class MBugLogStreamer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Early() {
        RunSuperEarlyInitialization();
        EarlyLog("Early SubsystemRegistration");
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    public static void Early2() { EarlyLog("Early AfterAssembliesLoaded"); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void Early3() { EarlyLog("Early BeforeSplashScreen"); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Early4() { EarlyLog("Early BeforeSceneLoad"); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Early5() { EarlyLog("Early AfterSceneLoad"); }

    private static void EarlyLog(string v)
    {
        //Debug.Log(v);
    }

    static MBugLogStreamer()
    {
        //Debug.Log("Early static constructor");
        //RunSuperEarlyInitialization();  //using SubsystemRegistration instead, that works with domain reload (am told)
    }


    static System.DateTime initTime;
    static int logsCount = 0;

    //this needs to run as early as possible to avoid missing logs, before we can even know if we want to send or even cache the data.
    //It will only be cached in RAM till other systems are brought up    
    private static void RunSuperEarlyInitialization()
    {
        logsCount = 0;
        logQueue.Clear();
        initTime = System.DateTime.UtcNow;
        Application.logMessageReceivedThreaded -= Application_logMessageReceivedThreaded;
        Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
    }


    static DateTime timeLastGotTooManyLayersUsedToExcludeWarning = System.DateTime.MinValue;

    private static ConcurrentQueue<UnityLogEntry> logQueue = new ConcurrentQueue<UnityLogEntry>();

    private static void Application_logMessageReceivedThreaded(string condition, string stackTrace, UnityEngine.LogType type)
    {
        if (condition.StartsWith("[MBugStreamedDataBatch.FlushStreamedData]"))
            return;

        //this spam can end up being 90% of logs when this isn't cased in a project!
        if(condition.StartsWith("Too many layers used to exclude objects from lighting")) {
            if((System.DateTime.UtcNow - timeLastGotTooManyLayersUsedToExcludeWarning).TotalSeconds > 10d) {
                timeLastGotTooManyLayersUsedToExcludeWarning = System.DateTime.UtcNow;
            }
            else return;
        }
                
        var entry = new UnityLogEntry();
        entry.idx = logsCount;
        entry.content = condition;
        entry.stacktrace = stackTrace;
        entry.logType = (LogType)type;

        entry.utcRealTimeTicks = System.DateTime.UtcNow.Ticks;
        entry.realTimeFromStartTicks = (System.DateTime.UtcNow - initTime).Ticks;
        entry.scaledTime = PlayerLoopStartMeter.scaledTimeCachedForUpdateAndAfter;
        entry.frame = PlayerLoopEndMeter.frameCachedForOtherThreads;
        entry.fixedUpdateTick = PlayerLoopStartMeter.fixedUpdateCached;
        entry.bgVideoTime = BGVideoCapture.RecordingTime;
        entry.bgVideoFrame = BGVideoCapture.TotalFrameNum;

        logQueue.Enqueue(entry);
        logsCount++;
    }


    public static List<UnityLogEntry> PopQueue()
    {
        List<UnityLogEntry> popped = new List<UnityLogEntry>();
        while(logQueue.TryDequeue(out var entry)) {
            popped.Add(entry);
        }

        return popped;
    }
}
#endif