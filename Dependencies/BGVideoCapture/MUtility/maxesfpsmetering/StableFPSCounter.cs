using UnityEngine;
using System.Collections;
using System;
using System.Text;
using System.Reflection;

namespace MPerf
{
    public class StableFPSCounter : MonoBehaviour
    {
        public bool dontDestroyOnLoad = false;

        public bool on;
        public bool showfpsGraph = false;

        public static bool periodicallyLogFps = false;

        [HideInInspector] public float currentAvgFPS = 0;
        private float currFps = 0;

        private float worstFps = 0;
        private float worstTimer = 0;
        private float shownWorst = 0;

        public static StableFPSCounter instance;

        void Awake() {
            if (instance != null) {
                Debug.Log("StableFPSCounter: have one already, destroying this copy !");
                DestroyImmediate(gameObject);
                return;
            }

            instance = this;
        }


        private void Start()
        {
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }

        public static float smoothAveragesSmoothness = 1f;

        void LateUpdate() {
            currFps = (1 / Time.unscaledDeltaTime);
            //currentAvgFPS = UpdateCumulativeMovingAverageFPS(currFps);
            currentAvgFPS = Mathf.Lerp(currentAvgFPS, currFps,/*Time.unscaledDeltaTime*/0.033f * smoothAveragesSmoothness);

            if (currFps < worstFps) {
                worstFps = currFps;
            }
            worstTimer += Time.unscaledDeltaTime;
            if (worstTimer > 0) {
                worstTimer -= 1f;
                shownWorst = worstFps;
                worstFps = 10000;
                fixedUpdatesLastSecond = fixedUpdatesCounter;
                fixedUpdatesCounter = 0;
            }



            smoothLastUpdateLoopLen = Mathf.Lerp(smoothLastUpdateLoopLen, PlayerLoopEndMeter.lastUpdateLoopLen, Time.unscaledDeltaTime * smoothAveragesSmoothness);
            smoothLastFixedUpdateLoopLen = Mathf.Lerp(smoothLastFixedUpdateLoopLen, PlayerLoopEndMeter.lastFixedUpdateLoopLen, Time.unscaledDeltaTime * smoothAveragesSmoothness);
            smoothfixedsPerLastRenderedFrame = Mathf.Lerp(smoothfixedsPerLastRenderedFrame, PlayerLoopStartMeter.fixedsPerLastRenderedFrame, Time.unscaledDeltaTime * smoothAveragesSmoothness);

            smoothlastApproxOutsideScriptsOfFrameTimeSpent = Mathf.Lerp(smoothlastApproxOutsideScriptsOfFrameTimeSpent, PlayerLoopStartMeter.lastApproxOutsideScriptsOfFrameTimeSpent, Time.unscaledDeltaTime * smoothAveragesSmoothness);

            smoothTimeSpentInFixedUpdatesThisFrame = Mathf.Lerp(smoothTimeSpentInFixedUpdatesThisFrame, PlayerLoopStartMeter.timeSpentInFixedUpdatesThisFrame, Time.unscaledDeltaTime * smoothAveragesSmoothness);

            if (renderTimeChecker != null) {
                smoothTimeTakenPreCullToPreRender = Mathf.Lerp(smoothTimeTakenPreCullToPreRender, renderTimeChecker.lastTimeTakenPreCullToPreRender, Time.unscaledDeltaTime * smoothAveragesSmoothness);
                smoothTimeTakenPreToPostRender = Mathf.Lerp(smoothTimeTakenPreToPostRender, renderTimeChecker.lastTimeTakenPreToPostRender, Time.unscaledDeltaTime * smoothAveragesSmoothness);
                smoothTimeTakenPostRenderToEndOfFrame = Mathf.Lerp(smoothTimeTakenPostRenderToEndOfFrame, renderTimeChecker.lastTimeTakenPostRenderToEndOfFrame, Time.unscaledDeltaTime * smoothAveragesSmoothness);
            }

            HandleHavingRenderTimeChecker();

            HandleFPSGraph();

            if (showfpsGraph)
                CountGarbage();

            BuildMainDebugString();

            PeriodicallyLogFpsCheck();
        }

        System.DateTime startTime;
        float timeLastReportedFps = 0f;

