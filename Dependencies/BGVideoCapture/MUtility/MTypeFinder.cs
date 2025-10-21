using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MUtility;
using System.Text;
using System.Linq;

public static class MTypeFinder
{
    public static Type FindType(string shortTypename) {
        GeneratedTargetedTypeListIfNeeded();
        if (typesFastAccesDict.ContainsKey(shortTypename)) {
            return typesFastAccesDict[shortTypename];
        }
        return null;
    }

    static List<Type> cachedTargetedTypeList = null;
    static Dictionary<string, Type> typesFastAccesDict = new Dictionary<string, Type>();


    private static BidirectionalDictionary<int, Type> repeatableTypeCodes = new BidirectionalDictionary<int, Type>();
    public static BidirectionalDictionary<int, Type> RepeatableTypeCodes {
        get {
            GeneratedTargetedTypeListIfNeeded();
            return repeatableTypeCodes;
        }
    }

    public static List<Type> ClassesThatCouldHaveSerializableAttribute {
        get {
            GeneratedTargetedTypeListIfNeeded();            
            return m_classesThatCouldHaveSerializableAttribute;
        }
    }
    private static List<Type> m_classesThatCouldHaveSerializableAttribute = null;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Debug/DebugGeneratedTargetedTypeList")]
#endif
    public static void DebugGeneratedTargetedTypeListIfNeeded() {
        cachedTargetedTypeList = null;
        repeatableTypeCodes.Clear();
        GeneratedTargetedTypeListIfNeeded();
    }

    static private void GeneratedTargetedTypeListIfNeeded() {
        if (cachedTargetedTypeList != null) return;

        int numberedClassesCount = 0;

        var timer = System.Diagnostics.Stopwatch.StartNew();
        cachedTargetedTypeList = new List<Type>();
        m_classesThatCouldHaveSerializableAttribute = new List<Type>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        //string[] ourAssemblies = new[] {  }
        assemblies = assemblies.OrderByDescending(x => x.FullName.Contains("Assembly-CSharp")).ThenByDescending(l => l.FullName.Contains("GUIDPun")).ThenByDescending(k => k.FullName.Contains("GOTO")).ToArray(); //have our types first
        var md5Hasher = System.Security.Cryptography.MD5.Create();


        //Debug.Log("Assemblies:\n" + assemblies.Select(x => x.FullName).Aggregate((k, l) => k + "\n" + l));
        foreach (var assembly in assemblies) {
            var types = assembly.GetTypes();
            cachedTargetedTypeList.AddRange(types);
            foreach (var typp in types) {
                if (!typesFastAccesDict.ContainsKey(typp.Name)) { //we don't care about duplicates - if exists leave it be, we're dealing with quite specified types anyway.
                    typesFastAccesDict.Add(typp.Name, typp);
                }

                var considerForNumberedClasses = true;
                if (!assembly.FullName.Contains("Assembly-CSharp") && !assembly.FullName.Contains("UnityEngine.") && !assembly.FullName.Contains("GOTO") && !assembly.FullName.Contains("GUIDPun"))
                    considerForNumberedClasses = false;


                if (considerForNumberedClasses) {
                    //var hash = typp.GetHashCode();
                    //repeatableTypeCodes.Add(hash, typp);

                    //repeatableTypeCodes.Add(numberedClassesCount, typp);

                    var typeString = typp.FullName;
                    
                    var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(typeString));
                    var intHash = BitConverter.ToInt32(hashed, 0);
                    //Debug.Log(typeString+" :"+intHash);



                    if(!repeatableTypeCodes.ContainsKey(intHash)) { //if the hash already exists, it probably exists because the type name is something weird like "PrivateImplementationDetails" and we don't need to care about those                        
                        var couldHaveSerializableAttribute = true;
                        if (typp.Assembly.FullName.Contains("UnityEngine")) {
                            couldHaveSerializableAttribute = false;
                        }
                        else if (typp.Assembly.FullName.Contains("GOTO")) {
                            couldHaveSerializableAttribute = false;
                        }
                        else if (typp.FullName.Contains("MalbersAnimations.")) {
                            couldHaveSerializableAttribute = false;
                        }
                        else if (typp.FullName.Contains("Microsoft.Cci")) {
                            couldHaveSerializableAttribute = false;
                        }
                        else if (typp.FullName.Contains("UnityEngine.UI")) {
                            couldHaveSerializableAttribute = false;
                        }
                        else if (typp.FullName.Contains("GOTO.Logic")) {
                            couldHaveSerializableAttribute = false;
                        }
                        else if (typp.FullName.Contains("Mono.Cecil")) {
                            couldHaveSerializableAttribute = false;
                        }

                        if (couldHaveSerializableAttribute) {
                            m_classesThatCouldHaveSerializableAttribute.Add(typp);
                        }

                        repeatableTypeCodes.Add(intHash, typp);
                    }

                    numberedClassesCount++;
                }
            }
        }
        if (timer.ElapsedMilliseconds > 100) {
            Debug.Log("MTypeFinder GeneratedTargetedTypeListIfNeeded took " + timer.ElapsedMilliseconds + " ms");
        }
        
        //DebugListRepeatableTypes();
    }



    //public static string TypeNumberingFilePath => Application.dataPath + "/typeNumbers.txt";

    private static void DebugListRepeatableTypes() {
        var sb = new StringBuilder("RepeatableTypeCodes:");
        foreach (var item in repeatableTypeCodes.t1ToT2) {
            sb.AppendLine(item.Key + ":\t" + item.Value);
        }
        var str = sb.ToString();
        Debug.Log(str);
        System.IO.File.WriteAllText(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/typeListDebug" + System.DateTime.Now.ToFileTime() + ".txt", str);
    }

    public static void Prewarm() {
        GeneratedTargetedTypeListIfNeeded();
    }
}
