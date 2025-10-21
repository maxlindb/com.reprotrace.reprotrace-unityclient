#if !DISABLE_MBUG
using MUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


public static class MDropboxAPI
{
    static string bearerToken = "UNSET";

    static string refreshToken;
    static string appKey;
    static string appSecret;


    static string rootScope = null;

    public static List<DropboxFileListingEntry> GetFilesAndFolders(string inPath, bool recursive = false) {
        return GetFilesAndFolders(inPath, out var ignore, recursive);
    }

    public static List<DropboxFileListingEntry> GetFilesAndFolders(string inPath, out string cursor, bool recursive = false)
    {
        var paramsObj = new DropboxListingParams
        {
            path = inPath,
            limit = 1000,
            recursive = recursive,
            include_deleted = false
        };
        var paramsJson = ToJSON(paramsObj);

        var respStr = HttpPostDropBox("https://api.dropboxapi.com/2/files/list_folder", paramsJson);

        if (respStr == null) {
            cursor = "";
            return new List<DropboxFileListingEntry>();
        }


        respStr = respStr.Replace(".tag", "tag"); //lol
        var respObj = FromJSON<DropboxFileListingResponse>(respStr);
        var entries = respObj.entries;

        int extendTimes = 0;
        string currCursor = respObj.has_more ? respObj.cursor : "";
        while (!string.IsNullOrEmpty(currCursor)) {
            Log("has cursor, obtaining more data " + entries.Count);
            DropboxListingContinueParams continueParams = new DropboxListingContinueParams { cursor = currCursor };
            var continueParamsJson = ToJSON(continueParams);
            respStr = HttpPostDropBox("https://api.dropboxapi.com/2/files/list_folder/continue", continueParamsJson);
            respStr = respStr.Replace(".tag", "tag"); //lol
            respObj = FromJSON<DropboxFileListingResponse>(respStr);
            entries.AddRange(respObj.entries);
            Log("got " + respObj.entries.Count + " more entries thru cursor, now:" + entries.Count);

            currCursor = respObj.has_more ? respObj.cursor : "";
            extendTimes++;
        }

        Log("GetFilesAndFolders got " + entries.Count + " entries " + "(" + extendTimes + " extensions)");

        cursor = respObj.cursor;

        return entries;
    }

    public static void Download(string fromDropboxPath, string toFilepath) {
        if (!fromDropboxPath.StartsWith("/")) {
            fromDropboxPath = "/" + fromDropboxPath;
        }
        var paramsObj = new DropboxDownloadParams { path = fromDropboxPath };
        var argsJson = ToJSON(paramsObj);
        HttpPostDropBox("https://content.dropboxapi.com/2/files/download", argsJson, null, toFilepath);
    }

    
    static float someProgress = 0f;

    public static float GetSomeProgress()
    {
        return someProgress;

        /*if (prevStream != null) {
            UnityEngine.Debug.Log(prevStream.Position);
            var prog = (float)prevStream.Position / (float)prevStream.Length;
            return prog;
        }
        return -1f;*/
    }


    private static void ReportProgress(float obj)
    {
        someProgress = obj;
    }

    public static string HttpPostDropBox(string url, string dropboxArgs, Stream bodyStream = null, string dumpResultContentTo = null, bool requireAuth = true)
    {
        Log("HttpPostDropBox " + url);

        bool triedRefresh = false;

        while (true)
        {
            try
            {
                return InnerHttpPostDropBox(url, dropboxArgs, bodyStream, dumpResultContentTo, requireAuth);                
            }
            catch (System.Threading.ThreadAbortException) { return null; }
            catch (WebException e)
            {   
                LogError("Dropbox-API-Result err:" + e.Message);

                string bodyErr = "NO_BODY";
                if(e.Response != null) {
                    bodyErr = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                }

                LogError("bodyErr:"+bodyErr);

                if (bodyErr.Contains("expired")) {
                    if (triedRefresh) {
                        LogError("Failed even after token refresh");
                        return null;
                    }

                    RefreshAccessToken();
                    triedRefresh = true;
                }
                else {
                    return null;
                }
            }
        }

    }

