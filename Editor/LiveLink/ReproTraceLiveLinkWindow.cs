#if UNITY_EDITOR
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public delegate void ReproTraceLiveLinkCommandCallback(string command, string fileUrl, string fileName);

public class ReproTraceLiveLinkWindow : EditorWindow
{
    public static ReproTraceLiveLinkCommandCallback onCommand = null;

    bool poll;
    string machineName;
    string lastResponse = "";
    double nextPoll;

    [MenuItem("Tools/ReproTrace/LiveLink")]
    public static void ShowWindow() { GetWindow<ReproTraceLiveLinkWindow>("ReproTrace.com LiveLink"); }

    void OnEnable() { MBugCustomBackEndUploader.Initialize(); machineName = Environment.MachineName; EditorApplication.update += Tick; }
    void OnDisable() { EditorApplication.update -= Tick; }

    
    string lastResponseRan = "";
    //static HashSet<string> ranCommands = new HashSet<string>();    

    void OnGUI()
    {
        EditorGUILayout.LabelField("Backend", ReproTraceClientConfiguration.Resource.GetbackEndURL());
        machineName = EditorGUILayout.TextField("Machine", machineName);
        poll = EditorGUILayout.Toggle("Live poll (1 Hz)", poll);
        if (GUILayout.Button("Poll now")) DoPoll();
        EditorGUILayout.LabelField("Last response:");
        var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        EditorGUILayout.TextArea(lastResponse, style, GUILayout.MinHeight(120));

        if(onCommand == null) {
            EditorGUILayout.HelpBox("You have nothing registered to ReproTraceLiveLinkWindow.onCommand! This means your code will not receive any commands.", MessageType.Error);
        }
    }

    private void RunCommand(PendingLiveCommand item)
    {
        Debug.Assert(item.forMachine == machineName);

        Debug.Log("RunCommand " + item.command+ " fileName:" + item.fileName + " fileUrl:" + item.fileUrl);

        if(onCommand != null) {
            onCommand(item.command,item.fileUrl,item.fileName);
        }
        else {
            Debug.LogError("You have nothing registered to ReproTraceLiveLinkWindow.onCommand! Your command was ignored.");
        }
    }

    bool reqDone = false;

    void Tick()
    {
        if (reqDone) {
            reqDone = false;
            //Debug.Log("repait");
            Repaint();
        }

        if (!string.IsNullOrEmpty(lastResponse)) {
            if(lastResponseRan != lastResponse) {
                lastResponseRan = lastResponse;
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<LiveLinkAdvertiseAndGetCommandResponse>(lastResponse);

                foreach (var item in parsed.commandsToRun)
                {
                    RunCommand(item);
                }
            }
        }

        if (!poll) return;
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + 1.0;
        
        DoPoll();
    }

    void DoPoll()
    {
        var token = ReproTraceClientConfiguration.Resource.projectAPIToken;
        var url = ReproTraceClientConfiguration.Resource.GetbackEndURL() + "/api/LiveLink/AdvertiseAndGetPendingCommand";        
        //Debug.Log("req start");
        new Thread(() =>
        {
            try
            {                                
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.Headers.Add("token", token);
                var body = JsonConvert.SerializeObject(new LiveLinkAdvertiseAndGetCommandRequest { computerName = machineName });
                var bytes = Encoding.UTF8.GetBytes(body);
                req.ContentType = "application/json";
                req.ContentLength = bytes.Length;
                using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var rs = resp.GetResponseStream())
                using (var sr = new StreamReader(rs, Encoding.UTF8)) lastResponse = sr.ReadToEnd();
               // Debug.Log("polldone "+lastResponse);
            }
            catch (Exception e) { 
                lastResponse = e.Message;
                Debug.LogWarning(e);
            }
            reqDone = true;
        }).Start();
    }
}
#endif
