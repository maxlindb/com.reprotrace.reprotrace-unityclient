using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MCrashReporter
{
    public enum RunType { UnityEditor, Build }

    class Program
    {
        public static string companyName = "UNSET";
        public static string productName = "UNSET";
        public static string productNamSanitized = "UNSET";

        public static string changesetHash = "UNSET";

        public static string platform = "UNSET";

        public static string[] blob;

        public static bool hidden = false;

        public static bool editorOptOutInEffect = false;
        public static string sendablesDirectory;


        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("This app can only be run by a host game, and handles crash reporting of it if it dies");
                Console.ReadKey();
                return;
            }

            hidden = args[7] == "Hidden";
            if (hidden) {
                HideOwnConsoleWindowIfRelevant();
                Thread.Sleep(100);
                HideOwnConsoleWindowIfRelevant();
            }


            Console.WriteLine("Hello");
            PrintArgs(args);

            Console.WriteLine("MCrashReporter startup!");
            //Console.ReadKey();

            //Thread.Sleep(10000);

            var processID = args[0];
            var bgVideoFolderOrPreStart = args[1];
            var mCrashReporterActiveFlagPath = args[2];
            platform = args[3];
            //var runType = args[3] == "UnityEditor" ? RunType.UnityEditor : RunType.Build;
            var runType = platform.Contains("Editor") ? RunType.UnityEditor : RunType.Build;
            companyName = args[4];
            productName = args[5];
            changesetHash = args[6];

            MBugContentUploader.useCustomBackEnd = bool.Parse(args[8]);
            MBugContentUploader.useDropbox = bool.Parse(args[9]);
            MBugContentUploader.useDropboxOnCustombackendFail = bool.Parse(args[10]);
            MBugCustomBackEndUploader.Domain = args[11];

            productNamSanitized = args[12];

            blob = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(DecryptSomething(args[13], "76m3fmzbrb77sd3pnpde1x9jny89bsy1"))).Split("_");
            //Console.WriteLine("Token:" + MBugCustomBackEndUploader.Token);

            sendablesDirectory = args[14];


            MBugContentUploader.InitializeIfNotInitializedYet();
            










            Console.WriteLine("processID:" + processID);
            var gameProcess = System.Diagnostics.Process.GetProcessById(int.Parse(processID));


            if (bgVideoFolderOrPreStart == "prestart")
            {
                PreStartRun(gameProcess);
            }
            else
            {
                NormalRunning(gameProcess, bgVideoFolderOrPreStart, mCrashReporterActiveFlagPath, runType);
            }
        }

        private static void HideOwnConsoleWindowIfRelevant()
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                Console.WriteLine("Attempting to minimize window.");
                //Thread.Sleep(1000);
                var consoleWindow = GetConsoleWindow();
                Console.WriteLine("consoleWindow:" + consoleWindow);
                //Thread.Sleep(1000);
                if (consoleWindow != IntPtr.Zero)
                {
                    ShowWindow(consoleWindow, SW_HIDE);
                }
            }
        }

        private static void NormalRunning(Process unityAppProcess, string bgVideoCapFolder, string aliveCheckFilePath, RunType runType)
        {
            int cnt = -1;
            while (true) {
                cnt++;
                Thread.Sleep(1000);
                unityAppProcess.Refresh();

                if(hidden && (cnt + 5) % 10 == 0)
                {
                    HideOwnConsoleWindowIfRelevant();
                }

                var dead = unityAppProcess.HasExited;
                if (dead) {
                    Console.WriteLine("game process exited, deeming dead");
                }

                if(runType == RunType.UnityEditor) {
                    if (File.Exists(aliveCheckFilePath)) {
                        try {
                            var text = File.ReadAllText(aliveCheckFilePath);
                            var split = text.Split("_");
                            var numPart = split[0];
                            var data = new System.DateTime(long.Parse(numPart));
                            var delta = System.DateTime.UtcNow - data;
                            if (delta.TotalSeconds > 10f) {
                                Console.WriteLine("too old file at aliveCheckFilePath" + aliveCheckFilePath + ", deeming dead (oldness:"+delta+")");
                                File.Delete(aliveCheckFilePath);
                                dead = true;
                            }

                            var optOutVal = split[1] == bool.TrueString;
                            if(editorOptOutInEffect != optOutVal) {
                                Console.WriteLine("editorOptOutInEffect changed to " + optOutVal);
                                editorOptOutInEffect = optOutVal;
                            }
                            editorOptOutInEffect = optOutVal;
                        }
                        catch(System.Exception e) {
                            Console.WriteLine(e); //TODO: possibly allow consistent fails for only 20 seconds or so as that might mean that the app is died just after writing an empty file
                        }
                    }
                    else {
                        Console.WriteLine("nothing at aliveCheckFilePath " + aliveCheckFilePath+", deeming dead");
                        dead = true;
                    }
                }

                if (dead) {                
                    GameEndedOrCrashedAfterLoading(bgVideoCapFolder, runType);

                    if(!hidden) {
                        Console.WriteLine("Quitting soon");
                        Thread.Sleep(10000);
                    }
                    return;
                }
                Console.WriteLine("Game is still alive "+System.DateTime.Now.ToString()+" (didn't earlycrash, waiting for game exit/crash)");
            }
        }

        private static string GetRelevantLogPath(RunType runType)
        {
            Console.WriteLine("RUN TYPE:"+runType.ToString());

            if (runType == RunType.Build) return GetPlayerLogFilePath(companyName,productName);
            if (runType == RunType.UnityEditor) return GetEditorLogFilePath();
            else throw new System.Exception("unreachable");
        }

        private static void GameEndedOrCrashedAfterLoading(string bgVideoCapFolder, RunType runType)
        {
            Console.WriteLine("GameEndedOrCrashedAfterLoading " + bgVideoCapFolder);

            var logPath = GetRelevantLogPath(runType);      
            CopyAndPostLog(bgVideoCapFolder, "afterExitingPlayerLog", logPath, runType == RunType.UnityEditor);
        }

        private static void CopyAndPostLog(string bgVideoCapFolder, string logTag, string originalLogPath, bool copyOnlyLinesAfterPlaymodeEnter)
        {
            try
            {
                var theID = new DirectoryInfo(bgVideoCapFolder).Name;
                //var newPath = Path.Combine(new FileInfo(GetEditorLogFilePath()).DirectoryName, theID + "_" + logTag + ".txt");
                var newPath = Path.Combine(new FileInfo(originalLogPath).DirectoryName, theID + "_" + logTag + ".txt");

                if (copyOnlyLinesAfterPlaymodeEnter) {
                    GetTailOfLogAfterLastOccurenceOfGivenLinesAndCopyTo(originalLogPath, newPath,
                            "Initialize engine version:",
                            "Entering Playmode",
                            "Mono: successfully reloaded assembly");
                }
                else {
                    File.Copy(originalLogPath, newPath, true);
                }
            
                var dropPath = bgVideoCapFolder + "/" + new FileInfo(newPath).Name;

                if(editorOptOutInEffect) {
                    File.Copy(newPath, sendablesDirectory + "/" + new FileInfo(newPath).Name);
                }
                else {
                    if(!MBugContentUploader.UploadFileToDepository(newPath, dropPath)) {
                        File.Copy(newPath, sendablesDirectory + "/" + new FileInfo(newPath).Name);
                    }
                }                
                File.Delete(newPath);
            }
            catch(System.Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failure collecting and posting logfile after crash!" + e.ToString());
                Thread.Sleep(10000);
                Console.WriteLine("exiting in 10 seconds");
                Thread.Sleep(10000);
            }
        }


        //pre-start run is a special run of this program, that runs from the very initial startup of the game to the point where the video capture starts, which can be at start of actual gameplay/after loading.
        //pre-start run can capture if the game crashes before it is able load properly, and in that case, sends the logfile in a special way.
        private static void PreStartRun(Process unityAppProcess)
        {
            while (true) {
                Thread.Sleep(1000);
                unityAppProcess.Refresh();
                //if (unityAppProcess.ExitCode != 259) {
                if(unityAppProcess.HasExited) {
                    GameCrashedBeforeLoading();
                    return;
                }
                Console.WriteLine("Game is still alive (pre-start) " + System.DateTime.Now.ToString());
            }
        }

        private static string GetProcessToStringSafe(Process item) {
            return "\t" + GetProcName(item) + "\t" + GetProcStartTime(item);
        }

        private static string GetProcStartTime(Process item) {
            try {
                return item.StartTime.ToString();
            }
            catch (System.Exception e) {
                return e.Message.Substring(0, 6);
            }
        }

        private static string GetProcName(Process item) {
            try {
                return item.ProcessName;
            }
            catch(System.Exception e) {
                return e.Message.Substring(0, 6);
            }                
        }

        private static void GameCrashedBeforeLoading()
        {
            Console.WriteLine("GameCrashedBeforeLoading");
            var panicID = GetPanicID();            

            CopyAndPostLog(panicID, "beforeLoadedCrashPlayerLog", GetPlayerLogFilePath(companyName,productName), false);
        }


        //ID when there's no bg videocapture folder and id from that available
        private static string GetPanicID()
        {
            string machineIDExtraPostFix = "";
            var machineID = $"{RemoveWeirdChars(System.Environment.MachineName)}_{RemoveWeirdChars(System.Environment.UserName)}_" + RemoveWeirdChars(machineIDExtraPostFix);
            var appFolderName = RemoveWeirdChars(productName);
            //var machineID = $"{System.Environment.MachineName}_{System.Environment.UserName}";
            var sessionID = "SES-EARLYCRASH_" + appFolderName + "_" + System.DateTime.UtcNow.ToFileTime().ToString() + "_" + machineID +"__"+ platform + "_"+changesetHash;

            var currentTimeCompartmentDirectory = GetCurrentTimeCompartmentDirectory();


            sessionID = appFolderName + "/FullSessions/" + currentTimeCompartmentDirectory + "/" + sessionID;

            return sessionID;
        }


        public static void PrintArgs(string[] args) {            
            for (int i = 0; i < args.Length; i++) {
                Console.WriteLine("\targ" + i + ":" + args[i]);
            }
            Console.WriteLine();
        }



        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);




        public static string GetCurrentTimeCompartmentDirectory()
        {
            return GetTimeCompartmentDirectory(System.DateTime.UtcNow);
        }

        public static string GetTimeCompartmentDirectory(DateTime timestamp)
        {
            return timestamp.ToString("dd_MMMM_yyyy");
        }

        private static string RemoveWeirdChars(string inputText)
        {
            return inputText.Replace("_", "").Replace(" ", ""); //TODO make safer for filenames
        }

        public static string GetPlayerLogFilePath(string companyName, string productName) {        
            if(System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
                var appDataRoot = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).Parent.FullName;
                var finalPath = @$"{appDataRoot}\LocalLow\{companyName}\{productName}";
                return Path.Combine(finalPath, "Player.log");
            }
            else {
                //right now just assumes Mac
                return $"/Users/{System.Environment.UserName}/Library/Logs/{companyName}/{productName}/Player.log";
            }
        }
        public static string GetEditorLogFilePath() {
            if(System.Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Unity\\Editor\\Editor.log";
            }
            else {
                //right now just assumes Mac
                return $"/Users/{System.Environment.UserName}/Library/Logs/Unity/Editor.log";
            }
        }

        private static void GetTailOfLogAfterLastOccurenceOfGivenLinesAndCopyTo(string inputTextFilePath, string outputTextFilePath, params string[] afterLines)
        {
            var size = new FileInfo(inputTextFilePath).Length;
            var startPointInFile = size - 10000000;
            if (startPointInFile < 0) startPointInFile = 0;
            var fs = new FileStream(inputTextFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr = new StreamReader(fs);
            sr.BaseStream.Position = startPointInFile;
            long firstLinePoint = 0;


            int row = 0;
            int takeFromRow = 0;

            while (true)
            {
                var line = sr.ReadLine();
                if (firstLinePoint == 0) firstLinePoint = sr.BaseStream.Position;

                if (line == null)
                {
                    break;
                }

                foreach (var item in afterLines)
                {
                    if (line.Contains(item))
                    {
                        takeFromRow = row;
                        //Debug.Log(pos+" "+item+" "+ line);                    
                    }
                }
                row++;
            }
            sr.Dispose();
            fs.Dispose();


            if (File.Exists(outputTextFilePath))
                File.Delete(outputTextFilePath);


            var fs2 = new FileStream(inputTextFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var sr2 = new StreamReader(fs2);
            sr2.BaseStream.Position = startPointInFile;
            row = 0;

            var sw = new StreamWriter(outputTextFilePath);

            while (true)
            {
                var line = sr2.ReadLine();
                if (line == null)
                {
                    break;
                }

                if (row >= takeFromRow)
                {
                    sw.WriteLine(line);
                }
                row++;
            }
            sw.Dispose();
            sr2.Dispose();
            fs2.Dispose();
        }

        /// <summary>
        /// Decrypts a string
        /// </summary>
        /// <param name="CipherText">Text to be decrypted</param>
        /// <param name="Password">Password to decrypt with</param>
        /// <param name="Salt">Salt to decrypt with</param>
        /// <param name="HashAlgorithm">Can be either SHA1 or MD5</param>
        /// <param name="PasswordIterations">Number of iterations to do</param>
        /// <param name="InitialVector">Needs to be 16 ASCII characters long</param>
        /// <param name="KeySize">Can be 128, 192, or 256</param>
        /// <returns>A decrypted string</returns>
		public static string DecryptSomething(string CipherText, string Password,
			string Salt = "Kekkonen", string HashAlgorithm = "SHA1",
			int PasswordIterations = 2, string InitialVector = "OFRna73m*aze01xY",
			int KeySize = 256) {

			if (string.IsNullOrEmpty(CipherText))
				return "";

			byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(InitialVector);
			byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
			byte[] CipherTextBytes = System.Convert.FromBase64String(CipherText);
			PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
			byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
			RijndaelManaged SymmetricKey = new RijndaelManaged();
			SymmetricKey.Mode = CipherMode.CBC;
			SymmetricKey.Padding = PaddingMode.PKCS7;
			SymmetricKey.BlockSize = 128;
			SymmetricKey.KeySize = KeySize;
			byte[] PlainTextBytes = null;
			int ByteCount = 0;
			using (ICryptoTransform Decryptor = SymmetricKey.CreateDecryptor(KeyBytes, InitialVectorBytes)) {
				using (MemoryStream MemStream = new MemoryStream(CipherTextBytes)) {
					using (CryptoStream CryptoStream = new CryptoStream(MemStream, Decryptor, CryptoStreamMode.Read)) {
						using (MemoryStream PlainOut = new MemoryStream()) {
							byte[] buffer = new byte[4096];
							int read;
							while ((read = CryptoStream.Read(buffer, 0, buffer.Length)) > 0)
								PlainOut.Write(buffer, 0, read);
							PlainTextBytes = PlainOut.ToArray();
							ByteCount = PlainTextBytes.Length;
						}
						MemStream.Close();
						CryptoStream.Close();
					}
				}
			}
			SymmetricKey.Clear();
			return Encoding.UTF8.GetString(PlainTextBytes, 0, ByteCount);
		}
    }
}