    private static string InnerHttpPostDropBox(string url, string dropboxArgs, Stream bodyStream = null, string dumpResultContentTo = null, bool requireAuth = true)
    {
        Log("InnerHttpPostDropBox " + url);

        var reqq = (HttpWebRequest)System.Net.HttpWebRequest.Create(url);

        reqq.Method = "POST";

        if (requireAuth)
            reqq.Headers.Add("Authorization", $"Bearer {bearerToken}");

        if (rootScope != null)
        {
            var rootJeisson = "{ \".tag\": \"root\", \"root\": \"" + rootScope + "\"}";
            reqq.Headers.Add("Dropbox-API-Path-Root", rootJeisson);
        }

        //prevStream = bodyStream;
        //file uploads have just the file in the body. Other requests have the dropbox args json in the body (instead of being in the header)
        if (bodyStream != null || dumpResultContentTo != null)
        {
            reqq.Headers.Add("Dropbox-API-Arg", dropboxArgs);
            reqq.ContentType = "application/octet-stream";
            //reqq.SendChunked = true;
            if(bodyStream != null)reqq.ContentLength = bodyStream.Length;
            reqq.AllowWriteStreamBuffering = false;
        }
        else
        {
            reqq.ContentType = "application/json";
            var jsonBytes = System.Text.Encoding.UTF8.GetBytes(dropboxArgs);
            reqq.GetRequestStream().Write(jsonBytes, 0, jsonBytes.Length);
        }

        if (bodyStream != null)
        {
            someProgress = -1f;
            CopyStream(bodyStream, reqq.GetRequestStream(), "dropboxUpload", onAnalogProgress: ReportProgress);
            someProgress = 1f;
            //bodyStream.CopyTo(reqq.GetRequestStream());
        }

        //var resp = GetResponseOrLogFail(reqq);
        var resp = reqq.GetResponse();

        string respStr;
        if (dumpResultContentTo != null)
        {
            respStr = reqq.Headers["Dropbox-API-Result"];
            new FileInfo(dumpResultContentTo).Directory.Create();
            using (var fs = new FileStream(dumpResultContentTo, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                resp.GetResponseStream().CopyTo(fs);
            }
        }
        else
        {
            respStr = new StreamReader(resp.GetResponseStream()).ReadToEnd();
        }
        Log("RESP:" + respStr);

        resp.Dispose();
        

        return respStr;
    }

    private static WebResponse GetResponseOrLogFail(System.Net.WebRequest reqq)
    {
        try {
            var resp = reqq.GetResponse();
            return resp;
        }
        catch (System.Threading.ThreadAbortException) { return null; }
        catch (WebException e) {
            var resp = reqq.Headers["Dropbox-API-Result"];
            LogError("Dropbox-API-Result err:" + resp+"\n"+e);
            var err = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
            LogError(err);

            if (err.Contains("expired")) {
                RefreshAccessToken();
                reqq.Headers["Authorization"] = $"Bearer {bearerToken}";
                return GetResponseOrLogFail(reqq);
            }

            return e.Response;            
        }
    }

    public class AccessAndRefreshTokenResponse
    {
        public string access_token;
        public string refresh_token;
    }

    //use if new refreshToken is needed
    public static void OauthWithCodeHack()
    {
        string code = "";
        string appKey = "";
        string appSecret = "";
        string redirectUri = "http://localhost/dropboxredirect";

        using (var httpClient = new HttpClient())
        {
            var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("client_id", appKey),
                    new KeyValuePair<string, string>("client_secret", appSecret),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });

            var response = httpClient.PostAsync("https://api.dropbox.com/oauth2/token", requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            LogError(responseContent);
            var jsonResponse = FromJSON<AccessAndRefreshTokenResponse>(responseContent);

            string accessToken = jsonResponse.access_token;
            string refreshToken = jsonResponse.refresh_token;

            LogError("accessToken:"+ accessToken);
            LogError("refreshToken:" + refreshToken);
        }        
    }

