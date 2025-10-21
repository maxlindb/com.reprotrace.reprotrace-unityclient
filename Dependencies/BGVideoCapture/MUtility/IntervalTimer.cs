#if UNITY_STANDALONE || UNITY_MOBILE || UNITY_EDITOR
#define UNITY //srlsy why this is not in automatically?!
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

#if UNITY
using UnityEngine;
#endif

namespace MUtility
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad] //I think this is automatic in build
#endif
    public class IntervalTimer
    {
        public static bool DoInstaLogAll {
            get {
                return m_doInstaLogAll;
            }
            set {
                m_doInstaLogAll = value;
            }
        }

        private static bool m_doInstaLogAll = false;

#if UNITY
        static IntervalTimer() {
            //GTPlatLog.Log("IntervalTimer: setting up registering to main thread.");
            UnityEngine.Application.onBeforeRender += OnceBeforeRender;
        }

        private static void OnceBeforeRender() {
            UnityEngine.Application.onBeforeRender -= OnceBeforeRender;
#if !UNITY_EDITOR
        if(generalAllowLogToggle) Debug.Log("IntervalTimer: Registering main thread.");
#endif
            mainThread = System.Threading.Thread.CurrentThread;
        }
#endif

        static Thread mainThread = null;

        private string timerName;
        private System.Diagnostics.Stopwatch stopWatch;

        public System.Diagnostics.Stopwatch Stopwatch {
            get {
                return stopWatch;
            }
        }

        //[RegisteredDebugFlag]
        static bool generalAllowLogToggle = true;


        private List<TimerInterval> intervals = new List<TimerInterval>(16);

        private static List<IntervalTimer> runningTimers = new List<IntervalTimer>(16);

        private IntervalTimer() { } //hide default constructor

        public static IntervalTimer Start(string inTimerName) {
            var timer = new IntervalTimer();

            timer.timerName = inTimerName;
            timer.stopWatch = System.Diagnostics.Stopwatch.StartNew();
            timer.Interval("START");
            runningTimers.Add(timer);
            return timer;
        }

        static bool doProfilerPointsForIntervals = false;

        public void Interval(string v, bool? instaLog = null) { //TODO instalog off/configurable
            TimerInterval intr = new TimerInterval();
            intr.intervalName = v;
            intr.stopwatchTicks = stopWatch.ElapsedTicks;

#if UNITY
            if (MPerf.PlayerLoopStartMeter.fromFramefirstUpdateCallTimer != null)
                intr.frameAgeTicks = (int)MPerf.PlayerLoopStartMeter.fromFramefirstUpdateCallTimer.ElapsedTicks;
            else
                intr.frameAgeTicks = -1;

            bool onMainThread = Thread.CurrentThread == mainThread;

            if (doProfilerPointsForIntervals) {
                if (intervals.Count != 0) {
                    UnityEngine.Profiling.Profiler.EndSample();
                }
                UnityEngine.Profiling.Profiler.BeginSample("NEXT TO:" + v);
            }

            if (onMainThread) {
                intr.frameCount = Time.frameCount;
            }
            else intr.frameCount = -1;
#endif

            intervals.Add(intr);

            bool finalInstaLogValue;

            if (instaLog.HasValue && instaLog.Value) {
                finalInstaLogValue = instaLog.Value;
            }
            else {
                finalInstaLogValue = DoInstaLogAll;
            }


            if (finalInstaLogValue) {
                //stacktrace level changing: can only be called from main thread
#if UNITY
                StackTraceLogType ogStackTraceLev = (StackTraceLogType)(-1);
                if (onMainThread) {
                    ogStackTraceLev = Application.GetStackTraceLogType(LogType.Log);
                    Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                }
#endif

                long delta = intervals.Count != 1 ? intr.stopwatchTicks - intervals[intervals.Count - 2].stopwatchTicks : -1;

#if UNITY
                Debug.LogFormat("{0}:{1}", timerName, intr.GetToString(-1,delta));
#else
                Console.WriteLine(string.Format("{0}:{1}", timerName, intr.GetToString(-1, delta)));
#endif

#if UNITY
                if (onMainThread) {
                    Application.SetStackTraceLogType(LogType.Log, ogStackTraceLev);
                }
#endif
            }
        }

        public string Stop(bool doLog = true) {
            Interval("END");

            var sb = new StringBuilder();
            sb.AppendFormat("Interval timer {0}: {1}\n", timerName, TicsToReadable(intervals[intervals.Count - 1].stopwatchTicks));

            for (int i = 0; i < intervals.Count; i++) {
                var intr = intervals[i];

                long delta = i == 0 ? -1 : intervals[i].stopwatchTicks - intervals[i - 1].stopwatchTicks;

                sb.AppendLine(intr.GetToString(i, delta));
            }
            if (doLog && generalAllowLogToggle) {
#if UNITY
                Debug.Log(sb.ToString());
#else
                Console.WriteLine(sb.ToString());
#endif
            }

            runningTimers.Remove(this); //don't leak millions of structs over time please

            return sb.ToString();
        }

        public static string TicsToReadable(long inTics) {
            var microSecCount = (float)(inTics / (System.Diagnostics.Stopwatch.Frequency / 1000000));

            if (microSecCount < 1000) {
                return microSecCount.ToString("0") + " μs";
            }
            else if (microSecCount < 10000000) {
                return (microSecCount / 1000).ToString("0") + " ms";
            }
            else return new TimeSpan(inTics).ToString();
        }

        public struct TimerInterval
        {
            public string intervalName;
            public long stopwatchTicks;
            public int frameCount;
            public long frameAgeTicks;

            internal string GetToString(int listingIndex, long delta = -1) {
                var pre = "fAge:" +TicsToReadable(frameAgeTicks);
                if (delta == -1) {
                    return pre + string.Format("{1}\tInterval:{0}", intervalName.PadRight(15), TicsToReadable(stopwatchTicks).PadLeft(10));
                }
                else {
                    return pre + string.Format("{1}d:{2}\tInterval:{0}", intervalName.PadRight(15), TicsToReadable(stopwatchTicks).PadLeft(10).PadRight(15), TicsToReadable(delta).PadLeft(10));
                }
            }
        }

        internal static void ResetOnWouldBeDomainReload() {
            m_doInstaLogAll = false;
            mainThread = null;
            runningTimers = new List<IntervalTimer>(16);
        }
    }
}