using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using static MDropboxAPI;


public static class MBugCustomBackEndUploader 
{
    public static string Domain; //passed in by parent app

    public static string Token => MCrashReporter.Program.blob[0];  //passed in by parent app


    public static string UploadAPIEndPoint => "/api/DepositoryUpload";



    public static float someProgress = 0f;

    public static int totalReqFails = 0;

    static int[] retriedWaits = new int[] { 1000, 5000, 10000, 30000 };

    public static bool UploadFile(string localPath, string depositoryPath)
    {
        if (!depositoryPath.StartsWith("/")) depositoryPath = "/" + depositoryPath;

        int retriedCount = 0;

        while (true) {
            try
            {
                if (retriedCount > 0)
                {
                    var waitIDx = System.Math.Min(retriedCount, retriedWaits.Length - 1);
                    var wait = retriedWaits[waitIDx];
                    var randNorm = (float)new System.Random().Next(-1000, 1000) / 1000;
                    wait += (int)((float)wait * 0.25f * randNorm);
                    LogWarning("Retrying depository upload retriedCount: " + retriedCount + " after waiting " + wait + "ms \nLocal path:    " + localPath + "\nDepository path:   " + depositoryPath);
                    System.Threading.Thread.Sleep(wait);
                    LogWarning("Retrying now.");
                }

                UploadInternal(localPath, depositoryPath);
                if(retriedCount > 0)LogWarning("File upload succeeded after "+retriedCount+" retries.");
                break;
            }
            catch(System.Exception e)
            {
                LogWarning(e.ToString());
                var retry = true;

                if (retriedCount > 0) //TODO CHANGE THIS TO AT LEAST 4 AFTER CHANGEOVER IS COMPLETE
                    retry = false;
                
                if (!retry) {
                    LogError("Giving up retrying to upload " + localPath);
                    return false;
                    //break;
                }
                retriedCount++;
                totalReqFails++;
            }
        }

        return true;
    }
    

    private static void UploadInternal(string localPath, string depositoryPath)
    {
        var targetUrl = $"{Domain}{UploadAPIEndPoint}";

        using (var client = new HttpClient())
        using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 16 * 1024 * 1024))
        {
            var reqq = (HttpWebRequest)System.Net.HttpWebRequest.Create(targetUrl);
            reqq.Method = "POST";
            
            reqq.Headers.Add("depositoryPath", depositoryPath);
            reqq.Headers.Add("token", Token);
            reqq.Headers.Add("TomIsAGenious", "YES");

            reqq.ContentLength = fileStream.Length;
            reqq.AllowWriteStreamBuffering = false;

            //fileStream.CopyTo(reqq.GetRequestStream(),16 * 1024 * 1024);
            CopyStream(fileStream, reqq.GetRequestStream(), "depoUpload_"+new FileInfo(localPath).Name, onAnalogProgress: ReportProgress);

            //var response = client.PostAsync(targetUrl, requestContent).Result;

            var resp = (HttpWebResponse)reqq.GetResponse();
            var respStr = new StreamReader(resp.GetResponseStream()).ReadToEnd();

            if (resp.StatusCode != HttpStatusCode.OK) {
                throw new System.Exception("UploadInternal !response.IsSuccessStatusCode " + resp.StatusCode + "\n" + respStr);
            }
        }
    }


    private static void ReportProgress(float prog)
    {
        //Debug.Log("Upload:" + prog);
        someProgress = prog;
    }

    private static void LogError(string v)
    {
        //Debug.LogError(v);
        Console.WriteLine("ERROR:"+v);
    }

    private static void LogWarning(string v)
    {
        //Debug.LogWarning(v);
        Console.WriteLine(v);
    }



    //copypasta from BEAM/max's steam clone
    public static long CopyStream(Stream fromStream, Stream toStream, string progressReportingTag, int limitByteCount = -1, System.Action<float> onAnalogProgress = null, System.Action<ProgressInfo> onProgressAdvanced = null)
    {

        /*fromStream.CopyTo(toStream);
        return;*/

        long streamLenOrLimit = 0;

        try
        {
            streamLenOrLimit = fromStream.Length;
        }
        catch (System.NotSupportedException)
        {
            streamLenOrLimit = -1;
        }

        if (limitByteCount != -1 && streamLenOrLimit != -1)
        {
            streamLenOrLimit = System.Math.Min(streamLenOrLimit, limitByteCount);
        }

        //int reportEveryBytes = 100000;
        //int reportEveryBytes = 1024 * 1024 * 100; //100mb
        //int reportEveryBytes = 1024 * 128; //128k
        int reportEveryBytes = 1024 * 1024; //1mb


        int goneWithoutReportCounter = 0;
        long allReadCount = 0;

        //int bufLen = 1024 * 1024 * 16; //16MB, try to go faster
        int bufLen = 1024 * 1024 * 1; //1MB
        if (streamLenOrLimit != -1 && streamLenOrLimit < bufLen)
            bufLen = (int)streamLenOrLimit;

        byte[] buffer = new byte[bufLen];

        float smoothedDeltaTime = 1f;
        float velSpeed = 0f;

        var timeStart = System.DateTime.Now;

        var tickTimer = System.Diagnostics.Stopwatch.StartNew();

        int read;
        //while ((read = fromStream.Read(buffer, 0, buffer.Length)) > 0) {
        while (true)
        {
            read = fromStream.Read(buffer, 0, buffer.Length);
            if (read == 0) break;

            toStream.Write(buffer, 0, read);

            goneWithoutReportCounter += read;
            allReadCount += read;
            if (goneWithoutReportCounter >= reportEveryBytes)
            {
                var advancedThisTick = goneWithoutReportCounter;
                goneWithoutReportCounter = 0;

                var intervalTime = tickTimer.Elapsed;
                tickTimer.Restart();

                var rep = new ProgressInfo();
                rep.totalLen = streamLenOrLimit;
                rep.processedLen = allReadCount;
                rep.timeElapsed = System.DateTime.Now - timeStart;
                rep.perSec = (long)(advancedThisTick / intervalTime.TotalSeconds);
                rep.tag = progressReportingTag;

                //smoothedDeltaTime = Mathf.SmoothDamp(smoothedDeltaTime, (float)intervalTime.TotalSeconds, ref velSpeed, 5f, 10000000f, (float)intervalTime.TotalSeconds);
                //rep.perSecSmoothed = (long)(advancedThisTick / smoothedDeltaTime);

                //Debug.Log("Streaming data " + progressReportingTag + " " + ByteLenghtToHumanReadable(allReadCount) + " / " + (streamLenOrLimit == -1 ? "UNKNOWN" : ByteLenghtToHumanReadable(streamLenOrLimit)));
                //Debug.Log("Progress " + rep.tag + " " + rep.ReportString);
                //Console.ReadKey();

                if (onProgressAdvanced != null)
                {
                    onProgressAdvanced(rep);
                }

                if (onAnalogProgress != null)
                {
                    /*float toRep = 0f;
                    if (streamLenOrLimit != -1) {
                        toRep = (float)allReadCount / (float)streamLenOrLimit;
                    }
                    onAnalogProgress(toRep);*/
                    onAnalogProgress(rep.Progression);
                }
            }

            if (limitByteCount != -1 && allReadCount > limitByteCount)
            {
                throw new System.Exception("read too long wtf no");
            }
            if (allReadCount == limitByteCount)
            {
                break;
            }
        }

        //Debug.Log("CopyStream " + progressReportingTag + " done, transferred:" + ByteLenghtToHumanReadable(allReadCount));
        return allReadCount;
    }
}
