#if !DISABLE_MBUG
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[MessagePackObject]
[System.Serializable]
public class MBugStreamedDataBatch
{
    [Key(0)]
    public int batchNumber;

    [Key(1)]
    public List<UnityLogEntry> logEntries = new List<UnityLogEntry>();
    [Key(2)]
    public List<UnityFrameData> framesData = new List<UnityFrameData>();
}

[MessagePackObject]
[System.Serializable]
public class UnityLogEntry
{
    [Key(0)]public int idx = 0;
    [Key(1)]public string content;
    [Key(2)]public string stacktrace;
    [Key(3)]public LogType logType;
    
    [Key(4)]public long utcRealTimeTicks;
    [Key(5)]public long realTimeFromStartTicks;
    [Key(6)]public double scaledTime;
    [Key(7)]public int frame;
    [Key(8)]public int fixedUpdateTick;
    [Key(9)]public int bgVideoFrame;
    [Key(10)]public double bgVideoTime;
}

public enum LogType
{
    Error,
    Assert,
    Warning,
    Log,
    Exception
}

[MessagePackObject]
[System.Serializable]
public class UnityFrameData
{
    [Key(1)]public int frame;
    [Key(2)]public float fullFrameDuration;
    [Key(3)]public float behaviourUpdateDuration;
    [Key(4)]public float fixedUpdatesDuration;
    [Key(5)]public int fixedUpdatesCount;
    
    [Key(6)]public int bgVideoFrame;
    [Key(7)]public double bgVideoTime;
}
#endif