    public class RefreshAccessTokenResponse
    {
        public string access_token;
    }

    private static void RefreshAccessToken()
    {
        using (var httpClient = new HttpClient())
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", appKey),
                new KeyValuePair<string, string>("client_secret", appSecret)
            });

            var response = httpClient.PostAsync("https://api.dropbox.com/oauth2/token", requestContent).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var jsonResponse = FromJSON<RefreshAccessTokenResponse>(responseContent);

            Log("Token refresh response:"+responseContent);

            //return jsonResponse["access_token"].ToString();
            bearerToken = jsonResponse.access_token;
        }
    }


    public static void CreateDropboxFolder(string appRelativeDropboxPath) {
        if (!appRelativeDropboxPath.StartsWith("/")) {
            appRelativeDropboxPath = "/" + appRelativeDropboxPath;
        }
        var dropArgs = "{\"path\": \"" + appRelativeDropboxPath + "\", \"autorename\": false }";
        try {
            HttpPostDropBox("https://api.dropboxapi.com/2/files/create_folder_v2", dropArgs);
        }
        catch (System.Net.WebException e) {
            if (!e.Message.Contains("409")) {
                throw e;
            }
        }
    }

    static int[] retriedWaits = new int[] { 1000, 5000, 10000, 30000 };

    public static bool UploadFileToDropBox(string filePath, string dropboxFilePath)
    {
        if (!dropboxFilePath.StartsWith("/")) {
            dropboxFilePath = "/" + dropboxFilePath;
        }
        var dropArgs = "{ \"path\": \"" + dropboxFilePath + "\", \"mode\": \"add\", \"autorename\": true, \"mute\": false, \"strict_conflict\": false }";


        int retriedCount = 0;
        

        while (true) {
            if(retriedCount > 0) {
                var waitIDx = System.Math.Min(retriedCount, retriedWaits.Length - 1);
                var wait = retriedWaits[waitIDx];
                var randNorm = (float)new System.Random().Next(-1000, 1000) / 1000;
                wait += (int)((float)wait * 0.25f * randNorm);
                LogError("Retrying dropbox upload, retriedCount: " + retriedCount + " after waiting "+wait+"ms \nLocal path:    "+filePath+"\nDropbox path:   "+dropboxFilePath);
                System.Threading.Thread.Sleep(wait);
            }

            if(System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
                filePath = GetWin32LongPath(filePath);
            }

            string resp = "";
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                resp = HttpPostDropBox("https://content.dropboxapi.com/2/files/upload", dropArgs, fs);
            }
            //var retry = resp.Contains("(429) Too Many Requests");
            var retry = resp == null || resp.Contains("retry_after");

            if (retriedCount > 2) {
                return false;
            }

            if (!retry) {
                break;
            }


            retriedCount++;
        }        
        return true;
    }

    public static DrobboxGetThumbnailBatchResult GetThumbnailsBatch(List<string> paths, string quality = "w64h64")
    {
        var parms = new DropboxGetThumbnailsBatchParams
        {
            entries = paths.Select(x => new DropboxThumbnailsBatchSingleParams { path = x, format = "jpeg", size = quality, mode = "strict" }).ToList()
        };
        var paramsJson = ToJSON(parms);

        var respStr = HttpPostDropBox("https://content.dropboxapi.com/2/files/get_thumbnail_batch", paramsJson);
        respStr = respStr.Replace(".tag", "tag"); //lol
        var respObj = FromJSON<DrobboxGetThumbnailBatchResult>(respStr);

        return respObj;
    }



    static System.Func<string, System.Type, object> jsonDeserializeFunc = null;
    static System.Func<object, string> jsonSerializeFunc = null;

    private static string ToJSON(object obj) {
        EnsureHaveJSONHooksRegistered();
        var json = jsonSerializeFunc(obj);
        return json;
    }

    private static T FromJSON<T>(string json) {
        var type = typeof(T);
        object obj = null;
        try {
            obj = jsonDeserializeFunc(json, type);
        }
        catch(System.Exception e) {
            LogError("Fail parsing JSON:" + e + "\n\nTried to parse:\n" + json);
            return default(T);
        }
        return (T)obj;
    }

    private static void EnsureHaveJSONHooksRegistered() {
        if (jsonDeserializeFunc == null) throw new System.Exception("jsonDeserializeFunc not registered");
        if (jsonSerializeFunc == null) throw new System.Exception("jsonSerializeFunc not registered");
    }

    static System.Action<string> logFunc;

    private static void Log(string v, bool error = false) {
        if (!doInternalLog && !error) return;
        logFunc(v);
    }

    private static void LogError(string v) => Log(v, true);

    public static bool doInternalLog = false;

    public static void Init(string inBearerToken, string inAppKey, string inAppSecret, string inRefreshToken, System.Action<string> inLogFunc, System.Func<string, System.Type, object> inJsonDeserializeFunc, System.Func<object, string> inJsonSerializeFunc, string inRootScope = null) {
        bearerToken = inBearerToken;
        logFunc = inLogFunc;
        jsonDeserializeFunc = inJsonDeserializeFunc;
        jsonSerializeFunc = inJsonSerializeFunc;
        rootScope = inRootScope;

        appKey = inAppKey;
        appSecret = inAppSecret;
        refreshToken = inRefreshToken;

        //if no key, use an expired key to force refresh
        if(bearerToken == "") {
            bearerToken = "sl.BuuGLwzqrMwl1Gky6xJJpfIuwOYtzYEKGFfongQiLg-mR_Dtz1KzgAIhswrlD2Rr7IZBV3Pl0E6OU2ygghfJ4_fj0YObelF1LRCjeELoKqpRpEW01-ED0RTWILZMdPYOB0IJA-g0RAGl";
        }

        RefreshAccessToken();
    }


    public static void CombinedLongPollFolderForChanges(string inPath, System.Action<string> onInitialListing) {
        GetFilesAndFolders(inPath, out var cursor);
        if (onInitialListing != null) {
            onInitialListing(inPath);
        }

        LongPollFolderWithCursor(cursor);
    }

    public static bool LongPollFolderWithCursor(string cursor) {
        var url = "https://notify.dropboxapi.com/2/files/list_folder/longpoll";

        var paramsObj = new DropboxLongPollFolderParams
        {
            cursor = cursor,
            timeout = 30
        };
        var paramsJson = ToJSON(paramsObj);

        var resp = HttpPostDropBox(url, paramsJson, requireAuth: false);
        var respObj = FromJSON<DropboxLongPollFolderResponse>(resp);
        return respObj.changes;
    }


    public static DropboxUserInfo GetUserInfo(string dbIDFull)
    {
        var url = "https://api.dropboxapi.com/2/users/get_account";
        var paramsJson = "{ \"account_id\": \"DBIDHERE\" }";
        paramsJson = paramsJson.Replace("DBIDHERE", dbIDFull);

        var resp = HttpPostDropBox(url, paramsJson);
        var obj = FromJSON<DropboxUserInfo>(resp);

        return obj;
    }

    public static void DownloadFolderAsZip(string folderPath, string localPath)
    {
        var paramsObj = new DropboxDownloadParams { path = folderPath };
        var argsJson = ToJSON(paramsObj);
        HttpPostDropBox("https://content.dropboxapi.com/2/files/download_zip", argsJson, null, localPath);
    }

    public static void Delete(string path_lower)
    {
        var paramsObj = new DropboxDownloadParams { path = path_lower };
        var argsJson = ToJSON(paramsObj);
        var resp = HttpPostDropBox("https://api.dropboxapi.com/2/files/delete_v2", argsJson);
        if (resp.Contains("\"error\"")) {
            throw new System.Exception("operation failure " + resp);
        }
    }


    public static string GetFolderShareLink(string appRelativeDropboxPath)
    {
        if (!appRelativeDropboxPath.StartsWith("/")) {
            appRelativeDropboxPath = "/" + appRelativeDropboxPath;
        }
        var reqObj = new DropboxShareFolderRequest { path = appRelativeDropboxPath, settings = new DropboxShareFolderRequestSettings { access = "viewer", allow_download = true, audience = "public", requested_visibility = "public" } };
        var dropArgs = ToJSON(reqObj);

        var resp = HttpPostDropBox("https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings", dropArgs);

        //find url (hacky but avoids having tons of model classes)
        var urlStart = resp.IndexOf("\"url\": \"");
        var urlEnd = resp.Substring(urlStart).IndexOf("\"");
        var url = resp.Substring(urlStart, urlEnd - urlStart);        
        return url;
    }

    public static void DebugNamespaces()
    {
        var resp = HttpPostDropBox("https://api.dropboxapi.com/2/users/get_current_account",""); ;
        LogError(resp);
        System.Console.WriteLine(resp);
    }



    //copypasta from BEAM/max's steam clone
    public static long CopyStream(Stream fromStream, Stream toStream, string progressReportingTag, int limitByteCount = -1, System.Action<float> onAnalogProgress = null, System.Action<MDropboxAPI.ProgressInfo> onProgressAdvanced = null)
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

