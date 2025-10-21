using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MPerf {
    public class PlayerLoopEndMeter : MonoBehaviour {

	    public static float lastUpdateLoopLen;
	    public static float lastFixedUpdateLoopLen;

	    public static float lastFromFirstFixedToLastUpdateTime;

	    public static float timestampLastUpdateEnd;


        public static List<string> frameMessages = new List<string>();


        public static int frameCachedForOtherThreads;

		public static float timeSpentInFixedUpdatesLastFrame;


        public static void AddFrameMsg(string inMsg) {
            //frameMessages.Add(((Time.realtimeSinceStartup - PlayerLoopStartMeter.timeLastUpdateLoopStarted) * 1000).ToString("0.00") + " " + inMsg);
        }

        private void Start()
        {
			StartCoroutine(TrackingFrameBoundary());
        }


        // Update is called once per frame
        void Update () {
		    lastUpdateLoopLen = Time.realtimeSinceStartup - PlayerLoopStartMeter.timeLastUpdateLoopStarted;

		    lastFromFirstFixedToLastUpdateTime = Time.realtimeSinceStartup - PlayerLoopStartMeter.timeLastFixedUpdateLoopStarted;

		    timestampLastUpdateEnd = Time.realtimeSinceStartup;
	    }


	    //LAST TIME
	    public static Dictionary<System.Type,float> timeUsedPerTypeThisFrame = new Dictionary<System.Type, float> ();
	    public static Dictionary<System.Type,float> countPerTypeThisFrame = new Dictionary<System.Type, float> ();

	    //read only from these to get complete data
	    public static Dictionary<System.Type,float> timeUsedPerTypeLastFrame = new Dictionary<System.Type, float> ();
	    public static Dictionary<System.Type,float> countPerTypeLastFrame  = new Dictionary<System.Type, float> ();



	    public static int collectEvalStatsOverFramesCount = 100;

	    public static void RecTimeTakenByAnOperation (System.Type inType, float start)
	    {
		    if (!PlayerLoopEndMeter.timeUsedPerTypeThisFrame.ContainsKey (inType)) {
			    PlayerLoopEndMeter.timeUsedPerTypeThisFrame.Add (inType, 0f);
			    PlayerLoopEndMeter.countPerTypeThisFrame.Add (inType, 0);
		    }
		    PlayerLoopEndMeter.timeUsedPerTypeThisFrame [inType] += Time.realtimeSinceStartup - start;
		    PlayerLoopEndMeter.countPerTypeThisFrame [inType]++;
	    }

	    void LateUpdate() {
			timeSpentInFixedUpdatesLastFrame = PlayerLoopStartMeter.timeSpentInFixedUpdatesThisFrame;
            PlayerLoopStartMeter.timeSpentInFixedUpdatesThisFrame = 0f;
		    lastFixedUpdateLoopLen = 0f;


		    if (Time.frameCount % collectEvalStatsOverFramesCount == 0) {

			    //no more
			    timeUsedPerTypeLastFrame.Clear ();
			    foreach (var item in timeUsedPerTypeThisFrame) {
				    timeUsedPerTypeLastFrame [item.Key] = item.Value;
			    }

			    countPerTypeLastFrame.Clear ();
			    foreach (var item in countPerTypeThisFrame) {
				    countPerTypeLastFrame [item.Key] = item.Value;
			    }

			    timeUsedPerTypeThisFrame.Clear ();
			    countPerTypeThisFrame.Clear ();
		    }

            if(frameMessages.Count > 0) {
                var sb = new StringBuilder("FrameMessages:\n");
                foreach (var item in frameMessages) {
                    sb.AppendLine(item);
                }
                Debug.Log(sb.AppendLine());
                frameMessages.Clear();
            }

            /*if(PlayerLoopStartMeter.fromFramefirstUpdateCallTimer != null) {
                if(PlayerLoopStartMeter.fromFramefirstUpdateCallTimer.ElapsedMilliseconds > 50) {
                    Debug.Log(PlayerLoopStartMeter.allTimer.ElapsedMilliseconds + "####FRAME WAS LONG:" + PlayerLoopStartMeter.fromFramefirstUpdateCallTimer.ElapsedMilliseconds + "ms");
                }
            }*/
            PlayerLoopStartMeter.fromFramefirstUpdateCallTimer = System.Diagnostics.Stopwatch.StartNew(); //I know a bit weird yes


			var timeSinceUpdateStart = Time.realtimeSinceStartup - PlayerLoopStartMeter.timeLastUpdateLoopStarted;
			if(timeSinceUpdateStart > 1f) {
				Debug.LogWarning("From the first call of Update this frame, to the last call of LateUpdate, took "+ timeSinceUpdateStart.ToString("0.00")+" seconds.");
            }
		}

	    void FixedUpdate() {
		    lastFixedUpdateLoopLen = Time.realtimeSinceStartup - PlayerLoopStartMeter.timeLastFixedUpdateLoopStarted;
		    PlayerLoopStartMeter.timeSpentInFixedUpdatesThisFrame += lastFixedUpdateLoopLen;
        }

        private IEnumerator TrackingFrameBoundary()
        {
			var waitForEnd = new WaitForEndOfFrame();

			while (true)
			{
				yield return waitForEnd;
				OnEndOfFrame();
			}
        }

        private void OnEndOfFrame()
        {
			frameCachedForOtherThreads = Time.frameCount + 1; //the only way I figured would be valid for all client code is to update it at end of the previous frame, sue me
        }

        private void OnDestroy() {
            if(!Application.isEditor) Debug.Log("PlayerLoopEndMeter OnDestroy (at quite end of exec order)");
        }
    }
}