        private void PeriodicallyLogFpsCheck() {
            if(timeLastReportedFps == 0f) {
                startTime = System.DateTime.Now;
            }
            if (!periodicallyLogFps) return;
            if(Time.realtimeSinceStartup - timeLastReportedFps > 10f) {
                Debug.Log("StableFPSCounter current average FPS:"+currentAvgFPS.ToString("0")+"\tgame has ran for:"+ (System.DateTime.Now - startTime) );
                timeLastReportedFps = Time.realtimeSinceStartup;
            }
        }

        private void BuildMainDebugString() {
            if (ShouldEarlyOut()) return;

            if (fpsTextBuilder == null) {
                fpsTextBuilder = new StringBuilder(1024, 1024);
            }
            fpsTextBuilder.Length = 0;

            fpsTextBuilder.Append(Mathf.Round(currFps).ToString().PadRight(3));
            fpsTextBuilder.AppendLine("fps");

            fpsTextBuilder.Append(Mathf.Round(currentAvgFPS).ToString().PadRight(3));
            fpsTextBuilder.AppendLine("avg");

            fpsTextBuilder.Append(Mathf.Round(shownWorst).ToString().PadRight(3));
            fpsTextBuilder.AppendLine("worst");

            fpsTextBuilder.Append(Mathf.Round(fixedUpdatesLastSecond).ToString().PadRight(3));
            fpsTextBuilder.AppendLine("fix");

            //fpsTextBuilder.AppendLine(Mathf.Round(currFps).ToString().PadRight(2) + " fps\n" + Mathf.Round(currentAvgFPS).ToString().PadRight(2) + " avg\n" + Mathf.Round(shownWorst).ToString().PadRight(2) + " worst\nfix:" + fixedUpdatesLastSecond);
            //var str = Mathf.Round (currFps).ToString ().PadRight (2) + " fps\n" + Mathf.Round (currentAvgFPS).ToString ().PadRight (2) + " avg\n" + Mathf.Round (shownWorst).ToString ().PadRight (2) + " worst\nfix:" + fixedUpdatesLastSecond;

            //str += "\nfd:" + System.Math.Round (1f / FixedTicksCounter.lastFixedDeltaTime, 2);
            //str += "\nfd:" + System.Math.Round (FixedTicksCounter.realDeltaLastFixed*1000f, 2);

            //		str += "\nsd:" + System.Math.Round (PlayerLoopEndMeter.lastUpdateLoopLen*1000f, 2);
            //		str += "\nsfd:" + System.Math.Round (PlayerLoopEndMeter.lastFixedUpdateLoopLen*1000f, 2);
            //		str += "\nf/u:" + PlayerLoopStartMeter.fixedsPerLastRenderedFrame;

            fpsTextBuilder.AppendLine();

            fpsTextBuilder.Append("sd:");
            fpsTextBuilder.Append(System.Math.Round(smoothLastUpdateLoopLen * 1000f, 2));
            fpsTextBuilder.AppendLine();
            //fpsTextBuilder.AppendLine("sd:" + System.Math.Round(smoothLastUpdateLoopLen * 1000f, 2));
            //return;

            //str += "\nsd:" + System.Math.Round (smoothLastUpdateLoopLen*1000f, 2); //average time taken in Update() calls

            //str += "\n";


            fpsTextBuilder.Append("sfd:");
            fpsTextBuilder.Append(System.Math.Round(smoothLastFixedUpdateLoopLen * 1000f, 2));
            fpsTextBuilder.AppendLine();
            //fpsTextBuilder.AppendLine("sfd:" + System.Math.Round(smoothLastFixedUpdateLoopLen * 1000f, 2));
            //str += "\nsfd:" + System.Math.Round (smoothLastFixedUpdateLoopLen*1000f, 2); //average time taken by all FixedUpdate() calls per a physics update

            fpsTextBuilder.Append("f/u:");
            fpsTextBuilder.Append(System.Math.Round(smoothfixedsPerLastRenderedFrame, 2));
            fpsTextBuilder.AppendLine();
            //fpsTextBuilder.AppendLine("f/u:" + System.Math.Round(smoothfixedsPerLastRenderedFrame, 2));
            //str += "\nf/u:" +  System.Math.Round (smoothfixedsPerLastRenderedFrame,2); //average how many fixed updates there are per a frame, for example if game is running 30 fps but there was 60 fixed updates, this value is 2

            fpsTextBuilder.Append("sfdt");
            fpsTextBuilder.Append(System.Math.Round(smoothTimeSpentInFixedUpdatesThisFrame * 1000f, 2));
            fpsTextBuilder.AppendLine();
            //fpsTextBuilder.AppendLine("sfdt:" + System.Math.Round(smoothTimeSpentInFixedUpdatesThisFrame * 1000f, 2));
            //str += "\nsfdt:" + System.Math.Round (smoothTimeSpentInFixedUpdatesThisFrame*1000f, 2); //average time taken during a frame by all FixedUpdate calls (for example, during a frame there was 3 fixed update loops, this is the sum of them)

            fpsTextBuilder.AppendLine();
            //str += "\n";

            fpsTextBuilder.Append("out:");
            fpsTextBuilder.Append(System.Math.Round(smoothlastApproxOutsideScriptsOfFrameTimeSpent * 1000f, 2));
            fpsTextBuilder.AppendLine();
            //fpsTextBuilder.AppendLine("out:" + System.Math.Round(smoothlastApproxOutsideScriptsOfFrameTimeSpent * 1000f, 2));
            //str += "\n\nout:" +  System.Math.Round (smoothlastApproxOutsideScriptsOfFrameTimeSpent*1000f,2); //average time taken outside scripts or something, might not be reliable (check unity lifecycle on google image search)

            fpsTextBuilder.AppendLine();
            //str += "\n";

            if (renderTimeChecker) {
                fpsTextBuilder.AppendLine("rend:");
                //str += "\nrend:\n";

                fpsTextBuilder.AppendLine(" " + ToNiceMSString(smoothTimeTakenPreCullToPreRender));
                fpsTextBuilder.AppendLine(" " + ToNiceMSString(smoothTimeTakenPreToPostRender));
                fpsTextBuilder.AppendLine(" " + ToNiceMSString(smoothTimeTakenPostRenderToEndOfFrame));
                //str += " "+ToNiceMSString(smoothTimeTakenPreCullToPreRender)+"\n "+ToNiceMSString(smoothTimeTakenPreToPostRender) + "\n " + ToNiceMSString(smoothTimeTakenPostRenderToEndOfFrame); //average time taken to render the main camera, and average time taken from rendering the main camera to end of frame (post effects, UI, maybe other stuff)
            }

            mainDebugString = fpsTextBuilder.ToString();
        }

