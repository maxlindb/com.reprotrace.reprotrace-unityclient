#if !DISABLE_MBUG
using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public static class MBugCustomBackEndUploader 
{
    public static string configurationCustomBackendURL;
    public static string configurationAPIToken;

    //public static string Domain => "https://localhost:7192";
    public static string Domain => configurationCustomBackendURL;    


    public static string UploadAPIEndPoint => "/api/DepositoryUpload";
    public static string StreamUPloadAPIEndPoint => "/api/DepositoryUpload/UploadFrameData";
    public static string StreamUploadMSGPackAPIEndPoint => "/api/DepositoryUpload/UploadFrameDataMSGPackMiniBatch";

    public static string TestSettingsEndPoint => "/api/Projects/TestSettings";



    public static float someProgress = 0f;

    public static int totalReqFails = 0;

    static int[] retriedWaits = new int[] { 1000, 5000, 10000, 30000 };


    public static void Initialize() {
        configurationCustomBackendURL = MBugReporterClientConfiguration.Resource.GetbackEndURL();
        configurationAPIToken = MBugReporterClientConfiguration.Resource.projectAPIToken;
    }


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
    
    public static void TestSettings(out bool ok, out string error)
    {
        try
        {
            Initialize();
            var targetUrl = $"{Domain}{TestSettingsEndPoint}/" + MBugReporterClientConfiguration.Resource.projectAPIToken;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("TomIsAGenious", "TRUE");
            var resp = client.GetStringAsync(targetUrl).Result;
            var splt = resp.Split("|");
            ok = splt[0] == "0";
            error = splt[1];
        }
        catch(System.Exception e)
        {
            ok = false;
            error = "Failed checking settings\n"+e.Message;
            Debug.LogException(e);
        }
    }

    private static void UploadInternal(string localPath, string depositoryPath)
    {
        var targetUrl = $"{Domain}{UploadAPIEndPoint}";

        if (System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
            localPath = MaxinRandomUtils.GetWin32LongPath(localPath);
        }
        

        using (var client = new HttpClient())
        using (var fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 16 * 1024 * 1024))
        {
            var reqq = (HttpWebRequest)System.Net.HttpWebRequest.Create(targetUrl);
            reqq.Method = "POST";
            
            reqq.Headers.Add("depositoryPath", depositoryPath);
            reqq.Headers.Add("token", configurationAPIToken);
            reqq.Headers.Add("TomIsAGenious", "YES");

            reqq.ContentLength = fileStream.Length;
            reqq.AllowWriteStreamBuffering = false;

            //fileStream.CopyTo(reqq.GetRequestStream(),16 * 1024 * 1024);
            MaxinRandomUtils.CopyStream(fileStream, reqq.GetRequestStream(), "depoUpload_"+new FileInfo(localPath).Name, onAnalogProgress: ReportProgress);

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
        Debug.LogWarning(v);
    }

    private static void LogWarning(string v)
    {
        Debug.LogWarning(v);
    }

    public static bool UploadStreamedData(string json, string sessionID)
    {
        var targetUrl = $"{Domain}{StreamUPloadAPIEndPoint}";

        var reqq = (HttpWebRequest)System.Net.HttpWebRequest.Create(targetUrl);
        reqq.Method = "POST";

        reqq.Headers.Add("token", configurationAPIToken);
        reqq.Headers.Add("sessionID", sessionID);
        reqq.Headers.Add("TomIsAGenious", "YES");


        reqq.ContentType = "application/json";

        
        byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);        
        reqq.ContentLength = byteArray.Length;

        
        try
        {
            using (var requestStream = reqq.GetRequestStreamAsync().Result)
            {
                requestStream.WriteAsync(byteArray, 0, byteArray.Length).Wait();
            }
                
            using (var response = (HttpWebResponse)reqq.GetResponseAsync().Result)
            {   
                using (var streamReader = new StreamReader(response.GetResponseStream())) {
                    var responseBody = streamReader.ReadToEndAsync().Result;
                    if(response.StatusCode != HttpStatusCode.OK) {
                        throw new Exception($"Status code: {response.StatusCode}\n" + responseBody);
                    }
                }
            }
            return true;
        }
        catch(System.Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
}
#endif