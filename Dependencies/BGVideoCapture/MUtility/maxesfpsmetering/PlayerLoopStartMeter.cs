using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace MPerf {
    public class PlayerLoopStartMeter : MonoBehaviour {

        static PlayerLoopStartMeter instance = null;

	    public static float timeLastUpdateLoopStarted;
	    public static float timeLastFixedUpdateLoopStarted;

        public static Stopwatch allTimer = null;
        public static Stopwatch fixedStopWatch = null;
        public static Stopwatch fromFramefirstUpdateCallTimer = null;

	    public static int fixedPerUpdateCounter; //per-frame counter!

        public static int fixedUpdateCached = -1;
	    public static int fixedsPerLastRenderedFrame;
	    public static float lastApproxOutsideScriptsOfFrameTimeSpent;

        public static double scaledTimeCachedForUpdateAndAfter = 0d; //note that this is NOT reliable for things in FixedUpdate or animation update because there is no reliable place to update this from in those cases


        private void Awake() {
            if(allTimer == null) {
                allTimer = System.Diagnostics.Stopwatch.StartNew();
            }
            instance = this;
        }

        static System.Action generalEarlyUpdate;

        public static void AddToGeneralEarlyUpdate(System.Action inAct) {
            if (instance == null) UnityEngine.Debug.LogError("no PlayerLoopStartMeter!");
            generalEarlyUpdate += inAct;
        }

        // Update is called once per frame
        void Update () {
            scaledTimeCachedForUpdateAndAfter = Time.timeAsDouble;
            //UnityEngine.Debug.Log(allTimer.ElapsedMilliseconds + " firstupdate");
		    timeLastUpdateLoopStarted = Time.realtimeSinceStartup;
            //fromFramefirstUpdateCallTimer = System.Diagnostics.Stopwatch.StartNew();

            fixedsPerLastRenderedFrame = fixedPerUpdateCounter;
		    fixedPerUpdateCounter = 0;

		    if(Time.timeScale == 0f)lastFirstFixedForFrameOrPausedFirstUpdateStart = Time.realtimeSinceStartup;
		    if(Time.timeScale == 0f)lastApproxOutsideScriptsOfFrameTimeSpent = Time.realtimeSinceStartup - PlayerLoopEndMeter.timestampLastUpdateEnd;

            frameCount = Time.frameCount;


            if (generalEarlyUpdate != null) generalEarlyUpdate();
        }

	    int lastFrame = -1;
	    public static float lastFirstFixedForFrameOrPausedFirstUpdateStart;

	    public static float timeSpentInFixedUpdatesThisFrame;

	    public static float timeLastSingleFixedUpdateStarted;

        public static int frameCount = -1;


        public static bool AppIsExiting { get; private set; }

        private void OnApplicationQuit() {
            AppIsExiting = true;   
        }

        void FixedUpdate()
        {
            fixedUpdateCached++;

            timeLastSingleFixedUpdateStarted = Time.realtimeSinceStartup;

		    lastApproxOutsideScriptsOfFrameTimeSpent = Time.realtimeSinceStartup - PlayerLoopEndMeter.timestampLastUpdateEnd;

		    if(lastFrame != Time.frameCount) {
			    //Aslog.Log("first fixed for frame");
			    lastFirstFixedForFrameOrPausedFirstUpdateStart = Time.realtimeSinceStartup;
		    }
		    //Aslog.Log("fixed");

    //		if (fixedStopWatch == null) fixedStopWatch = new Stopwatch ();
    //		fixedStopWatch.Reset ();
    //		fixedStopWatch.Start ();

		    timeLastFixedUpdateLoopStarted = Time.realtimeSinceStartup;

		    fixedPerUpdateCounter++;

		    lastFrame = Time.frameCount;
	    }
    }
}
