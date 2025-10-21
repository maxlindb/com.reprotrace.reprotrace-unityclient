using MCrashReporter;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


public static class MBugContentUploader
{
    static bool initialized = false;

    public static bool useDropbox = false;
    public static bool useCustomBackEnd = true;

    public static bool useDropboxOnCustombackendFail = false;


    public static void InitializeIfNotInitializedYet()
    {
        if (!initialized) {
            Initialize();
        }
    }

    private static void Initialize()
    {
        if (useDropbox || useDropboxOnCustombackendFail) {
            InitializeDropbox();
        }
    }

    private static void InitializeDropbox()
    {
        MDropboxAPI.Init(Program.blob[1], Program.blob[2], Program.blob[3], Program.blob[4],
            (x) => Console.WriteLine(x),
            (x, y) => JsonConvert.DeserializeObject(x, y),
            (x) => JsonConvert.SerializeObject(x));
    }

    static int customBackEndFailureCount = 0;

    public static bool UploadFileToDepository(string filePath, string depositoryPath)
    {
        bool anythingSucceeded = false;
        if (useDropbox) {
            if (MDropboxAPI.UploadFileToDropBox(filePath, depositoryPath)) anythingSucceeded = true;
        }
        if (useCustomBackEnd) {
            var customBackendSuccess = MBugCustomBackEndUploader.UploadFile(filePath, depositoryPath);
            if (CustomBackEndFailOverCheck(filePath, depositoryPath, customBackendSuccess)) anythingSucceeded = true;
        }
        return anythingSucceeded;
    }

    public static void CreateFolder(string finalFolder)
    {
        if(useDropbox || useDropboxOnCustombackendFail)MDropboxAPI.CreateDropboxFolder(finalFolder);

        //custom backend is expected to do this automatically when uploading
    }



    private static bool CustomBackEndFailOverCheck(string filePath, string depositoryPath, bool customBackendSuccess)
    {
        if (!customBackendSuccess && useDropboxOnCustombackendFail)
        {
            customBackEndFailureCount++;
            if (customBackEndFailureCount == 1)
            {
                Console.WriteLine("MBugContentUploader: custom backend is not working. Falling back to dropbox upload path (this is now reported and this won't be spammed many days I promise)");
                var foldPath = depositoryPath.Substring(0, depositoryPath.LastIndexOf("/"));
                Console.WriteLine(foldPath);
                if (depositoryPath.Contains("SES"))
                {
                    var tempFilePath = System.IO.Path.GetTempFileName();
                    File.WriteAllText(tempFilePath, "custom backend fail started from" + filePath);
                    MDropboxAPI.UploadFileToDropBox(tempFilePath, foldPath + "/CUSTOM_BACKEND_FAILURE.txt");
                    File.Delete(tempFilePath);
                }
            }
            if (customBackEndFailureCount >= 1)
            {
                Console.WriteLine("MBugContentUploader: customBackEndFailureCount >= 1, flip");
                useCustomBackEnd = false;
                useDropbox = true;
            }

            return MDropboxAPI.UploadFileToDropBox(filePath, depositoryPath);
        }

        return false;
    }
}

