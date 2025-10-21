using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class FrameCounter : MonoBehaviour
{
    public BGVideoCapture bgCap;

    public bool spam;
    public bool simpleSpam;

    [Range(0,0.2f)]
    public float slowdown = 0f;

    [Range(0, 0.2f)]
    public float jit = 0f;

#if !DISABLE_MBUG
    // Update is called once per frame
    void Update()
    {
        GetComponent<TextMesh>().text = BGVideoCapture.RecordingTime.ToString("0.00")+" "+Time.frameCount.ToString("00000") + "\n" + BGVideoCapture.TotalFrameNum.ToString("00000");

        if (spam && Random.value < 0.05f)
        {
            Debug.Log("bgvid time:" + BGVideoCapture.RecordingTime.ToString("0.00") + " game frame:" + Time.frameCount.ToString("00000") + " bgvid frame:" + BGVideoCapture.TotalFrameNum.ToString("00000"));

            if(Random.value < 0.01f)
            {
                for (int i = 0; i < 10; i++)
                {
                    Debug.Log("randomspam:"+new System.Random().Next());
                }

                Debug.LogWarning("some weird");
            }

            if(Random.value < 0.001)
                Debug.LogError("too bad luck!!!");
        }

        if(slowdown > 0f)
        {
            Thread.Sleep((int)(slowdown * 1000));
        }
        if(jit > 0f)
        {
            if(Random.value > jit) Thread.Sleep((int)(jit * 1000));
        }

        if (simpleSpam) {
            Debug.Log("SimpleSpam frame " + Time.frameCount);
        }
    }
#endif
}
