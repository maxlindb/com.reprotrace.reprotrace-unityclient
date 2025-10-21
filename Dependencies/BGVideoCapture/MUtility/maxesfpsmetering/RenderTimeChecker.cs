using System;
using System.Collections;
using UnityEngine;


namespace MPerf {
    public class RenderTimeChecker : MonoBehaviour
    {
        public float lastTimePreRender = -1f;
        public float lastTimePostRender = -1f;
        public float lastTimePreCull = -1f;

        public float lastTimeTakenPreCullToPreRender;
        public float lastTimeTakenPreToPostRender;
        public float lastTimeTakenPostRenderToEndOfFrame;

        void OnPreCull() {
            lastTimePreCull = Time.realtimeSinceStartup;

            LogOrder("OnPreCull");
        }

        void OnPreRender() {
            lastTimeTakenPreCullToPreRender = Time.realtimeSinceStartup - lastTimePreCull;

            lastTimePreRender = Time.realtimeSinceStartup;

            LogOrder("OnPreRender");
        }

        void OnPostRender() {
            lastTimePostRender = Time.realtimeSinceStartup;

            lastTimeTakenPreToPostRender = lastTimePostRender - lastTimePreRender;

            LogOrder("OnPostRender");
        }

        void Start() {
            StartCoroutine(EndOfFrameRunning());
        }

        IEnumerator EndOfFrameRunning() {
            while(true) {
                yield return new WaitForEndOfFrame();
                lastTimeTakenPostRenderToEndOfFrame = Time.realtimeSinceStartup - lastTimePostRender;

                LogOrder("EndOfFrameRunning");
            }
        }

        public static bool debugOrder = false;
        static int lastFrameOrderCheck = -1;
        static float lastTimeLog = -1f;

        private void LogOrder(string v) {
            if(debugOrder) {
                bool isNewFrame = Time.frameCount != lastFrameOrderCheck;

                var debugStr = isNewFrame ? "NEWFRAME" : StableFPSCounter.ToNiceMSString(Time.realtimeSinceStartup - lastTimeLog);

                Debug.Log("RenderTimeChecker.LogOrder [" + v + "]: "+ debugStr);

                lastFrameOrderCheck = Time.frameCount;
                lastTimeLog = Time.realtimeSinceStartup;
            }
        }
    }
}