        long lastGarbage = -1;
        long lastCollectedAt = -1;
        long lastCollectedTo = -1;
        float garbageNormalized = -1f;

        int lastSecond = 0;

        long garbageAllocatedLastFrame = 0;

        long garbageAllocatedThisSecondCounter = 0;
        long garbageAllocatedLastSecond = 0;

        public int lastFrameCollectedGarbageAt = -1;

        public long garbageGeneratedTotal = 0;

        public bool DidCollectGarbageLastFrame {
            get {
                var framesSince = Time.frameCount - lastFrameCollectedGarbageAt;
                return framesSince == 1;
            }
        }

        void CountGarbage() {

            var secondNow = Mathf.FloorToInt(Time.realtimeSinceStartup);
            if (secondNow != lastSecond) {
                garbageAllocatedLastSecond = garbageAllocatedThisSecondCounter;
                garbageAllocatedThisSecondCounter = 0;
            }

            var garbageNow = System.GC.GetTotalMemory(false);

            garbageAllocatedLastFrame = 0;
            if (garbageNow < lastGarbage) {
                lastCollectedAt = lastGarbage;
                lastCollectedTo = garbageNow;
                lastFrameCollectedGarbageAt = Time.frameCount;
            }
            else garbageAllocatedLastFrame = garbageNow - lastGarbage;

            garbageAllocatedThisSecondCounter += garbageAllocatedLastFrame;
            garbageGeneratedTotal += garbageAllocatedLastFrame;

            garbageNormalized = Mathf.InverseLerp((float)lastCollectedTo, (float)lastCollectedAt, (float)garbageNow);

            lastSecond = secondNow;
            lastGarbage = garbageNow;
        }

        private void HandleFPSGraph() {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.F) && Input.GetKeyDown(KeyCode.M)) showfpsGraph = !showfpsGraph;

