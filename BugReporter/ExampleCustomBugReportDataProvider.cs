using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class ExampleCustomBugReportDataProvider : MonoBehaviour
{
    private void Start()
    {
        MBugReporter.onProvideGameSpecificBugReporterData = ProvideGameSpecificBugReporterdata;
    }

    private static void ProvideGameSpecificBugReporterdata(string bugReportFolderPath)
    {
        var path = Path.Combine(bugReportFolderPath, "exampleCustomGameData" + UnityEngine.Random.value+".txt");
        File.WriteAllText(path, "test content");
    }
}