#if UNITY_2017_1_OR_NEWER
        float smoothedDeltaTime = 1f;
        float velSpeed = 0f;
#endif

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

#if UNITY_2017_1_OR_NEWER
                smoothedDeltaTime = UnityEngine.Mathf.SmoothDamp(smoothedDeltaTime, (float)intervalTime.TotalSeconds, ref velSpeed, 0.5f, 10000000f, (float)intervalTime.TotalSeconds);
                rep.perSecSmoothed = (long)(advancedThisTick / smoothedDeltaTime);
#endif

                //Debug.Log("Streaming data " + progressReportingTag + " " + ByteLenghtToHumanReadable(allReadCount) + " / " + (streamLenOrLimit == -1 ? "UNKNOWN" : ByteLenghtToHumanReadable(streamLenOrLimit)));
                Log("Progress " + rep.tag + " " + rep.ReportString);
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

    public static string ByteLenghtToHumanReadable(long byteLenght, bool useBits = false)
    {
        string suffix = "";
        long lenght;
        if (useBits) lenght = byteLenght * 8;
        else lenght = byteLenght;

        int kilo = 1024;
        int mega = 1024 * 1024;
        int giga = 1024 * 1024 * 1024;

        if (lenght < kilo)
        {
            if (useBits) suffix = " bits";
            else suffix = " B";
            return lenght + suffix;
        }
        else if (lenght < mega)
        {
            if (useBits) suffix = " kbits";
            else suffix = " kB";
            return (lenght / kilo) + suffix;
        }
        else if (lenght < giga)
        {
            if (useBits) suffix = " Mbits";
            else suffix = " MB";
            return System.Math.Round((float)lenght / mega, 2) + suffix;
        }
        else
        {
            if (useBits) suffix = " Gbits";
            else suffix = " GB";
            return System.Math.Round((float)lenght / giga, 2) + suffix;
        }
    }

    public static string GetWin32LongPath(string path) {
        if (path.StartsWith(@"\\?\")) return path;

        if (path.StartsWith("\\")) {
            path = @"\\?\UNC\" + path.Substring(2);
        }
        else if (path.Contains(":")) {
            path = @"\\?\" + path;
        }
        else {
            var currdir = System.Environment.CurrentDirectory;
            path = Combine(currdir, path);
            while (path.Contains("\\.\\")) path = path.Replace("\\.\\", "\\");
            path = @"\\?\" + path;
        }
        return path.TrimEnd('.'); ;
    }

    private static string Combine(string path1, string path2) {
        return path1.TrimEnd('\\') + "\\" + path2.TrimStart('\\').TrimEnd('.'); ;
    }


    public struct ProgressInfo
    {
        public long processedLen;
        public long totalLen;
        public System.TimeSpan timeElapsed;
        public long perSec;
        public string tag;
        public long perSecSmoothed;

        public string ReportString
        {
            get
            {
                return ByteLenghtToHumanReadable(processedLen) + " / " + (totalLen == -1 ? "UNKNOWN" : ByteLenghtToHumanReadable(totalLen) + " (" + SpeedReportString + ")");
            }
        }
        public string SpeedReportString
        {
            get
            {
                return ByteLenghtToHumanReadable(perSecSmoothed) + "/s";
            }
        }

        public float Progression
        {
            get
            {
                return (float)processedLen / totalLen;
            }
        }
    }
}

public class DropboxLongPollFolderParams
{
    public string cursor;
    public int timeout;
}
public class DropboxLongPollFolderResponse
{
    public bool changes;
}

[System.Serializable]
public class DropboxGetThumbnailsBatchParams
{
    public List<DropboxThumbnailsBatchSingleParams> entries;
}
[System.Serializable]
public class DropboxThumbnailsBatchSingleParams
{
    public string path;
    public string format;
    public string size;
    public string mode;
}

[System.Serializable]
public class DropboxListingParams
{
    public string path;
    public int limit;
    public bool recursive;
    public bool include_media_info;
    public bool include_deleted;
    public bool include_has_explicit_shared_members;
    public bool include_mounted_folders;
    public bool include_non_downloadable_files;
}

[System.Serializable]
public class DrobboxGetThumbnailBatchResult
{
    public List<GetThumbnailBatchResultEntry> entries;
}
[System.Serializable]
public class GetThumbnailBatchResultEntry
{
    public GetThumbnailBatchResultEntryMetadata metadata;
    public string thumbnail;
}
[System.Serializable]
public class GetThumbnailBatchResultEntryMetadata
{
    public string name;
}


[System.Serializable]
public class DropboxListingContinueParams
{
    public string cursor;
}

[System.Serializable]
public class DropboxDownloadParams
{
    public string path;
}

[System.Serializable]
public class DropboxFileListingResponse
{
    public List<DropboxFileListingEntry> entries;
    public string cursor;
    public bool has_more;
}

[System.Serializable]
public class DropboxFileListingEntry
{
    public string tag;
    public string name;
    public string path_lower;
    public string path_display;
    public string id;
    public System.DateTime client_modified;
    public System.DateTime server_modified;
    public string rev;
    public long? size;
    public SharingInfo sharing_info;
    public bool? is_downloadable;
    public string content_hash;
}

[System.Serializable]
public class SharingInfo
{
    public bool read_only;
    public string parent_shared_folder_id;
    public string modified_by;
}

[System.Serializable]
public class Name
{
    public string given_name;
    public string surname;
    public string familiar_name;
    public string display_name;
    public string abbreviated_name;
}

[System.Serializable]
public class DropboxUserInfo
{
    public string account_id;
    public Name name;
    public string email;
    public bool email_verified;
    public bool disabled;
    public bool is_teammate;
    public string team_member_id;
}

[System.Serializable]
public class DropboxShareFolderRequest
{
    public string path;
    public DropboxShareFolderRequestSettings settings;
}

[System.Serializable]
public class DropboxShareFolderRequestSettings
{
    public string access;
    public bool allow_download;
    public string audience;
    public string requested_visibility;
}



#endif