            if (FPSGraphDrawer.current != null) {
                if (FPSGraphDrawer.current.gameObject.activeSelf != showfpsGraph) {
                    FPSGraphDrawer.current.gameObject.SetActive(showfpsGraph);
                }
                if (showfpsGraph) {
                    FPSGraphDrawer.current.AddPoint(Time.unscaledDeltaTime);
                    FPSGraphDrawer.current.Refresh();
                }
            }
        }

        RenderTimeChecker renderTimeChecker = null;

        private void HandleHavingRenderTimeChecker() {
            if (renderTimeChecker == null) {
                if (Camera.main != null) {
                    //print("adding RenderTimeChecker to" + Camera.main);
                    renderTimeChecker = Camera.main.gameObject.AddComponent<RenderTimeChecker>();
                }
            }
        }

        int fixedUpdatesCounter = 0;
        int fixedUpdatesLastSecond = 0;

        void FixedUpdate() {
            fixedUpdatesCounter++;
        }

        //tracked
        float smoothLastUpdateLoopLen;
        float smoothLastFixedUpdateLoopLen;
        float smoothfixedsPerLastRenderedFrame;
        float smoothlastApproxOutsideScriptsOfFrameTimeSpent;

        float smoothTimeSpentInFixedUpdatesThisFrame;

        float smoothTimeTakenPreCullToPreRender;
        float smoothTimeTakenPreToPostRender;
        float smoothTimeTakenPostRenderToEndOfFrame;

        Rect fpsMainRect = new Rect();
        Rect bgRekt = new Rect();

        StringBuilder fpsTextBuilder = null;


        string mainDebugString = "NOTSET";

        void OnGUI() {

            //System.Threading.Thread.Sleep(100);

            if (ShouldEarlyOut()) return;

            fpsMainRect.x = Screen.width - 53;
            fpsMainRect.y = 0;
            fpsMainRect.width = 70;
            fpsMainRect.height = 260;

            //var rekt = new Rect (Screen.width - 53, 0, 70, 260);
            //print(Gerboga);

            //bgrekt.width = bgrekt.width + 10f;
            if (bgRekt.width < 5) {
                bgRekt = new Rect(fpsMainRect);
                bgRekt.x = fpsMainRect.x - 5f;
            }

            //GUI.DrawTexture (bgrekt, Texture2D.blackTexture);
            GUI.Box(bgRekt, "");


            GUI.Label(fpsMainRect, mainDebugString);

            if (showfpsGraph)
                DrawGCStuff(fpsMainRect);
        }

        private bool ShouldEarlyOut() {
            if (!on) return true;
            return false;
        }

        private void DrawGCStuff(Rect prevRect) {

            prevRect.y += prevRect.height + 3f;
            prevRect.height = 25f;
            //GUI.Label(prevRect, "grbg:" + MaxinRandomUtils.ByteLenghtToHumanReadable(lastGarbage));

            prevRect.x -= 20f;
            var bgRekt = new Rect(prevRect);
            var fillRekt = new Rect(bgRekt);

            fillRekt.width = fillRekt.width * garbageNormalized;

            var guistyle = new GUIStyle("Box");
            //guistyle.border = new RectOffset(0, 0, 0, 0);
            //guistyle.normal.background = Texture2D.blackTexture;

            //print(fillRekt);
            GUI.Box(bgRekt, ByteLenghtToHumanReadable(lastGarbage), guistyle);
            GUI.Box(fillRekt, "", guistyle);

            bgRekt.y += 20;
            GUI.Label(bgRekt, ByteLenghtToHumanReadable(garbageAllocatedLastSecond) + "/s");

            //GUILayout.Label("grbg:" + MaxinRandomUtils.ByteLenghtToHumanReadable(garbageNow));
        }

        public static string ToNiceMSString(float inSecs) {
            var numb = System.Math.Round(inSecs * 1000f, 2);

            return numb.ToString();
        }

        public static string ByteLenghtToHumanReadable(long byteLenght, bool useBits = false) {
            string suffix = "";
            long lenght;
            if (useBits) lenght = byteLenght * 8;
            else lenght = byteLenght;

            if (lenght < 1024) {
                if (useBits) suffix = " bits";
                else suffix = " B";
                return lenght + suffix;
            }
            else if (lenght < 1048576) {
                if (useBits) suffix = " kbits";
                else suffix = " kB";
                return (lenght / 1024) + suffix;
            }
            else {
                if (useBits) suffix = " Mbits";
                else suffix = " MB";
                return System.Math.Round(((float)lenght / (float)1048576), 2) + suffix;
            }
        }
    }
}
