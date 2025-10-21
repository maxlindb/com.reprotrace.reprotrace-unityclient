#if !DISABLE_MBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class MBugContentUploader
{
    static bool initialized = false;
    

    public static void InitializeIfNotInitializedYet()
    {
        if (!initialized) {
            Initialize();
        }
    }

    private static void Initialize()
    {
        MBugCustomBackEndUploader.Initialize();
    }

    
    public static bool UploadFileToDepository(string filePath, string depositoryPath, bool neverDropBox = false)
    {        
        return MBugCustomBackEndUploader.UploadFile(filePath, depositoryPath);        
    }

    public static void CreateFolder(string finalFolder)
    {   
        //custom backend is expected to do this automatically when uploading
    }
}
#endif
