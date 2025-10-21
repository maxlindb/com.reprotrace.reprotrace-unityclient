using UnityEngine;
using System.Collections;

public class DummyScript : MonoBehaviour
{
	public IEnumerator RunCoroutineAndDie(IEnumerator coroutineToStart, System.Action onDone = null, System.Action<System.Exception> onException = null, int dontSkipFramesWhenDelayUnder = 0) {

		var runManually = onException != null || dontSkipFramesWhenDelayUnder > 0;

		if (runManually) {
            var timer = System.Diagnostics.Stopwatch.StartNew();
			while (true) {
				bool ran = false;
				try {
					ran = coroutineToStart.MoveNext ();
				}
				catch(System.Exception e) {
                    if (onException != null) onException(e);
                    else throw e;
				}

				if (!ran) break;

                if(dontSkipFramesWhenDelayUnder > 0) {
                    if(timer.ElapsedMilliseconds > dontSkipFramesWhenDelayUnder) {
                        yield return null;
                        timer.Reset();
                        timer.Start();
                    }
                    //else: keep running without actually skipping a frame
                }
				else yield return null;
			}
		}
		else {
			yield return StartCoroutine (coroutineToStart);
		}

        /*#if UNITY_EDITOR
                name += "SHOULD BE DEAD";
        #endif*/

        if (onDone != null) onDone ();

		//Destroy (gameObject); recycle
	}
}





















































































