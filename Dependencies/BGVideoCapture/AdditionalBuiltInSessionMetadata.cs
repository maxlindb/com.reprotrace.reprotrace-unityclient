#if !DISABLE_MBUG
[System.Serializable]
public class AdditionalBuiltInSessionMetadata
{
    public int mbugClientVersion;
    public string startupScene;
    public string normalStartupSceneForGame;

    public double timeDeltaFromInitToRecordingStart;
    public long exactVideoRecordingStartTimeUTCTicks;
}
#endif
