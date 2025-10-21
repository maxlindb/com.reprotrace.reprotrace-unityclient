using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MUtility
{
    [ExecuteAlways]
    public class AsyncRunnerHelper : MonoBehaviour
    {
        public enum FinisherCallingThreadMode
        {
            UNSET = 0,
            UnityMainThread = 1,
            NotUnityMainThread = 2
        }

        static AsyncRunnerHelper instance;

        bool IsEditor {
            get {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static Thread mainThread;

        public static bool doGeneralDebug = false;
        public static bool doWatchDogWatchDoggingDebug = true;
        public static bool doLogIfFinishersCallerLongSinceTickError = true;
        public static bool doDebugOnGUI = false;

        private const float TIME_NO_TICK_CONSIDERED_TOO_LONG = 3f;

        public static void InitializeIfNotInitialized()
        {
            if (instance == null)
            {
                dying = false;
                if (doGeneralDebug)Debug.Log("AsyncRunnerHelper initializing");
                instance = new GameObject("AsyncRunnerHelper").AddComponent<AsyncRunnerHelper>();
                instance.gameObject.hideFlags = HideFlags.DontSave;

                if (Application.isPlaying) {
                    DontDestroyOnLoad(instance.gameObject);                
                }
                
                instance.StartOffThreadFinisherMonitor();

                instance.PreWarmRunnerThreads();

                mainThread = Thread.CurrentThread;
            }
        }

        private void Awake() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.pauseStateChanged += EditorApplication_pauseStateChanged;
#endif
        }

#if UNITY_EDITOR
        private void EditorApplication_pauseStateChanged(UnityEditor.PauseState obj) {
            isPaused = obj == UnityEditor.PauseState.Paused;
        }
#endif

        private void PreWarmRunnerThreads() {
            List<RunnerThread> rnrs = new List<RunnerThread>();
            for (int i = 0; i < 10; i++) {
                var runner = GetPooledRunnerThread();
                rnrs.Add(runner);
            }
            foreach (var item in rnrs) {
                ReturnRunnerThread(item);
            }
        }

        /// <summary>
        /// Creates a new thread and runs the supplied action in it.
        /// </summary>    
        public static void RunInSeparateThread(System.Action toRunOnOffThread) {
            RunInSeparateThreadThenRunFinisher(toRunOnOffThread, null, FinisherCallingThreadMode.NotUnityMainThread);
        }

        public static void RunOnMainThread(System.Action toRunOnMainThread, bool doBlockCallingThreadTillDone = false) {            

            if(Thread.CurrentThread == mainThread) {
                //if (!doBlockCallingThreadTillDone) throw new System.Exception("wtf? you're already on main thread. Cannot do this async!");
                if(doBlockCallingThreadTillDone) {
                    toRunOnMainThread();
                    return;
                }
            }
            
            if(doBlockCallingThreadTillDone) {
                AutoResetEvent resetEvent = new AutoResetEvent(false);
                Exception pendingException = null;
                RunInSeparateThreadThenRunFinisher( () => Thread.Sleep(0), () => {
                    try {
                        toRunOnMainThread();
                    }
                    catch(System.Exception e) {
                        Debug.LogException(e);
                        pendingException = e;
                    }
                    resetEvent.Set();
                }, FinisherCallingThreadMode.UnityMainThread);
                resetEvent.WaitOne();
                if (pendingException != null) throw pendingException;
            }
            else {
                RunInSeparateThreadThenRunFinisher( () => Thread.Sleep(0), toRunOnMainThread, FinisherCallingThreadMode.UnityMainThread);
            }
        }

        /// <summary>
        /// First creates a new thread and runs the first supplied action in it, and when its done, calls the second action on Unity's main thread.
        /// </summary>
        public static void RunInSeparateThreadThenRunFinisher(System.Action toRunOnOffThread, System.Action toRunOnMainThreadWhenDone, FinisherCallingThreadMode finisherCallingMode)
        {
            InitializeIfNotInitialized();
            instance.RunAsyncInstance(toRunOnOffThread, toRunOnMainThreadWhenDone, finisherCallingMode);
        }

        Thread finisherscallingthread = null;

        private void StartOffThreadFinisherMonitor() {
            finisherscallingthread = new Thread( () => OffThreadFinisherTicker() );
            finisherscallingthread.Name = "AsyncRunnerHelper OffThreadFinisherTicker";
            finisherscallingthread.Start();

            var monitorThread = new Thread(() => FinishersTickingWatchDog());
            monitorThread.Name = "AsyncRunnerHelper FinishersTickingWatchDog";
            monitorThread.Start();
        }

# pragma warning disable CS0414
        bool isPaused = false;
#pragma warning restore CS0414

        private void FinishersTickingWatchDog() {            
            Thread.Sleep(1000);
            int cntr = 0;
            while(true) {
#if UNITY_EDITOR
                while(true) {
                    if (!isPaused) break;
                    Thread.Sleep(1000);
                }
                if (!playing) return;
#endif

                var timeSinceMainThreadTick = System.DateTime.Now - timeLastRanFinisherTickingFromMainThread;
                var timeSinceOffThreadTick = System.DateTime.Now - timeLastRanFinisherTIckingFromOffThread;

                if(timeLastRanFinisherTickingFromMainThread != default) {
                    if (timeSinceMainThreadTick.TotalSeconds > TIME_NO_TICK_CONSIDERED_TOO_LONG) {
                        if (doLogIfFinishersCallerLongSinceTickError) {
                            Debug.LogWarning("AsyncRunnerHelper: timeSinceMainThreadTick " + timeSinceMainThreadTick);
                            if(timeSinceMainThreadTick.TotalMinutes >= 2) {
#if !UNITY_EDITOR
                                Debug.LogError("GAME HAS PROBABLY LOCKED UP, no new frames during the last 2 minutes!");
#endif
                            }
                        }
                    }
                    if (timeSinceOffThreadTick.TotalSeconds > TIME_NO_TICK_CONSIDERED_TOO_LONG) {
                        if (doLogIfFinishersCallerLongSinceTickError) Debug.LogWarning("AsyncRunnerHelper: timeSinceOffThreadTick " + timeSinceOffThreadTick);
                    }
                }
                Thread.Sleep(1000);

                cntr++;
                if(cntr == 60) {
                    if(doWatchDogWatchDoggingDebug && !Application.isEditor)Debug.Log("FinishersTickingWatchDog ticking spam");
                    cntr = 0;
                }

                if (dying)
                    return;
            }
        }

        public static bool dying = false;

        private void OffThreadFinisherTicker()
        {
            while(true) {
                //Debug.Log("OffThreadFinisherMonitor");
                if (dying) {
                    if(doGeneralDebug) Debug.Log("OffThreadFinisherMonitor: dying");
                    return;
                }                
                try {                    
                    SafeFinishercallingTick(FinisherCallingThreadMode.NotUnityMainThread);
                }
                catch(System.Exception e) {
                    Debug.LogError("AsyncRunnerHelper off thread FinishersCallingTick crash:\n" + e);
                }
                
                Thread.Sleep(100);
            }
        }

        private void OnDestroy()
        {
            //Debug.Log("AsyncRunnerHelper OnDestroy");

            if (!IsEditor) {
                dying = true;
            }                


            foreach (var item in runningRunners) {
                item.Abort();
            }
        }

        float timeDotTime = 0f;

        bool playing;

        void Update()
        {
            playing = Application.isPlaying;
            if(/*!Application.isPlaying && */finisherscallingthread == null) { //stayed thru assembly relaod though should not have
                DestroyImmediate(gameObject);
                return;
            }

            mainThreadActionsRunThisFrame = 0;

            SafeFinishercallingTick(FinisherCallingThreadMode.UnityMainThread);

            if(!finisherscallingthread.IsAlive) {
                Debug.LogError("AsyncRunnerHelper: finisherscallingthread is dead! wtf");
            }
            timeDotTime = Time.time;

        }

        System.DateTime timeLastRanFinisherTickingFromMainThread;
        System.DateTime timeLastRanFinisherTIckingFromOffThread;

        public static bool doTickTraceDebug = false;
        

        void SafeFinishercallingTick(FinisherCallingThreadMode callingFromThreadMode)
        {
            bool lockWastaken = false;
            try {
                Monitor.TryEnter(runninThreadsLock, 10000, ref lockWastaken);
                if (!lockWastaken) {
                    Debug.LogError("AsyncRunnerHelper: FinishersCallingTick lock timeout!!! Is the previous stuck? this mode:"+callingFromThreadMode);
                }
                else {
                    FinishersCallingTick(callingFromThreadMode);
                }
            }
            finally {
                if (lockWastaken) {
                    Monitor.Exit(runninThreadsLock);
                }
            }
        }

        private void FinishersCallingTick(FinisherCallingThreadMode callingFromThreadMode)
        {
            if (doTickTraceDebug) Debug.Log("trace"+"1"+callingFromThreadMode);

            if(callingFromThreadMode == FinisherCallingThreadMode.UnityMainThread) {
                if (doTickTraceDebug) Debug.Log("trace" + "1a" + callingFromThreadMode);
                timeLastRanFinisherTickingFromMainThread = System.DateTime.Now;
            }
            else {
                if (doTickTraceDebug) Debug.Log("trace" + "1b" + callingFromThreadMode);
                timeLastRanFinisherTIckingFromOffThread = System.DateTime.Now;
            }

            if (doTickTraceDebug) Debug.Log("trace" + "2" + callingFromThreadMode);

            if (runningRunners.Count != 0) {
                if (doTickTraceDebug) Debug.Log("trace" + "3" + callingFromThreadMode);

                while (true) {
                    RunnerThread runnerToPop = null;
                    Action finisherFromDict = null;                    
                    
                    foreach (var runner in runningRunners)
                    {                        
                        if(!runner.isRunningOrPending)
                        {
                            if(runnersToFinisherActions.TryGetValue(runner, out var tempFinisher) && tempFinisher != null)
                            {                                                     
                                if (finisherCallingModesForRunners[runner] == callingFromThreadMode) {
                                    runnerToPop = runner;
                                    finisherFromDict = tempFinisher;                                    
                                    runnersToFinisherActions[runner] = null;
                                    finisherCallingModesForRunners[runner] = FinisherCallingThreadMode.UNSET;
                                    break;
                                }
                            }
                            else {
                                runnerToPop = runner;
                                break;
                            }
                        }
                    }
                    

                    if (doTickTraceDebug) Debug.Log("trace" + "4" + callingFromThreadMode);
                    if (runnerToPop == null) {                        
                        break;
                    }

                    if (doTickTraceDebug) Debug.Log("trace" + "5" + callingFromThreadMode);
                    runningRunners.Remove(runnerToPop);
                    ReturnRunnerThread(runnerToPop);



                    if (finisherFromDict == null) {
                        break;
                    }
                    if (doTickTraceDebug) Debug.Log("trace" + "6" + callingFromThreadMode);
                    var finisherTimer = System.Diagnostics.Stopwatch.StartNew();

                    if (doGeneralDebug) Debug.LogWarning("AsyncRunnerHelper calling finisher for " + finisherFromDict.Method);
                    if (callingFromThreadMode == FinisherCallingThreadMode.NotUnityMainThread) {
                        //make new thread for calling this finisher. This is due that that finisher possibly calling AsyncRunnerHelper again, and without this this could cause a deadlock!
                        var finisherRunningThread = new Thread(() => { //TODO OPTIMIZE!!!
                            try {
                                finisherFromDict();
                            }
                            catch (System.Exception e) {
                                Debug.LogError("AsyncRunnerHelper: finisher on off thread crashed!:\n" + e);
                            }
                        });
                        finisherRunningThread.Start();
                    }
                    else {
                        try {
                            finisherFromDict();
                        }
                        catch (System.Exception e) {
                            Debug.LogError("AsyncRunnerHelper: mainThreadFinisher crashed!:\n" + e);
                        }
                    }

                    if (finisherTimer.ElapsedMilliseconds > 33) {
                        Debug.LogWarning("AsyncRunnerHelper: Firing finisher on main thread took " + finisherTimer.ElapsedMilliseconds + " ms");
                        Debug.LogWarning("Finisher:"+ finisherFromDict.Method + " " + finisherFromDict.Target);
                    }
                    mainThreadActionsRunThisFrame++;
                    if (mainThreadActionLimitPerFrame == mainThreadActionsRunThisFrame) {
                        break;
                    }
                                        
                }
            }
        }


        List<RunnerThread> runningRunners = new List<RunnerThread>();
        ConcurrentDictionary<RunnerThread, Action> runnersToFinisherActions = new ConcurrentDictionary<RunnerThread, Action>();
        ConcurrentDictionary<RunnerThread, FinisherCallingThreadMode> finisherCallingModesForRunners = new ConcurrentDictionary<RunnerThread, FinisherCallingThreadMode>();
        
        
        private object runninThreadsLock = new object();
        System.Random rng;

        private void RunAsyncInstance(Action inAction, Action finisherAction, FinisherCallingThreadMode finisherCallingMode)
        {
            if(dying) {
                Debug.LogError("AsyncRunnerHelper: cannot run, app is dying");
                return;
            }

            if(rng == null) {
                rng = new System.Random();
            }
            var rand = rng.Next();
            
            if (doGeneralDebug) Debug.LogWarning("AsyncRunnerHelper RunAsyncInstance called:"+inAction.Method+ " rand"+ rand);

            if (finisherCallingMode == FinisherCallingThreadMode.UNSET)
                throw new System.Exception("finisherCallingMode == FinisherCallingThreadMode.Unset");

            //var t = new Thread(() => RunningAction(inAction));
            //t.Start();

            lock(runninThreadsLock)
            {
                var runner = RunSomethingOnSomeRunnerThread(inAction);

                if (finisherAction != null) {
                    runnersToFinisherActions[runner] = finisherAction;
                    finisherCallingModesForRunners[runner] = finisherCallingMode;
                }

                runningRunners.Add(runner);

                if (doGeneralDebug) {
                    var stack = new System.Diagnostics.StackTrace();
                    runner.debugInfo = rand + " " + stack.GetFrame(3) + " -> " + stack.GetFrame(2).ToString();
                }
            }                        

        }

        public RunnerThread RunSomethingOnSomeRunnerThread(Action inAction)
        {
            RunnerThread toRet = GetPooledRunnerThread();

            //actionsToRunners.Add(inAction, toRet);

            toRet.TriggerToRunSomething(inAction);

            return toRet;
        }        

        private RunnerThread GetPooledRunnerThread()
        {            
            if (pooledRunnerThreads.Count == 0/* || true*/) {
                if (timeDotTime > 2f) {
                    Debug.LogWarning("Creating new runner thread after start due to running out. 10 are pre-started at init. Will have:" + (pooledRunnerThreadCount + 1)+" running:"+runningRunners.Count+"\n\nSTACK:\n"+new System.Diagnostics.StackTrace(true).ToString());
                }
                pooledRunnerThreads.Add(RunnerThread.CreateNew());
                pooledRunnerThreadCount++;
            }
            var toRet = pooledRunnerThreads[0];
            pooledRunnerThreads.Remove(toRet);
            return toRet;
        }

        public int pooledRunnerThreadCount = 0;

        public void ReturnRunnerThread(RunnerThread runner)
        {            
            pooledRunnerThreads.Add(runner);            
        }

        public List<RunnerThread> pooledRunnerThreads = new List<RunnerThread>();
        public static int mainThreadActionLimitPerFrame = 9999999;
        public static int mainThreadActionsRunThisFrame = 0;

        public class RunnerThread
        {
            public string runnerName;
            Action toRunOrRunning;
            public bool isRunningOrPending;

            public static RunnerThread CreateNew() {
                var r = new RunnerThread();
                r.runnerName = "runner"+AsyncRunnerHelper.instance.pooledRunnerThreadCount.ToString();
                r.Init();
                return r;
            }

            Thread ownThread = null;

            void Init() {
                ownThread = new Thread(Loop);
                ownThread.Start();
            }

            AutoResetEvent waiter = new AutoResetEvent(false);

            public void TriggerToRunSomething(Action toRun)
            {
                isRunningOrPending = true;
                toRunOrRunning = toRun;
                waiter.Set();
            }


            void Loop()
            {
                try {
                    while (true) {
                        if (kill) return;
                        waiter.WaitOne();
                        running = true;
                        try {
                            toRunOrRunning();
                        }
                        catch (System.Exception e) { //happened once. hard to debug
                            if (toRunOrRunning == null) {
                                if (!AsyncRunnerHelper.dying) {
                                    Debug.LogError("AsyncRunnerHelper hit a weird race condition? toRunOrRunning == null");
                                }
                            }
                            Debug.LogException(e);
                        }
                        running = false;
                        toRunOrRunning = null;
                        isRunningOrPending = false;
                    }
                }
                catch(ThreadAbortException) {
                    //it's fine
                }
            }

            bool running = false;
            bool kill = false;
            internal string debugInfo;

            internal void Abort() {
                waiter.Set();
                if(running) {
                    ownThread.Abort();
                }
            }

            public override string ToString() {
                return runnerName;
            }
        }

        private void OnGUI()
        {
            if (!doDebugOnGUI)
                return;

            GUILayout.Space(200f);
            GUILayout.Label("AsyncRunnerHelper");
            GUILayout.Label("Pooled runners:" + pooledRunnerThreads.Count);
            var runningRunnersCopy = runningRunners.ToArray();
            GUILayout.Label("Running runners:" + runningRunnersCopy.Length);
            foreach (var item in runningRunnersCopy) {
                var hasFinishMode = finisherCallingModesForRunners.TryGetValue(item, out var mode) && mode != FinisherCallingThreadMode.UNSET;
                var finisherModeStr =  hasFinishMode ? mode.ToString() : "NONE";
                var hasFinisher = runnersToFinisherActions.TryGetValue(item, out var finisher) && finisher != null;
                var finisherStr = hasFinisher ? finisher.Target+" "+finisher.Method : "NONE";

                var finalStr = finisherModeStr.PadRight(50) + " finisher:" + finisherStr;
                var mismatch = hasFinisher != hasFinishMode;
                if (mismatch) {
                    Debug.LogError("MISMATCH");
                    finalStr += mismatch;
                }
                GUILayout.Label("\trunnin:" + item.isRunningOrPending + " " + finalStr+" dbg:"+item.debugInfo);
            }

            GUILayout.Label("runnersToFinisherActions:" + runnersToFinisherActions.Count);
            foreach (var item in runnersToFinisherActions) {
                GUILayout.Label(item.Key + ":" + item.Value);
            }
            GUILayout.Space(5f);
            GUILayout.Label("finisherCallingModesForRunners:" + finisherCallingModesForRunners.Count);
            foreach (var item in finisherCallingModesForRunners) {
                GUILayout.Label(item.Key + ":" + item.Value);
            }
        }
    }
}
