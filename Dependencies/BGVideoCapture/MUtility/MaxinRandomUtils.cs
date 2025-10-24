//#undef UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.SceneManagement;

using UnityEngine.AI;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEditor;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace MUtility
{
    public enum TimeType {Seconds, Frames, FixedTics, UnScaledSeconds}

    public class StopIenumingAndWaitFrameInstruction { }

#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	public static class MaxinRandomUtils {

#if UNITY_EDITOR
		static MaxinRandomUtils() {
			EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
		}

		private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj) {
			if (obj == PlayModeStateChange.EnteredEditMode) {
				quitting = false;
			}
		}
#endif

        public const string SENSIBLE_FILENAMECOMPATIBLE_DATETIME_STRING = "dd_MMMM_yyyy_hh.mm_tt";

		public static bool SceneOfNameIsLoaded(string thisSceneName) {
			var scene = SceneManager.GetSceneByName(thisSceneName);
			return scene.isLoaded && scene.IsValid();
		}

		public const string SENSIBLER_FILENAMECOMPATIBLE_DATETIME_STRING = "dd_MMMM_yyyy_HH.mm.ss";

		public static Rect LerpRect(Rect startRect, Rect endRect, float prog) {
			var tweenedRect = new Rect();
			tweenedRect.x = Mathf.Lerp(startRect.x, endRect.x, prog);
			tweenedRect.y = Mathf.Lerp(startRect.y, endRect.y, prog);

			tweenedRect.width = Mathf.Lerp(startRect.width, endRect.width, prog);
			tweenedRect.height = Mathf.Lerp(startRect.height, endRect.height, prog);

			return tweenedRect;
		}

		public static string NullSafeUnityObjName(UnityEngine.Object obj, string nullString = "NULL") {
			if (obj == null) return nullString;
			else return obj.name;
		}

		public static string RemoveWhitespace(string v) {
			var newStr = "";
			for (int i = 0; i < v.Length; i++) {
				if (!string.IsNullOrWhiteSpace(v[i].ToString())) {
					newStr += v[i];
				}
			}
			return newStr;
		}

		public static void SetLayerRecursively(GameObject go, int layerToSet) {
			go.layer = layerToSet;
			var children = go.GetComponentsInChildren<Transform>();
			foreach (var item in children) {
				item.gameObject.layer = layerToSet;
			}
			/*foreach (Transform child in gameObject.transform) {
                child.gameObject.layer =
            }*/
		}

        public static Vector3 GetCombinedVertMiddle(Transform trans, out bool failOnSome)
		{
			failOnSome = false;
			Dictionary<Transform, Vector3[]> vertexHolders = new Dictionary<Transform, Vector3[]>();

			var filts = trans.GetComponentsInChildren<MeshFilter>(true);
            foreach (var item in filts) {
				if (item.sharedMesh == null) continue;
				if(!item.sharedMesh.isReadable) {
					failOnSome = true;
					continue;
                }
				vertexHolders.Add(item.transform, item.sharedMesh.vertices);
            }
            /*foreach (var item in trans.GetComponentsInChildren<ProBuilderMesh>()) {
				vertexHolders.Add(item.transform, item.GetVertices().Select(x => x.position).ToArray());
            }*/


			List<Vector3> veccs = new List<Vector3>();

            foreach (var item in vertexHolders)
			{

				var verts = item.Value;
				var middle = GetMiddleOfPoints(verts);
				veccs.Add(item.Key.TransformPoint(middle));
            }

			if(veccs.Count == 0) {
				return trans.position;
            }

			return GetMiddleOfPoints(veccs.ToArray());
        }

        public static string TruncateToXRowsIfNeeded(string toTruncate, int maxRows)
		{
			var sb = new StringBuilder();
			var split = toTruncate.Split(new[] { '\n' }, System.StringSplitOptions.None);

            for (int i = 0; i < Mathf.Min(maxRows,split.Length); i++) {
				sb.AppendLine(split[i]);
				if(i == maxRows) {
					sb.AppendLine("... and " + (split.Length - maxRows) + " more rows");
					break;
                }
            }
			return sb.ToString();
        }

        public static void IgnoreCollisioBetweenRoots(Transform transform1, Transform transform2) {
			var colliders1 = transform1.GetComponentsInChildren<Collider>();
			var colliders2 = transform2.GetComponentsInChildren<Collider>();

			foreach (var collA in colliders1) {
				foreach (var collB in colliders2) {
					if (collA == collB) continue;
					if (collA.isTrigger || collB.isTrigger) continue;
					Physics.IgnoreCollision(collA, collB, true);
				}
			}
		}

		/*
#if UNITY_EDITOR
		[MenuItem("TEST/OpenNotepadWithContentTest")]
#endif
		public static void OpenNotepadWithContentTest()
		{
			OpenNotepadWithContent("TESTstrinn");
		}
		*/


		public static void OpenNotepadWithContent(string v, string persistentDataPathIfNeeded = null)
		{
			var persPath = persistentDataPathIfNeeded;
			if(persPath == null) {
				persPath = Application.persistentDataPath;
            }

			var tempPath = persPath + "/temp_notepadinspect.txt";
			File.WriteAllText(tempPath, v);
			System.Diagnostics.Process.Start("notepad.exe",tempPath);
        }

        public static string StripRichTextTags(string lineText) {
			var sb = new StringBuilder();
			//int currTagCount = 0;
			bool inTag = false;
			bool tagWasClosed = false;
            foreach (var charr in lineText) {
				if(tagWasClosed) {
					inTag = false;
					tagWasClosed = false;
                }
				if(charr == '<') {
					inTag = true;
                }
				if (charr == '>') {
					tagWasClosed = true;
				}
				if(!inTag) {
					sb.Append(charr);
                }
			}

			return sb.ToString();
        }

        public static string UnityAssetPathToAbsolutePath(string unityPath) {
		    var root = Application.dataPath;
		    root = root.Split (new []{"Assets"},System.StringSplitOptions.RemoveEmptyEntries).First ();
		    //Debug.Log (root);

		    var result = root + unityPath;

		    return result;
	    }

        public static void SetListContainsOrDoesnt<T>(T item, List<T> list, bool shouldExistInList)
		{
            if (shouldExistInList) {
				if (!list.Contains(item)) {
					list.Add(item);
				}
			}
			else {
				if (list.Contains(item)) {
					list.Remove(item);
				}
			}
        }

        public static string AbsolutePathToUnityPath (string absPath, bool appendAssets = true)
		{
			absPath = absPath[0].ToString().ToUpper()[0] + absPath.Substring(1);
			absPath = absPath.Replace('\\', '/');
			var withDeleted = absPath.Replace (Application.dataPath/*.Replace ("/", "\\")*/, "");
			if(appendAssets) withDeleted = "Assets" + withDeleted;
			//Debug.Log (withDeleted);
			return withDeleted;
		}

	    public static Vector3 CastToDirUntilHit (Vector3 startCastFromPos, Vector3 dir,float limit = 1000f)
	    {
		    RaycastHit hit;
		    if (RaycastWithVisual (startCastFromPos, dir, out hit, limit)) {
			    return hit.point;
		    }
		    //else Debug.LogWarning ("No hit wtf");
		    return startCastFromPos;
	    }

        public static char GetRotatingChar() {
            var chars = new[] { '-', '\\', '|', '/' };

            var remaind = Time.renderedFrameCount % chars.Length;

            return chars[remaind];
        }

        public static void DestroyChildren(this Transform parent) {
		    while(parent.childCount > 0) {
			    Object.DestroyImmediate(parent.GetChild(0).gameObject);
		    }
	    }

	    public static float RoundToClosest(this float inFloat, float step) {
		    var downwardsStepped = inFloat - (inFloat % step);
		    var upwardsStepped = downwardsStepped + step;

		    var downwardsDist = Mathf.Abs (inFloat-downwardsStepped);
		    var upwardsDist = Mathf.Abs (inFloat-upwardsStepped);

		    if (downwardsDist < upwardsDist) return downwardsStepped;
		    else return upwardsStepped;

	    }

        public static void RunIENumSynchronously(IEnumerator enumerator) {
            while (enumerator.MoveNext()) ;
        }

        public static string FormatStringList(IEnumerable<string> enumerable) {
            var sb = new StringBuilder();

            sb.AppendLine("List length:" + enumerable.Count());
            foreach (var item in enumerable) {
                sb.AppendLine(item);
            }
            return sb.ToString();
        }

        public static void SetGlobalScale(Transform trans, Vector3 scale) {
            var existingScale = trans.parent != null ? trans.parent.lossyScale : Vector3.one;
            scale.Scale(existingScale);        
            trans.localScale = scale;
        }

        public static void SwapValues(ref float a, ref float b) {
            var temp = a;
            a = b;
            b = temp;
        }

        public static void SetHDRPUnlitColor(Material material, Color color) {
            string colorPropName = "_UnlitColor";
            material.SetColor(colorPropName, color);
        }
		public static void SetHDRPLitColor(Material material, Color color) {
			string colorPropName = "_BaseColor";
			material.SetColor(colorPropName, color);
		}

		public static float GetNonNormalizedTime(this AnimatorStateInfo stateInfo) {
		    float time = stateInfo.normalizedTime * stateInfo.length;
		    return time;
	    }

        public static string PrintableList<T>(IEnumerable<T> items, string delimiter = "\n", int limit = 0) {
			var sb = new StringBuilder();
			var itemCount = items.Count();			
			var max = Mathf.Min(limit, itemCount);
			if (limit == 0) {
				max = itemCount;
				limit = itemCount;
			}
			var wasTruncated = itemCount > limit;

			var iener = items.GetEnumerator();
			
			for (int i = 0; i < max; i++) {
				iener.MoveNext();
				sb.Append(iener.Current.ToString());
				var left = itemCount - i;
				if (left != 1)sb.Append(delimiter);
            }
			if(wasTruncated) {
				var howManyMore = itemCount - limit;
				sb.Append("\n... (and " + howManyMore + " more)");
            }

			return sb.ToString();
        }

        public static bool GameObjectIsDontDestroyOnLoad(GameObject go)
        {
            var hasFlag = (go.hideFlags & HideFlags.DontSave) == HideFlags.DontSave;
            if (!hasFlag) {
                hasFlag = go.scene.buildIndex == -1; //idk, above DOES NOT WORK
            }
            //Debug.Log(go.name + " is DontDestroyOnLoad: " + hasFlag+" flags:"+go.hideFlags);
            return hasFlag;            
        }

        /*public static void MatchTarget (this Animator animator, Vector3 matchPosition, Quaternion matchRotation, AvatarTarget targetBodyPart, MatchTargetWeightMask weightMask, float startNormalizedTime, float targetNormalizedTime = 1f, bool keepInPos = true) {
		    animator.MatchTarget (matchPosition, matchRotation, targetBodyPart, weightMask, startNormalizedTime, targetNormalizedTime);
		    if(keepInPos)StartUndyingCoroutine (MonitorMatchTarget (animator, matchPosition));
	    }*/

        public static int lastFrameSlicedTotalMoveNexts;

        public static IEnumerator DoingTimeLimitedFrameSlicedIENumRunning(IEnumerator enumerator, int milliSecondsMax = 5)
		{
            var timer = System.Diagnostics.Stopwatch.StartNew();
            int its = 0;
            int itsThisFrame = 0;

            while(true) {
                var ran = enumerator.MoveNext();
                its++;
                itsThisFrame++;
                //Debug.Log("DoingTimeLimitedFrameSlicedIENumRunning itsThisFrame "+itsThisFrame);                

                if(timer.ElapsedMilliseconds >= milliSecondsMax || enumerator.Current is StopIenumingAndWaitFrameInstruction) {
					var over = timer.ElapsedMilliseconds - milliSecondsMax;
					if(over > 10) {
						if(Time.frameCount > 5)Debug.LogWarning("framesliced workslice went " + over + "ms over budget");
                    }

                    yield return null;                

                    itsThisFrame = 0;
                    timer.Reset();
                    timer.Start();

                    //Debug.Log("DoingTimeLimitedFrameSlicedIENumRunning new frame");
                }

				if (!ran) break;
			}
			lastFrameSlicedTotalMoveNexts = its;
            //Debug.Log("DoingTimeLimitedFrameSlicedIENumRunning is done");
        }

        /*public static void MatchTarget (this Animator animator, MatchTargetParams matchParams) {
		    animator.MatchTarget (
			    matchParams.targetAnchor.position, matchParams.targetAnchor.rotation,
			    matchParams.targetBodyPart, new MatchTargetWeightMask (matchParams.positionXYZWeight, matchParams.rotationWeight),
			    matchParams.startMatchingTime, matchParams.stopMatchingTIme);
	    }

	    static IEnumerator MonitorMatchTarget (Animator animator, Vector3 matchPosition)
	    {
		    var startState = animator.GetCurrentAnimatorStateInfo (0);
		
		    while (true) {
			    if (animator == null) yield break;
			    if (animator.isMatchingTarget) yield return null;
			    else {
				    var startPos = animator.transform.position;
				    var startRot = animator.transform.rotation;
				    while (animator.GetCurrentAnimatorStateInfo (0).fullPathHash == startState.fullPathHash) {
					    animator.transform.position = startPos;
					    animator.transform.rotation = startRot;
					    //Debug.Log ("keeping " + animator.name + " in position");
					    yield return null;
				    }
				    Debug.Log ("state ending, not keeping in pos any longer");
				    yield break;
			    }

			    yield return null;
		    }
	    }*/

        static long lastAllocMemoryForGCTest = 0;

        public static bool ArraysMatch<T>(T[] seq1, T[] seq2) where T : class
        {
            if (seq1.Length != seq2.Length) {
                Debug.Log("fail on len");
                Debug.Log("seq1:" + seq1.Aggregate("", (x, y) => x + " " + y));
                Debug.Log("seq2:" + seq2.Aggregate("", (x, y) => x + " " + y));
                return false;
            }

            for (int i = 0; i < seq1.Length; i++) {
                //if (seq1[i] != seq2[i]) return false;
                if(!EqualityComparer<T>.Default.Equals(seq1[i], seq2[i])) {
                    Debug.Log("fail on match");
                    Debug.Log("seq1:" + seq1.Aggregate("", (x, y) => x + " " + y));
                    Debug.Log("seq2:" + seq2.Aggregate("", (x, y) => x + " " + y));
                    return false;
                }
            }
            return true;
        }

        /*public static void FireWhenDone(UnityWebRequest op, Action onDone) {
            StartUndyingCoroutine(FiringWhenDone(op, onDone));
        }

        private static IEnumerator FiringWhenDone(UnityWebRequest op, Action onDone) {
            while(true) {

            }
        }*/

        static long lastStartToEndGCAmount = 0;

        public static void StartEvalGarbage() {
            lastAllocMemoryForGCTest = System.GC.GetTotalMemory(false);
        }

        public static float[,] CloneArray(float[,] heights) {
            float[,] clone = new float[heights.GetLength(0), heights.GetLength(1)];
            System.Array.Copy(heights, clone, heights.Length);
            return clone;
        }

        public static long EndEvalGarbage() {
            var allocNow = System.GC.GetTotalMemory(false);
            lastStartToEndGCAmount = allocNow - lastAllocMemoryForGCTest;

            //Debug.Log("EndEvalGarbage: " + ByteLenghtToHumanReadable(lastStartToEndGCAmount));

            return lastStartToEndGCAmount;
        }

        public static bool Contains(this string source, string toCheck, System.StringComparison comp)
	    {
		    return source.IndexOf(toCheck, comp) >= 0;
	    }

	    public static T GetComponentInParent<T>(this Component fromComp,bool includeInActive) where T : Component
	    {
		    if (!includeInActive) Debug.LogError ("asdiojfsdoijfsdjiofsdojifsdoijfsdojifsdojidfsojisdfoijojifd i didnt do this for dthis");

		    var trans = fromComp.transform;

		    T found = null;


		    while (true) {
			    found = trans.GetComponent<T> ();
			    if (found != null) return found;

			    trans = trans.parent;
			    if (trans == null) return null;
		    }
	    }

	    public static string ByteLenghtToHumanReadable(long byteLenght, bool useBits = false) {
		    string suffix = "";
		    long lenght;
		    if (useBits) lenght = byteLenght * 8;
		    else lenght = byteLenght;

            int kilo = 1024;
            int mega = 1024 * 1024;
            int giga = 1024 * 1024 * 1024;
		
		    if (lenght < kilo) {
			    if (useBits) suffix = " bits";
			    else suffix = " B";
			    return lenght + suffix;
		    }
		    else if (lenght < mega) {
			    if (useBits) suffix = " kbits";
			    else suffix = " kB";
			    return (lenght / kilo) + suffix;
		    }
		    else if(lenght < giga) {
			    if (useBits) suffix = " Mbits";
			    else suffix = " MB";
			    return System.Math.Round((float)lenght / mega, 2) + suffix;
		    }
            else {
                if (useBits) suffix = " Gbits";
                else suffix = " GB";
                return System.Math.Round((float)lenght / giga, 2) + suffix;
            }

        }

	    public static byte[] ReadStreamFully(Stream input)
	    {
		    input.Position = 0;
		    byte[] buffer = new byte[16*1024];
		    using (MemoryStream ms = new MemoryStream())
		    {
			    int read;
			    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
			    {
				    ms.Write(buffer, 0, read);
			    }
			    return ms.ToArray();
		    }
	    }

        public static Dictionary<TKey, TValue> CloneDictionary<TKey, TValue>(this Dictionary<TKey, TValue> dictToCopy) {

            var dictBeingCopied = new Dictionary<TKey, TValue>();

            foreach (var item in dictToCopy) {
                dictBeingCopied.Add(item.Key, item.Value);
            }
            return dictBeingCopied;
        }

        static System.Random randomGen = null;

	    public static long GetRandomLong() {
		    if(randomGen == null)randomGen = new System.Random();
		    return randomGen.NextInt64();
	    }

	    public static long NextInt64(this System.Random rnd)
	    {
		    var buffer = new byte[sizeof(long)];
		    rnd.NextBytes(buffer);
		    return System.BitConverter.ToInt64(buffer, 0);
	    }

	    public static StringBuilder DebugPrintDirStructure (string temporaryCachePath)
	    {
		    StringBuilder sb = new StringBuilder("Dumping folder structure below "+temporaryCachePath);

		    AddItemsToSB(new DirectoryInfo(temporaryCachePath),sb);

		    Debug.Log(sb.ToString());

		    return sb;
	    }

	    static void AddItemsToSB (DirectoryInfo directoryInfo,StringBuilder sb)
	    {
		    foreach (var item in directoryInfo.GetFileSystemInfos()) {
			    sb.Append(item.FullName);

			    try {
				    if((item.Attributes & FileAttributes.Directory) == FileAttributes.Directory) {
					    AddItemsToSB((item as DirectoryInfo),sb);
				    }
			    }
			    catch {
				    sb.Append("(blocked)");
			    }
			    sb.AppendLine();
		    }
	    }

	    public static int BoolToInt (bool b)
	    {
		    if(b)return 1;
		    else return 0;
	    }
	    public static bool IntToBool(int i) {
		    return i == 1;
	    }

	    #if UNITY_EDITOR
	    [UnityEditor.MenuItem ("Tools/Clear PlayerPrefs")]
	    public static void ClearPlayerPrefs ()
	    {
		    PlayerPrefs.DeleteAll ();
	    }

        public static List<RaycastHit> RecursiveRaycast(Vector3 origPos, Vector3 dir) {
            List<RaycastHit> hits = new List<RaycastHit>();
            Vector3 fromPoint = origPos;
            while(true) {
                if(MaxinRandomUtils.RaycastWithVisual(fromPoint, dir, out RaycastHit hit, 10000f, doVisualize:false)) {
                    fromPoint = hit.point + dir * 0.001f;
                    hits.Add(hit);
                }
                else {
                    break;
                }
            }
            return hits;
        }

        /*public static string GetCurrentAnimatorState(Animator inAnimator) {
		
		    var states = GetAnimatorStatesFromController (inAnimator);

		    var currentstateinfo = inAnimator.GetCurrentAnimatorStateInfo (0);

		    var wtf = states.Where(x => currentstateinfo.shortNameHash == x.nameHash);// cant be duplicate namess
		    //Aslog.Log (wtf.ToList()); 
		    var kers = wtf.FirstOrDefault ();
		    //if(kers != null) print (kers.name);

		    return (kers != null) ? kers.name : "NULL";
	    }

        public static List<UnityEditor.Animations.AnimatorState> GetAnimatorStatesFromController(Animator inAnimator) {
		
		    var pers = (UnityEditor.Animations.AnimatorController)inAnimator.runtimeAnimatorController;
		    List<UnityEditor.Animations.AnimatorState> states = new List<UnityEditor.Animations.AnimatorState> ();

		    foreach (var layer in pers.layers) {
			    RecursivelyAddStatesFromMachine (layer.stateMachine, states);
		    }

		    return states;
	    }

	    public static Dictionary<UnityEditor.Animations.AnimatorState,UnityEditor.Animations.ChildAnimatorState> animatorStatesToChildStatesCached = new Dictionary<UnityEditor.Animations.AnimatorState,UnityEditor.Animations.ChildAnimatorState> ();

	    static void RecursivelyAddStatesFromMachine (UnityEditor.Animations.AnimatorStateMachine machine, List<UnityEditor.Animations.AnimatorState> states)
	    {
		    foreach (var item in machine.states) {
			    states.Add (item.state);

			    animatorStatesToChildStatesCached.AddOrChangeDictionaryValue (item.state,item);
		    }

		    foreach (var item in machine.stateMachines) {
			    RecursivelyAddStatesFromMachine (item.stateMachine,states);
		    }
	    }*/


        [UnityEditor.MenuItem("Helper/Hideflags/Get for selected object")]
        public static void PrintHideFlagsForSelectedObject() {
            var selection = UnityEditor.Selection.activeObject;
            PrintHideFlags(selection);
        }

        [UnityEditor.MenuItem("Helper/Hideflags/Get for selected Gameobject")]
        public static void PrintHideFlagsForSelectedGameObject() {
            var selection = UnityEditor.Selection.activeGameObject;
            PrintHideFlags(selection);
        }

        private static void PrintHideFlags(UnityEngine.Object selection) {
            var flags = selection.hideFlags;
            Debug.Log("HideFlags for" + selection + ":" + flags);
        }


        [UnityEditor.MenuItem ("Tools/Throw thing from scenecam _&T")]
	    public static void ThrowThingFromSceneCam() {
		    Transform shootAnchorTrans = Camera.current.transform;

		    if (shootAnchorTrans == null) shootAnchorTrans = GameObject.Find ("SceneCamera").transform;

		    var go = GameObject.CreatePrimitive (PrimitiveType.Sphere);
		    go.transform.position = shootAnchorTrans.position;
		    var rigid = go.AddComponent<Rigidbody> ();
		    go.transform.localScale = Vector3.one * 0.35f;

#if UNITY_6000_0_OR_NEWER
			rigid.linearVelocity = shootAnchorTrans.forward * 6f;
#else
            rigid.velocity = shootAnchorTrans.forward * 6f;
#endif

            //go.AddComponent<DebugLogPhysicsEvents> ().doLog = false;

        }

	    [UnityEditor.MenuItem ("Tools/GroupSelectionUnderNewParent")]
	    public static void GroupSelectionUnderNewParent() {		

		    var allSelected = UnityEditor.Selection.gameObjects;

		    var newGO = new GameObject ("Grouping");
		    newGO.transform.parent = allSelected.First ().transform.parent;

		    newGO.transform.position = allSelected.First ().transform.position;

		    foreach (var item in allSelected) {
			    item.transform.parent = newGO.transform;
		    }
	    }

	    [UnityEditor.MenuItem ("Tools/StickToWallByRotation")]
	    public static void StickToWallByRotation() {
		    var toStick = UnityEditor.Selection.transforms;

		    float standOffDistance = 0.01f;
		
		    foreach (var item in toStick) {
			    var ray = new Ray (item.position, item.forward);
			    RaycastHit hit;
			    if (RaycastWithVisual (ray, out hit, 1000f)) {
				    var rot = Quaternion.LookRotation (-hit.normal);
				    UnityEditor.Undo.RecordObject(item,item.name+": stick to wall");
				    item.rotation = rot;
				    item.position = hit.point + hit.normal * standOffDistance;
			    }
			    else Debug.Log (item + " didn't hit anything towards it's forwards axis", item);
		    }
	    }

	    [UnityEditor.MenuItem ("Tools/PrintPrefabOverrides")]
	    public static void PrintPrefabOverrides() {
		    var mods = UnityEditor.PrefabUtility.GetPropertyModifications (UnityEditor.Selection.activeGameObject);
		    foreach (var item in mods) {		
			    /*GameObject go;
			    var component = item.objectReference as Component;
			    if (component == null) go = item.objectReference as GameObject;*/
			    Transform trans = null;
			    if (item.target is GameObject) {
				    //Debug.Log (item.objectReference.name item.propertyPath);
				    trans = (item.target as GameObject).transform;
			    }
			    else if (item.target is Component) {
				    trans = (item.target as Component).transform;
			    }

			    Debug.Log (MaxinRandomUtils.GetHieararchyVerbose(trans)+": "+item.propertyPath+" "+item.target,trans.gameObject);
		    }
	    }

        [UnityEditor.MenuItem("Tools/Output scene hierarchy of selection")]
        public static void OutputSceneHieararchyOfSelection() {

            var scene = UnityEditor.Selection.activeGameObject.scene;
            var sb = new StringBuilder("SCENE HIEARARCHY OF "+scene.name+":\n");

            /*var gos = Object.FindObjectsOfType<GameObject>();

            foreach (var item in gos) {
                if(item.scene == scene) {
                    sb.AppendLine(item.GetHieararchyPath(true));
                }
            }*/

            var roots = scene.GetRootGameObjects();

            foreach (var item in roots) {
                AppendScenePathsToSBRecursive(item.transform, sb);
            }


            var str = sb.ToString();
            Debug.Log(str);

            File.WriteAllText(Application.persistentDataPath+"/scenehieararchy_" + scene.name + ".txt", str);
        }

        private static void AppendScenePathsToSBRecursive(Transform inTrans, StringBuilder sb) {
            sb.AppendLine(inTrans.GetHieararchyPath(false));

            foreach (Transform item in inTrans) {
                AppendScenePathsToSBRecursive(item, sb);
            }
        }

#endif

		public static GameObject LeaveMarker (Vector3 inPos, string goname, Quaternion rot = default(Quaternion), bool makeVisible = true, Color color = default(Color), bool leaveCallStackInfo = true)
	    {
#if !UNITY_EDITOR
			return null;
#endif

			//return;
			var parent = new GameObject();
		    parent.transform.position = inPos;
		    parent.transform.rotation = rot;
            //int tiks = Application.isPlaying ? GOTO.Utilities.PhysicsTicksCounter.Ticks : -1;
            parent.name = "x[f"+ Time.frameCount +"]"+ goname;

		    var markerChild = GameObject.CreatePrimitive (PrimitiveType.Cube);
		    var coll = markerChild.GetComponent<Collider> ();
		    coll.enabled = false;            

		    if (Application.isPlaying) Object.Destroy (coll);
		    else Object.DestroyImmediate (coll);

            if (!makeVisible) markerChild.GetComponent<Renderer>().enabled = false;

            markerChild.transform.localScale = Vector3.one * 0.1f;
			if (Application.isPlaying) {
				markerChild.GetComponent<Renderer>().material.color = color;
				SetHDRPLitColor(markerChild.GetComponent<Renderer>().material, color);
			}

		    markerChild.transform.parent = parent.transform;
		    markerChild.transform.localPosition = Vector3.zero;
		    markerChild.transform.localRotation = Quaternion.identity;
		    //markerChild.hideFlags = HideFlags.HideInHierarchy;

		    /*if (leaveCallStackInfo) {
			    var stringer = parent.AddComponent<PlaceHolderDialogComment> (); //lol
			    var callstack = new System.Diagnostics.StackTrace (true);
			    stringer.comment = callstack.ToString ();
		    }*/

		    return parent;
	    }
	
	    public static GameObject LeaveMarker(Transform inTrans,string goname = "", bool makeVisible = true, Color color = default(Color)) {
		    return LeaveMarker(inTrans.position,goname == "" ? inTrans.name : goname,inTrans.rotation, makeVisible, color);
	    }


	    public static string GetHieararchyPath(this UnityEngine.Object inObj, bool includeSceneName = false, bool includeCOmponentIndex = false) {
		    if (inObj == null) return "NULL (scenepath get fail)";
            var endStr = "";
            if (includeCOmponentIndex) {
                endStr = " componentIndex:" + System.Array.IndexOf((inObj as Component).GetComponents<MonoBehaviour>(), inObj);
            }
		    if (inObj is GameObject) return GetHieararchyVerbose ((inObj as GameObject).transform,includeSceneName);
		    if (inObj is Component) return GetHieararchyVerbose ((inObj as Component).transform,includeSceneName) + endStr;
		    else throw new System.NotSupportedException ();
	    }

	    public static string GetHieararchyVerbose(Transform inTrans, bool includeSceneName = false) {
		    if (inTrans == null) return "NULL";

		    var sb = new StringBuilder ();

            if (includeSceneName) {
                if (!inTrans.gameObject.scene.IsValid()) sb.Append("(prefab?)");
                sb.Append(inTrans.gameObject.scene.name + "//");
            }

		    var parents = new List<Transform> ();

		    var tempTrans = inTrans;
		    while (true) {
			    parents.Add (tempTrans);
			    tempTrans = tempTrans.parent;
			    if (tempTrans == null)break;
		    }

		    parents.Reverse ();

		    foreach (var item in parents) {
			    sb.AppendFormat ("/{0}",item.name);
		    }
		    return sb.ToString ();

	    }

#pragma warning disable CS0618
	    #if UNITY_EDITOR
	    [UnityEditor.MenuItem ("Tools/GetPathInsidePrefabFromSelectionGOComponent")]
	    public static void GetPathInsidePrefabFromSelectionGOComponent() {
		    Debug.Log (GetPathInsidePrefab (UnityEditor.Selection.activeGameObject.GetComponent<Component>()));
	    }

	    [UnityEditor.MenuItem ("Tools/GetPathInsidePrefabFromSelectionGO")]
	    public static void GetPathInsidePrefabFromSelectionGO() {
		    Debug.Log (GetPathInsidePrefab (UnityEditor.Selection.activeGameObject));
	    }

	    public static string GetPathInsidePrefab(this UnityEngine.Object obj) {
		    Transform start = null;
		    if(obj is GameObject)start = ((GameObject)obj).transform;
		    if(obj is Component)start = ((Component)obj).transform;


		    var parents = new List<Transform> ();

		    Transform iterating = start;
		    while(true) {
			    parents.Add(iterating);
			    iterating = iterating.parent;

			    if(iterating == null)break;
			    if(UnityEditor.PrefabUtility.GetPrefabObject(iterating) == null)break;
		    }

		    var sb = new StringBuilder ();
		    parents.Reverse();
		    if(parents.Count > 0) parents.RemoveAt (0);

		    foreach (var item in parents) {
			    sb.AppendFormat ("/{0}",item.name);
		    }
		    return sb.ToString ();
	    }
	    #endif
#pragma warning restore CS0618

	    public static bool LayerMaskHasLayer(int layermask,int layer) {
		    //return (layermask & (1<<layer));
		    //return (layermask | (1 << layer));

		    /*if (((layermask >> layer)  1) == 1) {
			    return true;
		    }
		    else return false;*/

		    if (layermask == (layermask | (1 << layer))) {
			    return true;
		    }
		    else return false;
	    }

	    public static int CountLayerMaskLayers(int mask) {
		    int cnt = 0;
		    for (int i = 0; i < 32; i++) {
			    if(LayerMaskHasLayer(mask,i))cnt++;
		    }
		    return cnt;
	    }

	    public static bool IsAnyChildOf (this Transform child, Transform requireBeChildOf, bool acceptSelfToo = true)
	    {
            if(acceptSelfToo) {
                if (child == requireBeChildOf) return true;
            }

		    Transform checkTrans = child;

		    while (checkTrans != null) {
			    if (checkTrans.parent == requireBeChildOf) return true;
			    checkTrans = checkTrans.parent;
		    }
		    return false;
	    }

        public static string RemoveNumbersFromString(string inString) {
            for (int i = 0; i < 10; i++) {
				inString = inString.Replace(i.ToString(), "");
            }
			return inString;
        }

        public static void SnapToGround (Transform inTransform)
	    {
		    RaycastHit hit;
		    if(Physics.Raycast(inTransform.position+Vector3.up,Vector3.down,out hit,10000f,LayerMask.GetMask("Default"),QueryTriggerInteraction.Ignore)) {
			    inTransform.position = hit.point;
		    }
		    else Debug.LogWarning(GetHieararchyVerbose(inTransform)+": snap failed",inTransform);
	    }

	    public static Vector3 GetForwardVector(Quaternion inQuat) {

		    return inQuat * Vector3.forward;

    //		var go = new GameObject ("jiosa");
    //		go.transform.rotation = inQuat;
    //		return go.transform.forward;

    //		return new Vector3( 2 * (inQuat.x * inQuat.z + inQuat.w * inQuat.y), 
    //			2 * (inQuat.y * inQuat.x - inQuat.w * inQuat.x),
    //			1 - 2 * (inQuat.x * inQuat.x + inQuat.y * inQuat.y));	
	    }


        public static IEnumerator TweenValueAndIterate (float from, float to, float fullTime, System.Action<float> callback,AnimationCurve curve = null, bool forceUnscaled = false, float maxDelta = 0.2f)
	    {
		    //haks
		    if(curve == null)curve = AnimationCurve.EaseInOut(0f,0f,1f,1f);

		    float timer = 0f;
            while (true) {
                var delta = (Time.timeScale < 0.001f || forceUnscaled) ? Time.unscaledDeltaTime : Time.deltaTime;                

                if (delta > maxDelta) {
                    //Debug.Log((delta * 1000).ToString("0") + "ms hang when running TweenValueAndIterate. Your shit might look laggy. "+ callback.Method+" (CLAMPING; REMOVE THIS IF BEAKS)");
                    delta = Mathf.Min(0.033f, maxDelta);
                }
                timer += delta;

                float prog = timer / fullTime;

                bool doStop = false;
                if (prog > 1f) {
                    doStop = true;
                    prog = 1f;
                }

                if (float.IsNaN(prog))
                    prog = 0f; //happens at startup for whatever reason

                float val;
                if (curve != null) {
                    //Debug.Log("prog:" + prog);
                    val = Mathf.Lerp(from, to, curve.Evaluate(prog));
                }
                else val = Mathf.Lerp(from, to, prog);

                callback(val);

                if (doStop) break;
                yield return null;
            }
        }

	    //public static DummyScript undyingCoroutineSharedBeh = null;
	    public static Transform undyingRunnersParent;

        /*public static void StartUndyingCoroutine(IEnumerator ie) {		
		    if (undyingCoroutineSharedBeh == null) {
			    var tempgo = new GameObject ("undyingCoroutineSharedGO");		
			    undyingCoroutineSharedBeh = tempgo.AddComponent<DummyScript> ();
		    }
		
		    undyingCoroutineSharedBeh.StartCoroutine(undyingCoroutineSharedBeh.RunCoroutineAndDie (ie));
	    }*/

        static DummyScript destroyOnLoadGOForCoRoutines = null;
        static DummyScript dontDestroyOnLoadGOForCoRoutines = null;

        static bool registeredToApplicationQuit = false;
        static bool quitting = false;

        static void DetectQuit()
        {
            quitting = true;
        }

        static DummyScript GetTempGOForUndyingCoRoutines(bool dontDestroyOnLoad) {

            if (dontDestroyOnLoad) {
                if (dontDestroyOnLoadGOForCoRoutines == null) dontDestroyOnLoadGOForCoRoutines = new GameObject("dontDestroyOnLoadGOForCoRoutines").AddComponent<DummyScript>();
                Object.DontDestroyOnLoad(dontDestroyOnLoadGOForCoRoutines.gameObject);
                return dontDestroyOnLoadGOForCoRoutines;
            }
            else {
                if (destroyOnLoadGOForCoRoutines == null) destroyOnLoadGOForCoRoutines = new GameObject("destroyOnLoadGOForCoRoutines").AddComponent<DummyScript>();

                if (undyingRunnersParent == null) undyingRunnersParent = new GameObject("UndyingRunners").transform;
                if (!dontDestroyOnLoad) undyingRunnersParent.transform.parent = undyingRunnersParent;

                return destroyOnLoadGOForCoRoutines;
            }
        }

        public static void StartUndyingCoroutine(IEnumerator ie, bool dontDestroyOnLoad = false, System.Action onDone = null,System.Action<System.Exception> onException = null, int dontSkipFramesWhenDelayUnder = 0) {
			if (!StartCoroutineStartCheck(ie)) return;
            var tempComp = GetTempGOForUndyingCoRoutines(dontDestroyOnLoad);
            tempComp.StartCoroutine(tempComp.RunCoroutineAndDie (ie, onDone, onException, dontSkipFramesWhenDelayUnder));
	    }

        private static bool StartCoroutineStartCheck(IEnumerator ie) {
            if (!Application.isPlaying) {
                Debug.LogError("Trying to do StartUndyingCoroutine while in editor mode, aborting " + ie.ToString());
                return false;
            }

            if (quitting) {
                Debug.LogError("Tried to run StartUndyingCoroutine while application quitting");
                return false;
            }
            RegisterToApplicationQuitIfNeeded();
            return true;
        }

        private static void RegisterToApplicationQuitIfNeeded() {
            if (!registeredToApplicationQuit) {
                Application.quitting += DetectQuit;
                registeredToApplicationQuit = true;
            }
        }

        public static IEnumerator RunningUndyingCoroutine(IEnumerator ie, bool dontDestroyOnLoad = false, System.Action onDone = null, System.Action<System.Exception> onException = null, int dontSkipFramesWhenDelayUnder = 0) {
			if (!StartCoroutineStartCheck(ie)) yield break;
			var tempComp = GetTempGOForUndyingCoRoutines(dontDestroyOnLoad);
			yield return tempComp.StartCoroutine(tempComp.RunCoroutineAndDie(ie, onDone, onException, dontSkipFramesWhenDelayUnder));
		}

	    public static float ClampAngle(float angle) {
		    if (angle >= 360) angle -= 360;
		    return angle;
	    }

	    public static float DistanceWithoutY (Vector3 posA, Vector3 posB)
	    {
		    return Vector3.Distance(posA,new Vector3(posB.x,posA.y,posB.z));
	    }

	    public static Vector2 ClampVectorToNormalizedPerAxis(Vector2 vec) {
		    return new Vector2 (Mathf.Clamp01 (vec.x), Mathf.Clamp01 (vec.y));
	    }

	    public static bool TransformsMatch(Transform transA, Transform transB, bool tolerant = false) {
		    var posDiff = transA.position - transB.position;
		    var rotDiff = Quaternion.Angle (transA.rotation, transB.rotation);

		    //Aslog.Log ("Posdiff:" + posDiff.magnitude + " rotdiff:" + rotDiff);

		    bool rotMatch;
		    bool posMatch;

		    if (!tolerant) {
			    posMatch = posDiff.magnitude < 0.001f;
			    rotMatch = rotDiff < 0.01f;
		    }
		    else {
			    posMatch = posDiff.magnitude < 0.15f;
			    rotMatch = rotDiff < 5f;
		    }

		    return posMatch && rotMatch;
	    }

	    public static List<T> ClearNulls <T>(List<T> list)
	    {
		    if (list == null) return null;
		    list = list.Where (x => x != null).ToList();
		    return list;
	    }

	    public static IEnumerator TweeningToTransform (Transform toTween, Transform inTransform,float speed = 1f)
	    {

		    var lastPos = Vector3.zero;
		    while (true) {		

			    if (lastPos.sqrMagnitude > 1f) toTween.position = lastPos;

			    toTween.position = Vector3.MoveTowards (toTween.position, inTransform.position, Time.fixedDeltaTime * 0.5f * speed);
			    toTween.rotation = Quaternion.RotateTowards (toTween.rotation, inTransform.rotation, Time.fixedDeltaTime * 360);

			    if (MaxinRandomUtils.TransformsMatch (toTween, inTransform)) break;
			    lastPos = toTween.position;

			    yield return null;
		    }
	    }

	    public static RectTransform InstantiateCopyOfUIThing(this RectTransform original) {
	
		    var copy = Object.Instantiate<GameObject> (original.gameObject);
		    copy.transform.SetParent (original.parent,true);
		    copy.transform.SetAsLastSibling ();
		    copy.transform.position = original.position;
		    copy.transform.localScale = original.localScale;
		    copy.transform.rotation = original.rotation;

		    var copiedrekt = copy.transform as RectTransform;

		    copiedrekt.anchorMin = original.anchorMin;
		    copiedrekt.anchorMax = original.anchorMax;

		    copiedrekt.offsetMin = original.offsetMin;
		    copiedrekt.offsetMax = original.offsetMax;

		    return copiedrekt;
	    }

	    public static void SetRectRadius(RectTransform toSet, float radius) {
		    toSet.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, radius);
		    toSet.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, radius);
	    }

	    public static bool GetIsInsideScaledBox(Vector3 checkedPos, Transform box) {
		    var clamped = GetClosestPointInsideScaledBox (checkedPos, box.position, box.rotation, box.lossyScale);
		    if (Vector3.SqrMagnitude (checkedPos-clamped) > 0.001f) return false;
		    else return true;
	    }

	    public static Vector3 GetClosestPointInsideScaledBox(Vector3 checkedPos, Transform box) {
		    return GetClosestPointInsideScaledBox (checkedPos, box.position, box.rotation, box.lossyScale);
	    }

	    public static Transform checkedPosTransTempGO;
	    public static Transform clampBoxTransTempGO;

	    public static Vector3 GetClosestPointInsideScaledBox(Vector3 checkedPos, Vector3 boxPos, Quaternion boxRot, Vector3 boxScale) {
	
		    if (checkedPosTransTempGO == null) checkedPosTransTempGO = new GameObject ("checkedPosTransTempGO").transform;
		    if (clampBoxTransTempGO == null) clampBoxTransTempGO = new GameObject ("checkerBoxTransTempGO").transform;			

		    clampBoxTransTempGO.position = boxPos;
		    clampBoxTransTempGO.rotation = boxRot;
		    clampBoxTransTempGO.localScale = boxScale;


		    checkedPosTransTempGO.parent = clampBoxTransTempGO;
		    checkedPosTransTempGO.position = checkedPos;

		    var origRot = clampBoxTransTempGO.rotation;
		    clampBoxTransTempGO.rotation = Quaternion.identity;

		    //var boxcoll = clampBoxTransTempGO.GetOrAddComponent<BoxCollider> ();

		    Bounds checkBounds = new Bounds (boxPos, boxScale);
		    var clampedToBounds = checkBounds.ClosestPoint (checkedPosTransTempGO.position);

		    checkedPosTransTempGO.position = clampedToBounds;

		    clampBoxTransTempGO.rotation = origRot;
		    return checkedPosTransTempGO.position;
	    }

	    public static Vector3 GetRandomPositionInScaledBox(this Transform inTrans) {
		    var vec = new Vector3 (GetRandomAxisLocalPos (), GetRandomAxisLocalPos (), GetRandomAxisLocalPos ());
		    var worldPos = inTrans.TransformPoint (vec);
		    return worldPos;
	    }

	    public static float GetRandomAxisLocalPos() {
		    return Random.value - 0.5f;
	    }

	    public static void DoActionAfterFrames (System.Action action, float timeUnits, TimeType timeType, bool dontDestroyOnLoad = false) {
		    StartUndyingCoroutine (DoingActionAfterFrames (action, timeUnits, timeType), dontDestroyOnLoad);
	    }

		public static void DoOnEndOfFrame(System.Action action)
		{
			StartUndyingCoroutine(DoingActionAtEndOfFrame(action));
        }

        private static IEnumerator DoingActionAtEndOfFrame(System.Action action)
		{
			yield return new WaitForEndOfFrame(); //TODO can we cache this to avoid gerboga
			action();
        }

        static IEnumerator DoingActionAfterFrames (System.Action action, float timeUnits, TimeType timeType) {
		    if (timeType == TimeType.Seconds) {
			    yield return new WaitForSeconds (timeUnits);
		    }
		    else if (timeType == TimeType.UnScaledSeconds) {
			    yield return new WaitForSecondsRealtime (timeUnits);
		    }
		    else {
			    for (int i = 0; i < (int)timeUnits; i++) {
				    if (timeType == TimeType.Frames) yield return null;
				    else if (timeType == TimeType.FixedTics) yield return new WaitForFixedUpdate ();
			    }
		    }
		    action ();
	    }

	    public static void DestroyAfterFrames (GameObject go, int i)
	    {
		    StartUndyingCoroutine(DestroyingAfterFrames(go,i));
	    }

	    static IEnumerator DestroyingAfterFrames (GameObject go, int framesAmount)
	    {
		    for (int i = 0; i < framesAmount; i++) {
			    yield return new WaitForFixedUpdate ();
		    }
		    Object.Destroy (go);
	    }

	    public static bool RaycastWithVisual (Ray ray, out RaycastHit hit, float maxDist, int layerMask = int.MaxValue, QueryTriggerInteraction ignore = QueryTriggerInteraction.Ignore,bool doVisualize = true) {
		    return MaxinRandomUtils.RaycastWithVisual (ray.origin, ray.direction, out hit, maxDist, layerMask, ignore, doVisualize);
	    }

	    public static bool RaycastWithVisual (Vector3 position, Vector3 direction, out RaycastHit hit, float maxDist, int layerMask = int.MaxValue, QueryTriggerInteraction ignore = QueryTriggerInteraction.Ignore,bool doVisualize = true, bool drawSparkles = true, float sparkleSizMod = 1f, bool superDebug = false) {
	
		    bool didHit = Physics.Raycast(position,direction,out hit,maxDist,layerMask,ignore);

            #if UNITY_EDITOR
            //if (GizmoUtility.gizmosDisabled) doVisualize = false;

            //doVisualize = false;
			
		    if (doVisualize && Application.isEditor) {			
			    bool drawLines = true;
			    bool addArrowNotchesToLines = true;
			    bool drawHitSparkles = true;

                //for perf
                //drawHitSparkles = false;
                drawHitSparkles = addArrowNotchesToLines = drawSparkles;
                addArrowNotchesToLines = false;


			    var hitDist = didHit ? hit.distance : maxDist;

			    hitDist = Mathf.Clamp (hitDist, 0f, 10000f); //dont crash pls
			
			    if(drawLines)
					Debug.DrawRay(position,direction * hitDist,didHit ? Color.green : Color.red);				

			    if (addArrowNotchesToLines) {
				    float arrowFrequency = 0.35f;
				    float arrowSize = 0.1f;

				    int arrows = Mathf.FloorToInt (hitDist / arrowFrequency);
				    arrows = Mathf.Clamp (arrows, 0, 1000);

				    for (int i = 0; i < arrows; i++) {
					    float posOnLine = i * arrowFrequency + Mathf.Repeat (Time.time, arrowFrequency);
					    var pos = position + (direction * posOnLine);
					    float animDegrees = Time.time * 180f;
					    Debug.DrawRay (pos, (Quaternion.Euler (-45, 0f, -45) * -direction) * arrowSize, Color.white);
					    Debug.DrawRay (pos, (Quaternion.Euler (45, 0f, 45) * -direction) * arrowSize, Color.white);
				    }
			    }

			    if (didHit) {
				    //DebugDraw.Sphere (hit.point, 0.2f, Color.green, "raycasthit:" + hit.collider,0.01f);
				
				    if(drawHitSparkles) DrawLineSparkles (hit.point, sizeMod: sparkleSizMod);

                    //super expensive
                    //TODO add call stack too if needed
                    if(superDebug) {
                        var marker = LeaveMarker(hit.point, "rayhit", Quaternion.LookRotation(hit.normal));
                        marker.transform.localScale = Vector3.one * 0.02f;
                        marker.AddComponent<CommentWithRef>().referredObject = hit.collider;                    
                        MaxinRandomUtils.DoActionAfterFrames(() => Object.Destroy(marker), 1f, TimeType.Frames);
                    }
			    }
		    }

            /*onDrawingGizmos.Register(() => {
				    Gizmos.DrawMesh(PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.
			    }*/

            #endif

            return didHit;
	    }

	    #if UNITY_EDITOR
	    public static void DrawNotchedHandlesLine(Vector3 posA, Vector3 posB) {
		
		    UnityEditor.Handles.DrawLine(posA,posB/*,Color.white*/);

		    float arrowFrequency = 25f;
		    float arrowSize = 10f;

		    var direction = (posB - posA).normalized;

		    int arrows = Mathf.FloorToInt (Vector3.Distance(posA,posB) / arrowFrequency);
		    for (int i = 0; i < arrows; i++) {
			    float posOnLine = i * arrowFrequency + Mathf.Repeat (Time.time, arrowFrequency);
			    var pos = posA + (direction * posOnLine);
			    float animDegrees = Time.time * 180f;
			    UnityEditor.Handles.DrawLine (pos, pos + ((Quaternion.Euler (-45, 0f, -45) * -direction) * arrowSize)/*, Color.white*/);
			    UnityEditor.Handles.DrawLine (pos, pos + ((Quaternion.Euler (45, 0f, 45) * -direction) * arrowSize)/*, Color.white*/);
		    }

	    }
	    

        static Camera sceneCam;

		//static System.DateTime lastSceneViewDrawTime;
		static int lastSceneViewDrawFrame = -100;
		static bool hasRegisteredToSceneView = false;

		private static void SceneView_duringSceneGui(SceneView obj) {
			//lastSceneViewDrawTime = System.DateTime.Now;
			lastSceneViewDrawFrame = Time.frameCount;
		}
		#endif

		public static bool IsSceneViewProbablyVisible {
			get {
				#if UNITY_EDITOR				
				if(!hasRegisteredToSceneView) {
					hasRegisteredToSceneView = true;
					SceneView.duringSceneGui += SceneView_duringSceneGui;
					return false; //dunno
				}
				//return lastSceneViewDrawTime  SceneView.
				return (Time.frameCount - lastSceneViewDrawFrame) < 2;
				#else
				return false;
				#endif
			}
        }

		public static void DrawLineSparkles(Vector3 inPos,float time = -1f, float sizeMod = 1f) {            
			#if UNITY_EDITOR
		    int sparkleCount = 30;
		    float minSparkleSize = 0.01f;
		    float maxSparkleSize = 0.05f;

            if(sceneCam == null) {
                if(Camera.current != null) {
                    sceneCam = Camera.current;
                }
            }            

			if(!IsSceneViewProbablyVisible) {
				//Debug.Log("nope's's");
				return;
            }			
        
            if(sceneCam != null) {
                var modmod = 1f;
                var dist = Vector3.Distance(inPos, sceneCam.transform.position);
                modmod = Mathf.InverseLerp(0f, 2f, dist);
                sizeMod *= modmod;
            }

            var sparkleSize = Random.Range(minSparkleSize, maxSparkleSize) * sizeMod;

            for (int i = 0; i < sparkleCount; i++) {
			    if(time < 0) Debug.DrawRay (inPos, Random.onUnitSphere * sparkleSize, Color.yellow);
			    else Debug.DrawRay (inPos, Random.onUnitSphere * sparkleSize, Color.yellow,time);
		    }
			#endif
	    }

        public static bool dontDestryOnLoadSceneGotten = false;
        public static Scene dontDestroyOnLoadScene;
	
	    public static List<T> FindComponentsOfType<T>(Scene onlySceneOfIndex = default(Scene)) {
            List<GameObject> rootGOs = new List<GameObject> ();


		    var scenesCount = SceneManager.sceneCount;
		    for (int i = 0; i < scenesCount; i++) {				

			    var scene = SceneManager.GetSceneAt (i);

                if (onlySceneOfIndex.IsValid() && scene != onlySceneOfIndex)
                    continue;

                AddGosFromSceneToList(scene,rootGOs);
		    }

            if(Application.isPlaying) { 
                if(!dontDestryOnLoadSceneGotten) {
                    var tempGO = new GameObject("tempDontDestroyONLoadGO");
                    Object.DontDestroyOnLoad(tempGO);
                    dontDestroyOnLoadScene = tempGO.scene;
                    Object.DestroyImmediate(tempGO);
                }
                AddGosFromSceneToList(dontDestroyOnLoadScene, rootGOs);
            }


            List<T> components = new List<T> ();
		    foreach (var item in rootGOs) {
			    var those = item.GetComponentsInChildren<T> (true);
			    components.AddRange (those);

			    //Aslog.Log (those.Select(x => x != null ? (x as Component).name : "NULL").ToArray());
			    /*if(those.Any(x => x == null)) {
				    Debug.Log (" u fokin wot m8"); //this happens and is retarded, why are there nulls wtf
			    }*/
		    }

            //Debug.Log("foudn " + components.Count + " of type " + typeof(T));

            return components;
	    }

        private static void AddGosFromSceneToList(Scene scene, List<GameObject> rootGOs) {        

            if (!scene.isLoaded && !Application.isPlaying) {
                Debug.LogError(scene.name + ":!scene.isLoaded");
                return;
            }

            if (!scene.IsValid()) {
                Debug.LogError(scene.name+":!scene.IsValid()");
            }
        
            rootGOs.AddRange(scene.GetRootGameObjects());        
        }

        public static void SetGlobalStackTraceLogType(StackTraceLogType inType) {
            Debug.Log("SetGlobalStackTraceLogType " + inType);
            Application.SetStackTraceLogType(LogType.Log, inType);
            Application.SetStackTraceLogType(LogType.Warning, inType);
            Application.SetStackTraceLogType(LogType.Error, inType);
        }


    #if UNITY_EDITOR
        public static int AdvanceUntil(string fullText, string expected, int startIndex,bool goForwards = true) {
		    int countRight = 0;
		    for (int i = startIndex; i < fullText.Length; ) {
			    if (fullText [i] == expected [countRight]) {
				    countRight++;
				    if (countRight == expected.Length) return i;
			    }
			    else countRight = 0;

			    if (goForwards) i++;
			    else i--;
		    }
		    return -1;
	    }

	    public static int FindMatchingBrace(string fullText,int startBraceIndex) {
		    int braceIndex = 0;
		    for (int i = startBraceIndex; i < fullText.Length; i++) {
			    if (fullText[i] == '{') braceIndex++;
			    if (fullText [i] == '}') braceIndex--;

			    if (braceIndex == 0) return i;
		    }
		    return -1;
	    }


	    [UnityEditor.MenuItem ("Debug/Print asset GUID of selection")]
	    public static void PrintAssetGUIDOfSelection() {
            var selection = UnityEditor.Selection.activeObject;

            PrintAssetGUID(selection);
        }

        private static void PrintAssetGUID(Object selection) {
            var path = UnityEditor.AssetDatabase.GetAssetPath(selection);
            var assetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            Debug.Log(assetGuid + " is asset GUID of " + path);
            UnityEditor.EditorGUIUtility.PingObject(UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object)));

            UnityEditor.EditorGUIUtility.systemCopyBuffer = selection.name.PadRight(20) + " guid:" + assetGuid.PadRight(20) + " path:" + path;
        }

        [UnityEditor.MenuItem("CONTEXT/Component/PrintAssetGUID")]
		public static void PrintGUID(MenuCommand comm)
		{
			PrintAssetGUID(comm.context);
		}

		class MisMatchingAssetGuidPair {
		    public string assetPath;
		    public string unityGUID;
		    public string metafileGUID;
	    }

	    [UnityEditor.MenuItem ("Debug/DebugAllAssets")]
	    public static void DebugAllAssetGUIDMisMatches() {
		
		    var allAssets = UnityEditor.AssetDatabase.FindAssets ("*");
		    Debug.Log ("asset count un-distinct:" + allAssets.Length);

		    var allPaths = allAssets.Select (x => UnityEditor.AssetDatabase.GUIDToAssetPath (x));
		    allPaths = allPaths.Distinct ();
		    allAssets = allPaths.Select(x => UnityEditor.AssetDatabase.AssetPathToGUID(x)).ToArray();
		    Debug.Log ("asset count distinct:" + allAssets.Length);

		    //UnityEditor.AssetDatabase.ism

		    var sb = new StringBuilder ();

		    var misMatchesList = new List<MisMatchingAssetGuidPair> ();

		    var currentGuid = "";

		    float prog = 0;
		    //System.DateTime lastPrinted = default(System.DateTime);

		    int iterationCount = 2000;
		    iterationCount = allAssets.Length;

		    for (int i = 0; i < iterationCount; i++) {
			    currentGuid = allAssets [i];

			    try {

				    var path = UnityEditor.AssetDatabase.GUIDToAssetPath (currentGuid);
				    var metaPath = UnityEditor.AssetDatabase.GetTextMetaFilePathFromAssetPath (path);
				    //Debug.Log (metaPath);

				    var absolutePath = MaxinRandomUtils.UnityAssetPathToAbsolutePath (metaPath);

				    //Debug.Log ("abs path:" + absolutePath);

				    var metaFileContents = System.IO.File.ReadAllText (absolutePath);

				    //Debug.Log (metaFileContents);

				    var guidStartIndex = AdvanceUntil (metaFileContents, "guid: ", 0);
				    var guidReadFromMetaFile = metaFileContents.Substring (guidStartIndex+1, 32);

				    //Debug.Log ("guidReadFromMetaFile:" + guidReadFromMetaFile);

				    if (currentGuid != guidReadFromMetaFile) {
					    misMatchesList.Add(new MisMatchingAssetGuidPair{assetPath = path, unityGUID = currentGuid, metafileGUID = guidReadFromMetaFile});

					    //sb.AppendLine ("Mismatching guid for"+path+" assetdb guid:" + currentGuid + " meta file guid:" + guidReadFromMetaFile);
				    }

				    if (i % 50 == 0) {
					    prog = ((float)i / (float)iterationCount);
					    //Debug.Log (prog);
					    var str = (prog * 100) + " %";
					    bool shouldCancel = UnityEditor.EditorUtility.DisplayCancelableProgressBar ("Scanning", str +/* "\nlast:" + absolutePath +"\n"+ */string.Format ("  {0}/{1}", i, iterationCount), (float)prog);
					    if (shouldCancel) {
						    UnityEditor.EditorUtility.ClearProgressBar ();
						    Debug.LogWarning ("Cancelled");
						    return;
					    }
				    }

			    }
			    catch(System.Exception e) {
				    Debug.LogError ("Scanning exception, not stopping though "+e);
			    }

		    }
		    UnityEditor.EditorUtility.ClearProgressBar ();


		    Debug.Log ("Mismatches count:" + misMatchesList.Count);
		    foreach (var misMatch in misMatchesList) {
			    Debug.LogWarning ("Mismatching guid for" + misMatch.assetPath + "\nassetdb guid:" + misMatch.unityGUID + "\t meta file guid:" + misMatch.metafileGUID);

			    try {
				    var misMatchAssetLibrary = UnityEditor.AssetDatabase.LoadAssetAtPath (UnityEditor.AssetDatabase.GUIDToAssetPath (misMatch.unityGUID),typeof(UnityEngine.Object));
				    Debug.LogWarning ("\tlibrary GUID target:" +misMatchAssetLibrary+" "+ UnityEditor.AssetDatabase.GUIDToAssetPath (misMatch.unityGUID)+"\n\n", misMatchAssetLibrary);
				    //Debug.Assert(UnityEditor.AssetDatabase.IsMainAsset(misMatchAssetLibrary));
			    }
			    catch {	}

			    try {
				    var misMatchAssetMetaFile = UnityEditor.AssetDatabase.LoadAssetAtPath (UnityEditor.AssetDatabase.GUIDToAssetPath (misMatch.metafileGUID),typeof(UnityEngine.Object));
				    Debug.LogWarning ("\tmetafile GUID target:" +misMatchAssetMetaFile+" "+ UnityEditor.AssetDatabase.GUIDToAssetPath (misMatch.metafileGUID)+"\n\n", misMatchAssetMetaFile);
			    }
			    catch {	}

			    Debug.LogWarning("\n\n\nI'M TAKING SPACE YEY");
		    }


		    Debug.Log (sb.ToString ());
	    }


	    [UnityEditor.MenuItem ("Debug/Print_m_LocalIdentifierInFile")]	
	    public static void PrintSelectionLocalIdentifierInFile() {
		    PropertyInfo inspectorModeInfo =
			    typeof(UnityEditor.SerializedObject).GetProperty ("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);

		    var gameobject = UnityEditor.Selection.activeGameObject;

		    UnityEditor.SerializedObject serializedObject = new UnityEditor.SerializedObject (gameobject);
		    inspectorModeInfo.SetValue (serializedObject, UnityEditor.InspectorMode.Debug, null);

		    UnityEditor.SerializedProperty localIdProp = serializedObject.FindProperty ("m_LocalIdentfierInFile");   //note the misspelling!

		    int localId = localIdProp.intValue;

		    Debug.Log (gameobject.name + "'s m_LocalIdentfierInFile is " + localId);
	    }


    #endif

        public static Thread SafeExecInNewThread(System.Action inAction) {

            var t = new Thread(() => {
                var timer = System.Diagnostics.Stopwatch.StartNew();
                try {
                    inAction();
                }
                catch (System.Exception e) {
                    Debug.LogError("SafeExecInNewThread crash for " + inAction + ":\n" + e);
                }
                //unity crashes this because .Target.ToString() is main thead violation. Pls
                //if(timer.ElapsedMilliseconds > 1f) Debug.Log("SafeExecInNewThread " + inAction.Target +" "+ inAction + " done in " + timer.ElapsedMilliseconds + "ms"); 
                if (timer.ElapsedMilliseconds > 1f) Debug.Log("SafeExecInNewThread " /* + inAction.Target*/ + " " + inAction + " done in " + timer.ElapsedMilliseconds + "ms");
            });

            t.Start();
            return t;
        }

        public static void DoThingInThreadAfterRealMilliSeconds(System.Action toDo, int afterMillis) {
            var t = new System.Threading.Thread(() => WaitAndDoThingInThread(toDo, afterMillis));
            t.Name = "DoThingInThreadAfterRealMilliSeconds";
            t.Start();
        }

        public static void WaitAndDoThingInThread(System.Action toDo, int afterMillis) {
            System.Threading.Thread.Sleep(afterMillis);
            try {
                toDo();
            }
            catch (System.Exception e) {
                Debug.LogError("WaitAndDoThingInThread crash:" + e);
            }
        }

    #if UNITY_EDITOR
	    [UnityEditor.MenuItem ("Tools/Ping selection")]	
	    public static void PingSelection() {
		    UnityEditor.EditorGUIUtility.PingObject (UnityEditor.Selection.activeGameObject);
	    }

	    [UnityEditor.MenuItem ("Debug/Print scenepath of selection")]	
	    public static void PrintSelectedHierarchyVerbose() {
		
		    //var selection = UnityEditor.Selection.activeTransform;
		    var selection = UnityEditor.Selection.activeGameObject.transform;

		    var str = GetHieararchyVerbose (selection, true);
		    Debug.Log (str);
	    }
    #endif

	    public static float CalcCurrentActorHeight(Animator animator) {
		    var feetPos = animator.GetBoneTransform (HumanBodyBones.LeftFoot).transform.position;
		    var headPos = animator.GetBoneTransform (HumanBodyBones.Head).transform.position;

		    float guess1 = headPos.y - feetPos.y;

		    float guess2 = animator.humanScale * 1.66f;

		    return guess2;
	    }

	    public static string LayerMaskVerbose(int layermask) {

		    var layerCount = CountLayerMaskLayers (layermask);
		    bool invert = layerCount > 6;
			
		    StringBuilder sb = new StringBuilder ();
		    if (invert) sb.Append ("!not!:\t");
		    else sb.Append ("#has#:\t");

		    for (int i = 0; i < 32; i++) {
			    if(!invert ? LayerMaskHasLayer(layermask,i) : !LayerMaskHasLayer(layermask,i)) {
				    sb.Append (LayerMask.LayerToName (i)+" ");
			    }
		    }
		
		    return sb.ToString ();
	    }

	    public static bool CheckIfHasDontDestroyOnLoad(GameObject go) {		
		    if ( (go.hideFlags & HideFlags.DontSave) == HideFlags.DontSave) return true;
		    else return false;
	    }	

	    public static void DrawOddLine (Vector3 position, Vector3 position2)
	    {

            if (!Application.isEditor) return;

            var len = Vector3.Distance (position, position2);

		    DrawLineSparkles (position);
		    DrawLineSparkles (position2);
		
		    var offsets = new List<Vector3> ();
		    for (int i = 0; i < 20; i++) {
			    offsets.Add (Random.onUnitSphere*0.1f);
		    }

		    foreach (var item in offsets) {
			    Debug.DrawLine (position + item, position2 + item);
		    }
	    }

	    public static void DrawFat2DLine (Vector3 pointA, Vector3 pointB, float rot = 90f)
	    {
    #if UNITY_EDITOR
		    float thickness = 5f;

		    //var verts = new Vector3[]{pointA,pointB, (pointB+Vector3.down*5f), (pointA+Vector3.down*5f) };

		    //better
		    var pointWardsNormalized = (pointB - pointA).normalized;
		    //var crossedVec = new Vector3(pointWardsNormalized.y,pointWardsNormalized.x);
		    var crossedVec = Quaternion.Euler(0f,0f,rot) * pointWardsNormalized;

		    var verts = new Vector3[]{pointA -= crossedVec*thickness,pointB -= crossedVec*thickness, pointB += crossedVec*thickness, pointA += crossedVec*thickness};

		    UnityEditor.Handles.DrawSolidRectangleWithOutline (verts, Color.red, Color.white);
    #endif
	    }

    #if UNITY_EDITOR
	    [UnityEditor.MenuItem("Debug/PrintHieararchy")]
    #endif
	    public static void PrintHieararchy() {
		    var transforms = MaxinRandomUtils.FindComponentsOfType<Transform> (UnityEngine.SceneManagement.SceneManager.GetActiveScene());

		    var sb = new StringBuilder ();

		    foreach (var item in transforms) {
			    //sb.AppendLine (item.name + " hierarchyCount:" + item.hierarchyCount + " hierarchyCapacity:" + item.hierarchyCapacity);
			    var deepness = 0;
			    var tempTrans = item;
			    while (true) {
				    tempTrans = tempTrans.parent;
				    if (tempTrans != null) deepness++;
				    else break;
			    }
			    //sb.Append (new string ('-', deepness));
			    sb.Append (new string ('\t', deepness));
			    sb.Append (item.name);
			    sb.AppendLine ();
		    }
		    Debug.Log (sb.ToString ());
	    }

    #if UNITY_EDITOR
	    [UnityEditor.MenuItem("Debug/PrintComponents")]
    #endif
	    public static void PrintComponents() {
		    var comps = MaxinRandomUtils.FindComponentsOfType<Component> ();

		    var sb = new StringBuilder ();

		    var typedGroups = comps.GroupBy (x => x.GetType ());

		    foreach (var item in typedGroups) {
			    sb.AppendLine (item.Count () + " " + item.Key + " 's:");

			    foreach (var itme in item) {
				    sb.AppendLine("\t"+itme.GetHieararchyPath(true));
			    }
			    sb.AppendLine();
			    sb.AppendLine();
		    }

		    Debug.Log (sb.ToString ());
	    }	

    #if UNITY_EDITOR
	    [UnityEditor.MenuItem("Debug/CompareHierarchies")]
	    public static void CompareHieararchies() {
		    var root1 = UnityEditor.Selection.gameObjects [0];
		    var root2 = UnityEditor.Selection.gameObjects [1];

		    var allChilds1 = root1.GetComponentsInChildren<Transform> ().ToList ();
		    var allChilds2 = root2.GetComponentsInChildren<Transform> ().ToList ();

		    Debug.Log ("root1:" + allChilds1.Count + " root2:" + allChilds2.Count);

		    var intBoth = new List<Transform> ();
		    intBoth.AddRange (allChilds1.Where (x => allChilds2.SingleOrDefault (k => k.name == x.name)));
		    intBoth.AddRange (allChilds2.Where (x => allChilds1.SingleOrDefault (k => k.name == x.name)));

		    Debug.Log ("inboth");

		    for (int i = 0; i < allChilds1.Count; i++) {
			    var root1obj = allChilds1.ElementAtOrDefault (i);
			    var root2obj = allChilds2.ElementAtOrDefault (i);
			    Debug.Log (i+" root1obj:"+root1obj+ "root2obj:"+root2obj);

			    if (Vector3.Distance (root1obj.position, root2obj.position) > 0.001f) Debug.Log ("distance diff");
		    }
	    }

	    [UnityEditor.MenuItem("Debug/ClearProgressBar")]
	    public static void ClearProgressBar() {
		    UnityEditor.EditorUtility.ClearProgressBar ();
	    }

    #endif

	    public static float GetDistanceToClosestPointInCollider(Collider item, Vector3 inPos) {
		    var closestPoint = GetClosestPointInCollider (item,inPos);
		    return Vector3.Distance (inPos, closestPoint);
	    }

	    public static Vector3 GetClosestPointInCollider (Collider item, Vector3 inPos)
	    {
		    Vector3 calculatedPoint = Vector3.zero;

		    if (item is BoxCollider) {
			    var asBox = item as BoxCollider;
			    calculatedPoint = GetClosestPointInsideScaledBox (inPos, item.transform);
		    }
		    else if (item is SphereCollider) {
			    var asSphere = item as SphereCollider;
			    var magn = asSphere.radius * item.transform.localScale.x;

			    bool isInside = Vector3.Distance (inPos, item.transform.position) <= magn;
			    if (isInside) calculatedPoint = inPos;
			    else {
				    var collToTargetVec = inPos - item.transform.position;
				    calculatedPoint = item.transform.position + (collToTargetVec.normalized * magn);
			    }
		    }
		    else throw new System.Exception ("GetClosestPointInCollider: " + item.GetType () + " not supported, only box and sphere "+GetHieararchyVerbose(item.transform));

		    return calculatedPoint;
	    }

	    public static HumanBodyBones GetBoneType(Transform inTrans, Animator inAnim) {

		    if (inTrans == null) {
			    Debug.LogWarning ("GetBoneType: Tried to get bonetype for null");
			    return (HumanBodyBones)(-1);
		    }

		    for (int i = 0; i < (int)HumanBodyBones.LastBone; i++) {			
			    if (inAnim.GetBoneTransform ((HumanBodyBones)i) == inTrans) return (HumanBodyBones)i;
		    }

		    Debug.LogWarning ("GetBoneType: can't find bonetype for "+MaxinRandomUtils.GetHieararchyVerbose(inTrans));
		    return (HumanBodyBones)(-1);
	    }

	    public static float GetWrappedDistance (float num, float num2, float wrapVal)
	    {
		    var normalDist = Mathf.Abs(num-num2);

		    var smaller = Mathf.Min(num,num2);
		    var larger = Mathf.Max(num,num2);
		    //var wrappedDist = Mathf.Abs(num - (num2-wrapVal) );
		    var wrappedDist = Mathf.Abs(smaller - (larger-(wrapVal)) );

		    //wrappedDist -= 1;
		    //normalDist -= 1;

		    float returnVal;
		    if(normalDist < wrappedDist)returnVal = normalDist;
		    else returnVal = wrappedDist;

		    //Debug.Log("GetWrappedDist("+num+","+num2+","+wrapVal+"): "+returnVal);

		    return returnVal;
	    }

	    public static Transform GetTransFromGoOrComp(this UnityEngine.Object inObj) {
		    var asGo = inObj as GameObject;
		    if (asGo != null) return asGo.transform;

		    var asComp = inObj as Component;
		    if (asComp != null) return asGo.transform;

		    throw new System.NotSupportedException ();
	    }

	    public static string GetRelativeTransformPath (Transform fromParent, Transform toChild, bool includeGivenParent = false)
	    {
		    Transform trans = toChild;

		    string str = "";
		    while (true) {
				var stop = (trans == fromParent);
				if(stop && !includeGivenParent) break;

			    str = trans.name + str;

			    trans = trans.parent;

			    if (trans != fromParent || includeGivenParent) str = "/" + str;

				if (stop) break;

			}
		    return str;
	    }

	    public static float GetSeconds(this System.DateTime inDateTime) {
		    return (float)(inDateTime - System.DateTime.MinValue).TotalSeconds;
	    }

	    public static float GetSecondsFrom2017(this System.DateTime inDateTime) {
		    return (float)(inDateTime - new System.DateTime(2017,1,1)).TotalSeconds;
	    }

	    public static bool IsGenericList(this object o)
	    {
		    var oType = o.GetType();
		    return (oType.IsGenericType && (oType.GetGenericTypeDefinition() == typeof(List<>)));
	    }

	    public static void LogHuge (string str)
	    {
		    Debug.Log ("<size=25>"+str+"</size>");
	    }

	    public static float CalcPathLen (UnityEngine.AI.NavMeshPath navMeshPath, bool viz = false)
	    {
		    if (navMeshPath == null) return float.MaxValue;

		    if(Application.isEditor && viz) VisualizeNavMeshPath (navMeshPath);

		    var corners = navMeshPath.corners;
		    float len = 0f;
		    for (int i = 1; i < corners.Length; i++) {
			    len += Vector3.Distance (corners [i - 1], corners [i]);
		    }
		    return len;
	    }

	    public static UnityEngine.AI.NavMeshPath FindPathSimple (Vector3 fromPos, Vector3 position2, bool ifIncompleteReturnNull)
	    {
		    fromPos = ClampPosToNavMesh (fromPos, true);

		    var tempPath = new UnityEngine.AI.NavMeshPath();
		    var couldCalcPath = UnityEngine.AI.NavMesh.CalculatePath (fromPos, position2, int.MaxValue, tempPath);

		    if (!couldCalcPath) return null;
		    if (ifIncompleteReturnNull) {
			    if (tempPath.status != UnityEngine.AI.NavMeshPathStatus.PathComplete) return null;
			    if (Vector3.Distance (tempPath.corners.Last(), position2) > 0.1f) {
				    Debug.Log ("path says it's complete but doesn't terminate at destination, what");
				    return null;
			    }
		    }

		    return tempPath;
	    }

        public static bool LastClampToNavMeshSuccessful = false;
        /// <summary>
        /// BEVARY CAN RETURN IN POS EVEN IF INVALID
        /// Clamps the position to nav mesh.
        /// </summary>
        /// <returns>The position to nav mesh.</returns>
        /// <param name="inPos">In position.</param>
        /// <param name="clampToGround">If set to <c>true</c> clamps to ground OTHERWISE DOESNT ALTER Y AXIS </param>
        public static Vector3 ClampPosToNavMesh(Vector3 inPos, bool clampToGround = false, bool throwExIfFailed = false, bool logError = false)
		{
            if (float.IsInfinity(inPos.x)) {				
                Debug.LogError("trying to clamp to navmesh infinity!");
                if (throwExIfFailed) new System.ArgumentException();
            }

            UnityEngine.AI.NavMeshHit hit;

            bool shortDidHit = UnityEngine.AI.NavMesh.SamplePosition(inPos, out hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas);

            if (!shortDidHit) {
                bool longDidHit = UnityEngine.AI.NavMesh.SamplePosition(inPos, out hit, 100f, UnityEngine.AI.NavMesh.AllAreas);
                if (!longDidHit) {
					if (logError) {
						Debug.LogError("can't clamp pos " + inPos);
                    }
                    if (throwExIfFailed) new System.Exception("unable to clamp");
                    LastClampToNavMeshSuccessful = false;
                    return inPos;
                }
            }



            Vector3 hitPos;
            if (!clampToGround && hit.position.y < inPos.y) {
                hitPos = new Vector3(hit.position.x, inPos.y, hit.position.z);
                //if (inPos.y < hit.position.y) hitPos.y = hit.position.y;
            }
            else hitPos = hit.position;


            //CHECK IF INFINITE
            if (float.IsInfinity(hitPos.x)) {
                Debug.LogError("clamped navmesh pos to infinity, unity pls (just returning original)");
                if (throwExIfFailed) new System.Exception("infinity");
                LastClampToNavMeshSuccessful = false;
                if (Application.isPlaying) {
                    MaxinRandomUtils.LeaveMarker(hitPos, " tried to navmesh clamp this position but result was infinity");
                }
                return inPos;
            }

            //var hitPosWithyDisregarded = new Vector3 (hit.position.x, inPos.y, hit.position.z);
            //		var dist = Vector3.Distance (inPos, hitPos);
            //
            //		if (dist > 2f) {
            ////			Debug.LogWarning ("navmesh clamped dist:" + dist);
            //			//MaxinRandomUtils.LeaveMarker (inPos,/* name + */" agentpos");
            //			//MaxinRandomUtils.LeaveMarker (hit.position,/* name + */" clampedpos");
            //		}

            LastClampToNavMeshSuccessful = true;
            return hitPos;
        }


        public static void VisualizeNavMeshPath(UnityEngine.AI.NavMeshPath inPath, float time = -1f, bool fat = false) {
            if (!Application.isEditor) return;

            Color lineColor = Color.black;

            switch (inPath.status) {
                case UnityEngine.AI.NavMeshPathStatus.PathComplete:
                    lineColor = Color.green;
                    break;
                case UnityEngine.AI.NavMeshPathStatus.PathPartial:
                    lineColor = Color.yellow;
                    break;
                case UnityEngine.AI.NavMeshPathStatus.PathInvalid:
                    lineColor = Color.red;
                    break;
            }

            lineColor = Color.Lerp(lineColor, Color.black, Mathf.PingPong(MaxinRandomUtils.SomeTime * 5f, 1f));

            for (int i = 0; i < inPath.corners.Length - 1; i++) {
                var offset = Vector3.up * 0.02f;
                if (time > 0) {
                    Debug.DrawLine(inPath.corners[i] + offset, inPath.corners[i + 1] + offset, lineColor, time);

                }
                else {
                    Debug.DrawLine(inPath.corners[i] + offset, inPath.corners[i + 1] + offset, lineColor/*, 0.5f*/);
                }

                if (fat)
                    MaxinRandomUtils.DrawOddLine(inPath.corners[i] + offset, inPath.corners[i + 1] + offset);

            }

            /*for (int i = 0; i < inPath.corners.Length; i++) {
			    MaxinRandomUtils.LeaveMarker(inPath.corners [i],FixedTicksCounter.tics+" NAVPATH:"+i);
		    }*/
        }

        public static bool PausedInAnyWay {
		    get {
    #if UNITY_EDITOR
			    if(UnityEditor.EditorApplication.isPaused)return true;
    #endif
			    if (Time.timeScale < 0.001f) return true;

			    return false;
		    }
	    }

	    public static string GetRandomString (int length)
	    {
		    System.Random random = new System.Random ();
		    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" /*0123456789"*/;
		    return new string (Enumerable.Repeat (chars, length)
			    .Select (s => s [random.Next (s.Length)]).ToArray ());
	    }

    	public static NavMeshPath CalculatePath(Vector3 fromPos,Vector3 toPos, NavMeshPath suppliedPath = null)
		{
    		if (suppliedPath == null) suppliedPath = new NavMeshPath ();
    
    		var couldCalcPath = NavMesh.CalculatePath(fromPos, toPos, NavMesh.AllAreas, suppliedPath);
    
    		return suppliedPath;
    	}

	    public static float lastStartedErrorSoundTime = -100f;

        #if UNITY_EDITOR

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false) {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            method.Invoke(
                null,
                new object[] { clip, startSample, loop }
            );
        }

        public static void StopAllClips() {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] { },
                null
            );

            Debug.Log(method);
            method.Invoke(
                null,
                new object[] { }
            );
        }



        [UnityEditor.MenuItem("Helper/LockReloadAssemblies")]
        static void LockReloadAssemblies() {
            UnityEditor.EditorApplication.LockReloadAssemblies();
        }
        [UnityEditor.MenuItem("Helper/UnlockReloadAssemblies")]
        static void UnlockReloadAssemblies() {
            UnityEditor.EditorApplication.UnlockReloadAssemblies();
        }



        #endif

        public static float SomeTime {
		    get {
                #if UNITY_EDITOR
			    if(!Application.isPlaying)return (float)UnityEditor.EditorApplication.timeSinceStartup;
                #endif

			    return Time.realtimeSinceStartup;
		    }
	    }

        public static bool IsQuitting {
			get {
				if(!Application.isPlaying) {
					return false;
                }
				if (!registeredToApplicationQuit) {
					RegisterToApplicationQuitIfNeeded();					
				}
				return quitting;
            }
        }

        public static string GetSafeFileName(string name, char replace = '_', string extraBadChars = "") {
		    char[] invalids = Path.GetInvalidFileNameChars();
            var invalidslist = invalids.ToList();
			invalidslist.AddRange(extraBadChars.ToCharArray());
            invalidslist.Add(':'); //not invalid on linux but makes reading these on windows pain
            invalids = invalidslist.ToArray();
		    return new string(name.Select(c => invalids.Contains(c) ? replace : c).ToArray());
	    }

        public static void WrappedBuildSafeUndoRecord(UnityEngine.Object target, string explanation = "") {

            if (explanation == "") explanation = "unlabeled UndoRecord to "+target.NullSafeToString();

            #if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(target, explanation);
            #endif
        }

        static System.Reflection.PropertyInfo m_hScrollingFieldInfo = null;

        //reflection because this returns the exact result needed but isn't exposed because no good reason
        public static  bool GetHasEnoughContentToBeScrollableHorizontally(this ScrollRect inScrollRect) {
        
            if (m_hScrollingFieldInfo == null) {
                m_hScrollingFieldInfo = (typeof(ScrollRect)).GetProperty("hScrollingNeeded",BindingFlags.Instance | BindingFlags.NonPublic);
            }

            var hScrollingNeeded = (bool)m_hScrollingFieldInfo.GetValue(inScrollRect, null);
            return hScrollingNeeded;
        }

        static System.Reflection.PropertyInfo m_vScrollingFieldInfo = null;

        public static bool GetHasEnoughContentToBeScrollableVertically(this ScrollRect inScrollRect) {

            if (m_vScrollingFieldInfo == null) {
                m_vScrollingFieldInfo = (typeof(ScrollRect)).GetProperty("vScrollingNeeded", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            var vScrollingNeeded = (bool)m_vScrollingFieldInfo.GetValue(inScrollRect, null);
            return vScrollingNeeded;
        }


        public static string GetFileNameWithoutExtension(this string inString) {

            if(!inString.Contains(".")) {
                return inString;
            }

            var extensionPart = inString.Split('.').Last();

            var withoutExtension = inString.Substring(0, inString.Length - (extensionPart.Length+1) );

            return withoutExtension;
        }



        [System.Diagnostics.DebuggerStepThrough]
        public static void ThrowWin32Exception() {
            int code = Marshal.GetLastWin32Error();
            if (code != 0) {
                throw new System.ComponentModel.Win32Exception(code);
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

        public static void CreateDirectoryLongPathSupprting(string path) {
            var thruLongPath = MaxinRandomUtils.GetWin32LongPath(path);
            new DirectoryInfo(thruLongPath).Create();
        }


        public static void WriteAllTextLongPathSupporting(string path, string contents, bool doWin32Stuff = true) {

            string filePath = "";

            if (doWin32Stuff) {
                filePath = MaxinRandomUtils.GetWin32LongPath(path);
            }
            else {
                filePath = path; //RRMEMBER
            }       
        

            var filInf = new FileInfo(filePath);
            filInf.Directory.Create();
            var writer = filInf.AppendText();
            writer.Write(contents);
            writer.Flush();
            writer.Dispose();
        }

		/*public static void CopyDirectory(string from, string to) {
			//Now Create all of the directories
			foreach (string dirPath in Directory.GetDirectories(from, "*",
				SearchOption.AllDirectories))
				Directory.CreateDirectory(dirPath.Replace(from, to));

			//Copy all the files & Replaces any files with the same name
			foreach (string newPath in Directory.GetFiles(from, "*.*",
				SearchOption.AllDirectories))
				File.Copy(newPath, newPath.Replace(from, to), true);
		}*/

		public static string FixPathChars(this string str)
		{
			var removed = "";
			if(str.Length > 2 && str[1] == ':') {
				removed = str.Substring(0, 3);
				str = str.Substring(3);
            }
			str = str.Replace('\\', '/');
			str = removed + str;
			return str;
		}

		public static string GetRelativePath(this string path, string basePath)
		{
			if (!path.StartsWith(basePath)) throw new System.Exception("GetRelativePath: "+ path +" does not start with the base path !! basePath:"+ basePath);
			path = path.Substring(basePath.Length);
			path = path.TrimStart('\\');
			return path;
        }

		public static void CopyFolder(string from, string to, bool deletePreviousContentWhileReplacing = false, bool avoidRewrites = false, System.Func<string, bool> pathFilter = null, System.Func<string, bool> deletionFilter = null)
		{
			var copyVerbose = "CopyFolder " + new DirectoryInfo(from).Name + " => " + new DirectoryInfo(to).Name;
			Debug.Log(GetTCOpenBlockString(copyVerbose));

			//try to clean these up from weird chars
			from = new DirectoryInfo(from).FullName/*.FixPathChars()*/;
			to = new DirectoryInfo(to).FullName/*.FixPathChars()*/;

			Debug.Log("CopyFolder called:\nFROM:\t" + from + "\nTO:\t" + to);

			var timer = IntervalTimer.Start("CopyFolder " + new DirectoryInfo(from).Name + " => " + new DirectoryInfo(to).Name);

			var newPaths = new DirectoryInfo(from).GetFiles("*",SearchOption.AllDirectories);
			if(pathFilter != null) {
				newPaths = newPaths.Where(x => pathFilter(x.FullName)).ToArray();
            }
			var newShortPaths = newPaths.Select(x => x.FullName.GetRelativePath(from)).ToList();

			Directory.CreateDirectory(to);
			var oldPaths = new DirectoryInfo(to).GetFiles("*", SearchOption.AllDirectories);
			var oldShortPaths = oldPaths.Select(x => x.FullName.GetRelativePath(to)).ToList();


			Debug.Log(GetTCOpenBlockString("debugCrap"));
			//if(oldShortPaths.Count > 0) {
			Debug.Log(from);
				/*Debug.Log(newShortPaths[0]);
				var comb = Path.Combine(from, newShortPaths[0]);
				Debug.Log(comb);*/
			//}


			Debug.Log("BIGDEBUG:\nnewpaths:\n" + PrintableList(newPaths) + "\n\n\nnewShortPaths:\n" + PrintableList(newShortPaths) + "\n\n\noldPaths:\n" + PrintableList(oldPaths) + "\n\n\noldShortPaths:\n" + PrintableList(oldShortPaths));
			Debug.Log(GetTCCloseBlockString("debugCrap"));


			var newFiles = newShortPaths.Where(x => !oldShortPaths.Contains(x)).ToList();
			var removedFiles = oldShortPaths.Where(x => !newShortPaths.Contains(x)).ToList();
			var stayedFiles = newShortPaths.Where(x => oldShortPaths.Contains(x)).ToList();

			if(deletionFilter != null) {
				removedFiles = removedFiles.Where(x => deletionFilter(x)).ToList();
            }

			timer.Interval("EvaluateInitialChanges",true);


			Debug.Log(GetTCOpenBlockString("hash"));

			List<string> shortPathsChanged = null;
			if (avoidRewrites) {
				shortPathsChanged = GetWhichFilesHaveChanged(stayedFiles, from, to);
			}
			else {
				shortPathsChanged = stayedFiles.ToList();
			}

			var shortPathsChangedHashSet = new HashSet<string>();
			foreach (var item in shortPathsChanged) {
				shortPathsChangedHashSet.Add(item);
			}

			timer.Interval("DoHashChecks",true);
			Debug.Log(GetTCCloseBlockString("hash"));

			var report = "";
			report += new string('#', 100);
			report += "\n" + newFiles.Count + " files added";
			report += "\n" + removedFiles.Count + " files removed";
			report += "\n" + stayedFiles.Count + " files stayed";
			report += " (of which " + shortPathsChanged.Count + " have been modified)";
			report += "\n" + new string('#', 100);
			Debug.Log(report);

			Debug.Log(GetTCOpenBlockString("remove+add+overwrite"));
			var verbs = GetFileMassVerbose(removedFiles.Select(x => Path.Combine(to,x)).ToList());			
			foreach (var item in removedFiles) {
				var toDelFull = Path.Combine(to, item);
				if (deletePreviousContentWhileReplacing) {
					Debug.Log("Removing removed file:" + toDelFull);
					File.Delete(toDelFull);
				}
                else {
					Debug.Log("Skipping removing removed file (deletePreviousContentWhileReplacing off):" + toDelFull);
				}
			}
			timer.Interval("RemoveOldFiles " + verbs, true);

			foreach (var item in newFiles) {
				Debug.Log("Copying new file:"+item);
				GetFromAndToPaths(item, from, to, out var itemFrom, out var itemTo);
				CopyFileEnsureHavePath(itemFrom, itemTo);
			}
			timer.Interval("CopyNewFiles "+ GetFileMassVerbose(newFiles.Select(x => Path.Combine(from,x)).ToList()),true);

			foreach (var item in shortPathsChanged) {
				Debug.Log("Overwrite-copying modified file:" + item);
				GetFromAndToPaths(item, from, to, out var itemFrom, out var itemTo);
				CopyFileEnsureHavePath(itemFrom, itemTo);
			}
			timer.Interval("OverwriteCopyModifiedFiles " + GetFileMassVerbose(shortPathsChanged.Select(x => Path.Combine(from,x)).ToList()), true);
			Debug.Log(GetTCCloseBlockString("remove+add+overwrite"));

			timer.Stop();

			Debug.Log(GetTCCloseBlockString(copyVerbose));
		}

        private static string GetFileMassVerbose(List<string> fileList)
		{
			var size = fileList.Select(x => GetSize(x)).Sum();
			var str = fileList.Count+" files, "+ByteLenghtToHumanReadable(size);
			return str;
        }

        private static long GetSize(string x)
		{
			try {
				return new FileInfo(x).Length;
            }
			catch(System.Exception e) {
				Debug.LogWarning("fail in GetSize:" + e+"\nfor:"+x);
				return -1;
            }
		}

        private static void CopyFileEnsureHavePath(string itemFrom, string itemTo)
		{
			new FileInfo(itemTo).Directory.Create();
			File.Copy(itemFrom, itemTo, true);
        }

        private static void GetFromAndToPaths(string shortPath, string from, string to, out string itemFrom, out string itemTo)
		{
			itemFrom = Path.Combine(from, shortPath);
			itemTo = Path.Combine(to, shortPath);

			//Debug.Log("GetFromAndToPaths shortPath " + shortPath + "\nfrom:\t" + from + "\nto:\t" + to + "\nitemFrom:\t" + itemFrom + "\nitemTo:\t" + itemTo);
		}

        static int processes = 0;



        private static void LogProcessing(List<string> shortPaths, ConcurrentDictionary<string, bool> haveChangedStates) {
            Debug.Log("Checking hashes, " + haveChangedStates.Count + "/" + shortPaths.Count + ", " + processes + " checks in progress");
        }

        private static void CheckIfFilesHaveChanged(string shortPath, string fromPath, string toPath, ConcurrentDictionary<string, bool> haveChangedStates)
		{
			var hasChanged = !CheckAreFilesIdentical(fromPath, toPath);


			while (!haveChangedStates.TryAdd(shortPath, hasChanged)) ;

			Interlocked.Decrement(ref processes);
        }

        public static bool CheckAreFilesIdentical(string fromPath, string toPath)
		{
			var thisSize = new FileInfo(fromPath).Length;
			var toSize = new FileInfo(toPath).Length;

			if (thisSize != toSize) {
				return false;
			}
			else {

				//try some fast checks first
				var fs1 = File.OpenRead(fromPath);
				var fs2 = File.OpenRead(toPath);
				var middle = fs1.Length / 2;
				fs1.Position = middle;
				fs2.Position = middle;
				var byte1 = fs1.ReadByte();
				var byte2 = fs2.ReadByte();
				fs1.Dispose();
				fs2.Dispose();
				if(byte1 != byte2) {
					Debug.Log("middle byte check: different !" + fromPath);
					return false;
                }

				if(thisSize > 1024 * 1024 * 16) {
					return CheckIfFilesAreIdenticalMultiThreaded(fromPath, toPath);
				}
                else {
					var sourceHash = GeneralHashFile(fromPath);
					var destinationHash = GeneralHashFile(toPath);
					return sourceHash == destinationHash;
				}
			}
		}

		public class AreaCheckJob
        {
			public int workerID;

			public long startIndex;
			public long endIndex;
        }

#if UNITY_EDITOR
		[MenuItem("Debug/TestSameFileCheck")]
#endif
		public static void TestSameFileCheck() {
			CheckIfFilesAreIdenticalMultiThreaded(@"D:\attbuilds\att_auto\att_cs3792_132811237681389560\att_Data\resources.assets.resS", @"C:\temp\copytest\copiedATT\att_Data\resources.assets.resS");
		}

        private static bool CheckIfFilesAreIdenticalMultiThreaded(string fromPath, string toPath)
		{
			Debug.Log("CheckIfFilesAreIdenticalMultiThreaded " + fromPath + " " + toPath);
			var timer = System.Diagnostics.Stopwatch.StartNew();

			var inf = new FileInfo(fromPath);
			var len = inf.Length;

			//var workersNum = 16;
			var chunkSize = 1024 * 1024 * 16;

			//var wholeWorkAreaSize = len / workersNum;

			int i = 0;
			long filePoint = 0;
			List<AreaCheckJob> jobs = new List<AreaCheckJob>();

			while(true) {
				var thisSize = System.Math.Min(chunkSize, len - filePoint);
				var check = new AreaCheckJob();
				check.workerID = i;
				check.startIndex = filePoint;
				filePoint += thisSize;
				check.endIndex = filePoint;

				jobs.Add(check);
				if (filePoint == len)
					break;
				i++;
			}

            /*foreach (var item in jobs) {
				Debug.Log(item.workerID + " " + item.startIndex + "->" + item.endIndex + "siz:"+(item.endIndex - item.startIndex) );
            }*/

			//shuffle
			var rand = new System.Random();
			jobs = jobs.OrderBy(x => rand.Next()).ToList();

			int currentProcessCount = 0;

			bool changed = false;

            while (true) {
				if (changed) {					
					break;
				}
				if (jobs.Count == 0) {
					if(currentProcessCount == 0) {												
						break;
                    }
					Thread.Sleep(1);
					continue;
				}
				if (currentProcessCount > 16) {
					Thread.Sleep(1);
					continue;
				}

				var pluckedJob = jobs[0];
				jobs.RemoveAt(0);
				Interlocked.Increment(ref currentProcessCount);
				ThreadPool.QueueUserWorkItem((x) => CheckAreaChanged(fromPath, toPath, pluckedJob, ref currentProcessCount, ref changed)); 				
            }

			Debug.Log("CheckIfFilesAreIdenticalMultiThreaded result:" + !changed + " " + inf.Name + " time taken:"+ timer.Elapsed.TotalSeconds.ToString("0.00")+"sec, "+ ByteLenghtToHumanReadable((long)(len / timer.Elapsed.TotalSeconds))+"/sec" );
			return !changed;
		}


        private static void CheckAreaChanged(string fromPath, string toPath, AreaCheckJob pluckedJob, ref int currentProcessCount, ref bool changed)
		{
			var arr1 = ReadPartOfFile(fromPath, pluckedJob.startIndex, pluckedJob.endIndex);
			var arr2 = ReadPartOfFile(toPath, pluckedJob.startIndex, pluckedJob.endIndex);
			
            for (int i = 0; i < arr1.Length; i++) {
				if(arr1[i] != arr2[i]) {
					changed = true;
					Debug.Log("job found "+new FileInfo(fromPath).Name+ " changed point at offset "+(pluckedJob.startIndex + i) + " " + ByteLenghtToHumanReadable(pluckedJob.startIndex) + " jobid:"+ pluckedJob.workerID);
					break;
                }
            }
			
			Interlocked.Decrement(ref currentProcessCount);
		}

        private static byte[] ReadPartOfFile(string path, long startIndex, long endIndex)
		{
			var fs1 = File.OpenRead(path);
			fs1.Position = startIndex;
			long ind = endIndex - startIndex;
			var arr = new byte[ind];
			var read = fs1.Read(arr, 0, arr.Length);
			if (read != arr.Length) Debug.LogError("wtf read wrong amount");
			fs1.Dispose();
			return arr;
		}

        private static List<string> GetWhichFilesHaveChanged(List<string> shortPaths, string pathBaseA, string pathBaseB)
		{
			ConcurrentDictionary<string, bool> haveChangedStates = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

			int waitCnt = 0;
			//ThreadPool.SetMaxThreads(SystemInfo.processorCount - 2, 1);
			foreach (var item in shortPaths) {
				while (processes > 48)
					Thread.Sleep(1);

				var frompath = pathBaseA + "/" + item;
				var toPath = pathBaseB + "/" + item;

				Interlocked.Increment(ref processes);
				ThreadPool.QueueUserWorkItem((x) => CheckIfFilesHaveChanged(item, frompath, toPath, haveChangedStates));

				waitCnt++;
				if (waitCnt % 1000 == 0) LogProcessing(shortPaths, haveChangedStates);
			}

			while (haveChangedStates.Count != shortPaths.Count) {
				Thread.Sleep(10);
				waitCnt++;
				if (waitCnt % 100 == 0) LogProcessing(shortPaths, haveChangedStates);
			}
			Debug.Log("Hashes checked");

			var changedFiles = haveChangedStates.Where(x => x.Value).Select(k => k.Key).ToList();
			return changedFiles;
		}

		public static void CopyFolderOld(string from, string to, bool deletePreviousContentWhileReplacing = false, bool avoidRewrites = false)
		{
			var actuallyCopiedFiles = new List<string>();
			int fullCopyCount = 0;
			int fullSkipCount = 0;
			CopyFolderRecursive(from, to, ref fullCopyCount, ref fullSkipCount, 0, deletePreviousContentWhileReplacing,  avoidRewrites, actuallyCopiedFiles);

			var copiedFileInfos = actuallyCopiedFiles.Select(x => new FileInfo(x)).ToList();
						
			Debug.Log(new string('#',200) + "\nCopy complete, processed " + fullCopyCount + " files, skipped because hash match:" + fullSkipCount);
			var log = "Files actually changed:" + copiedFileInfos.Count + " (total size:" + MaxinRandomUtils.ByteLenghtToHumanReadable(copiedFileInfos.Sum(x => x.Length)) + "):";
            foreach (var item in copiedFileInfos) {
				log += "\n\t"+item.FullName+" ("+MaxinRandomUtils.ByteLenghtToHumanReadable(item.Length)+")";
            }
			log += "\n" + new string('#', 200);
			Debug.Log(log);
		}

		public static int CopyFolderRecursive(string from, string to, ref int fullCopyCount, ref int fullSkipCount, int copiedCount, bool deletePreviousContentWhileReplacing, bool avoidRewrites, List<string> actuallyCopiedFiles)
        {
            DirectoryInfo source = new DirectoryInfo(from);
            DirectoryInfo target = new DirectoryInfo(to);

            if (!Directory.Exists(target.FullName)) {
                Debug.Log("CopyFolder: Creating directory:\t " + target.FullName);
                Directory.CreateDirectory(target.FullName);
            }

            string[] pathsBefore = null;
            if (deletePreviousContentWhileReplacing) {
                pathsBefore = Directory.GetFileSystemEntries(from, "*", SearchOption.AllDirectories);
                pathsBefore = pathsBefore.Select(x => x.Replace(from, to)).ToArray();
            }

            if(copiedCount == 0) {
                var filesToCopy = Directory.GetFiles(from, "*", SearchOption.AllDirectories);
                fullCopyCount = filesToCopy.Length;
            }


			var files = source.GetFiles().ToList();

			var skippableFilePaths = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

			if(avoidRewrites) {
				var checkedFilePaths = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

				//multi-threaded hash check to avoid rewriting files
				foreach (FileInfo fi in files) {
					try {
						AsyncRunnerHelper.RunInSeparateThread(() => {

							var sourceFilePath = fi.FullName;

							try {
								var fullPathTo = Path.Combine(target.FullName, fi.Name);

								if (File.Exists(fullPathTo) && new FileInfo(fullPathTo).Length == fi.Length) {
									var sourceHash = GeneralHashFile(sourceFilePath);
									var destinationHash = GeneralHashFile(fullPathTo);

									Debug.Log("HASHCHECK " + fi.Name + "\n sourceHash:\t" + sourceHash + "\n" + "destinationHash:\t" + destinationHash);

									if (sourceHash == destinationHash) {
										skippableFilePaths.TryAdd(sourceFilePath, true);
									}
								}
								/*else {
									throw new System.Exception(" does not exist:" + fullPathTo);
								}*/
							}
							catch (System.Exception e) {
								Debug.LogError(e);
							}

							checkedFilePaths.TryAdd(sourceFilePath, true);
						});
					}
					catch (System.Exception e) {
						Debug.LogError("AsyncRunnerHelper fail while copying, will just copy needlessly at worst so no danger:"+e);
					}
				}

				var timer = System.Diagnostics.Stopwatch.StartNew();
				while (true) {
					if (checkedFilePaths.Count == files.Count)
						break;

					if(timer.Elapsed.Seconds > 20) { //this thing breaks once a while, if it breaks it just means it will do a copy regardless if the file has changed so it's fine
						Debug.LogError("hash check failure, did it hang? breaking (folder:" + source.FullName + ")");
						break;
                    }

					Thread.Sleep(1);
				}
			}



			// Copy each file into the new directory.
			foreach (FileInfo fi in files) {
                var sourceFilePath = fi.FullName;

				if(skippableFilePaths.ContainsKey(sourceFilePath)) {
					Debug.Log("File Can be skipped, no change to content:" + sourceFilePath);
					copiedCount++;
					fullSkipCount++;
					continue;
                }

                var fullPathTo = Path.Combine(target.FullName, fi.Name);
				//UnityEngine.Debug.LogFormat($"CopyFolder: Copying file {copiedCount+1}/{fullCopyCount} {sourceFilePath} => {fullPathTo}");
				UnityEngine.Debug.LogFormat($"CopyFolder: Copying file {sourceFilePath} => {fullPathTo}");
				//fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
				//File.WriteAllBytes(fullPathTo, File.ReadAllBytes(sourceFilePath));
				//CopyFileLongPathSupporting(sourceFilePath, fullPathTo);
				File.Copy(sourceFilePath, fullPathTo, true);
				actuallyCopiedFiles.Add(fullPathTo);
				copiedCount++;
			}

			// Copy each subdirectory using recursion.
			foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
                var targPath = Path.Combine(target.FullName, diSourceSubDir.Name);
                //targPath = GetWin32LongPath(targPath);
                //DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);

                //Debug.Log("debug targPath:" + targPath);
                var nextTargetSubDir = new DirectoryInfo(targPath);
                nextTargetSubDir.Create();

                copiedCount = CopyFolderRecursive(diSourceSubDir.FullName, nextTargetSubDir.FullName, ref fullCopyCount, ref fullSkipCount, copiedCount, deletePreviousContentWhileReplacing, avoidRewrites, actuallyCopiedFiles);
			}


            if(deletePreviousContentWhileReplacing) {
                var pathsAfter = Directory.GetFileSystemEntries(to, "*", SearchOption.AllDirectories);
                var pathsOnlyInPrevious = pathsAfter.Where(x => !pathsBefore.Contains(x)).ToArray();


                foreach (var item in pathsOnlyInPrevious) {
                    if (File.Exists(item)) {
                        Debug.Log("CopyFolder: Deleting file only in previous content of target folder:\t " + item);
                        File.Delete(item);
                    }
                    if (Directory.Exists(item)) {
                        Debug.Log("CopyFolder: Deleting directory only in previous content of folder:\t " + item);
                        Directory.Delete(item, true);
                    }
                }
            }

            return copiedCount;
        }

		public static string GeneralHashFile(string filePath)
		{
			//Debug.Log("GeneralHashFile "+ filePath);
			if (Directory.Exists(filePath)) return "0";
			var timer = System.Diagnostics.Stopwatch.StartNew();
			using (SHA1 sha = SHA1.Create()) {
				var file = new FileInfo(filePath);
				using (var handle = file.OpenRead()) {
					var hashBytes = sha.ComputeHash(handle);
					var hashString = System.Convert.ToBase64String(hashBytes);

					if (timer.ElapsedMilliseconds > 3000) {
						Debug.Log("hashing took " + timer.ElapsedMilliseconds + " ms for " + filePath + "\n" +
						"(" + MaxinRandomUtils.ByteLenghtToHumanReadable(file.Length) + ", " + MaxinRandomUtils.ByteLenghtToHumanReadable((long)((float)file.Length / timer.Elapsed.TotalSeconds)) + "/s" + ")");
					}
					return hashString;
				}
			}
		}

        public static FileInfo CopyFileLongPathSupporting(string fromPath, string toPath, bool overWrite = true) {
            fromPath = GetWin32LongPath(fromPath);
            toPath = GetWin32LongPath(toPath);

            var newFile = new FileInfo(fromPath);
            newFile.CopyTo(toPath, overWrite);

            return newFile;
        }

        public static void MoveFileLongPathSupporting(string fromPath, string toPath) {
            fromPath = GetWin32LongPath(fromPath);
            toPath = GetWin32LongPath(toPath);

            new FileInfo(fromPath).MoveTo(toPath);
        }

        public static void MoveFolderLongPathSupporting(string fromPath, string toPath) {
            fromPath = GetWin32LongPath(fromPath);
            toPath = GetWin32LongPath(toPath);

            new DirectoryInfo(fromPath).MoveTo(toPath);
        }

        public static void MoveDirectoryOverWritingFiles(string source, string target, bool deleteOriginal = true) {
            var stack = new Stack<Folders>();
            stack.Push(new Folders(source, target));

            while (stack.Count > 0) {
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                foreach (var file in Directory.GetFiles(folders.Source, "*.*")) {
                    string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }

                foreach (var folder in Directory.GetDirectories(folders.Source)) {
                    stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
            if(deleteOriginal) Directory.Delete(source, true);
        }
        public class Folders
        {
            public string Source { get; private set; }
            public string Target { get; private set; }

            public Folders(string source, string target) {
                Source = source;
                Target = target;
            }
        }

        public static string GetNiceTimerMSString(this System.Diagnostics.Stopwatch timer) {
            return System.Math.Round(((float)timer.ElapsedTicks / (float)System.Diagnostics.Stopwatch.Frequency) * 1000f, 2) + " ms";
        }

        public static string GetASCIIProgressBar(float progressGues, int widthInChars) {
            var charsCount = widthInChars;
            //charsCount = 20;

            var donesCount = (int)System.Math.Round(progressGues * charsCount);
            var emptysCount = charsCount - donesCount;
            if (emptysCount < 0) emptysCount = 0;

            return "[" + new string('0', donesCount) + new string('_', emptysCount) + "]";

            //var lines = new String('-', Console.WindowWidth - 10);
        }

        public static List<char> GetASCIIChars() {
            var printableChars = new List<char>();
            for (System.Int32 i = 0x20; i <= 0x7e; i++) {
                printableChars.Add(System.Convert.ToChar(i));
            }
            return printableChars;
        }

        private static string m_AsciiCharsString = "";

        public static string GetASCIICharsString() {
            if(m_AsciiCharsString == "") {
                m_AsciiCharsString = "";
                var chars = GetASCIIChars();
                foreach (var item in chars) {
                    m_AsciiCharsString += item;
                }
            }
            return m_AsciiCharsString;
        }

        public static string LimitToMaxLenOf(this string inStr, int maxLen) {
            if (inStr.Length > maxLen) {
                inStr = inStr.Substring(0, maxLen) + "<TRUNCATED>";
            }
            return inStr;
        }

	    public static void TryCatchedExec(System.Action inAction) {
		    try {
			    inAction();
		    }
		    catch(System.Exception e) {
			    Debug.LogError ("TryCatchedExec crash for " + inAction.Method.NullSafeToString () + ":" + e.ToString());
		    }
	    }

	    public static string NullSafeToString (this object o)
	    {
		    if (o == null) { 
			    return "null";
		    }
		    else {
			    string toStr = "";
			    try { 
				    toStr = o.ToString();
			    }
			    catch(System.Exception e) {
				    var err = o.GetType().ToString() + " toStr CRASH " + e;
				    Debug.LogError(e);
				    return err;                   
			    }
			    return toStr;
		    }

	    }

	    public static void TryWriteDateToFile(System.DateTime inDateTime, string inPath) {
		    TryCatchedExec (() => {
			    File.WriteAllText(inPath,inDateTime.ToFileTime().ToString());
		    });
	    }

	    public static System.DateTime TryGetDateTimeFromPath(string inPath) {
		    System.DateTime dateTime = System.DateTime.MinValue;
		    TryCatchedExec (() => {
			    var str = File.ReadAllText (inPath);
			    var longgg = long.Parse (str);
			    dateTime = System.DateTime.FromFileTime (longgg);			
		    });
		    return dateTime;
	    }			

	    public static void TryReNameAFileOrDir(FileSystemInfo inInfo, string newName, bool overwriteExisting = false) {
		    TryCatchedExec (() => {

			    Debug.Log("TRYING TO RENAME FILE/DIRECTORY "+inInfo+" TO "+newName);

			

			    var asFile = inInfo as FileInfo;
			    if(asFile != null) {
				    var pathh = asFile.DirectoryName+"/"+newName;
				    if(File.Exists(pathh) && overwriteExisting) {
					    Debug.LogWarning("TryReNameAFileOrDir: overwrite: deleting previous file at "+pathh);
					    File.Delete(pathh);
				    }
				    asFile.MoveTo(pathh);
			    }
			    else {
				    var asDir = inInfo as DirectoryInfo;
				    var pathh = asDir.Parent.FullName +"/"+newName;
				    if(Directory.Exists(pathh) && overwriteExisting) {
					    Debug.LogWarning("TryReNameAFileOrDir: overwrite: deleting previous directory at "+pathh);
					    Directory.Delete(pathh,true);
				    }
				    asDir.MoveTo(pathh);
			    }
		    });
	    }

        public static string WithCharsRemovedFromEnd(this string inStr, int toRemCount) {
            var str = inStr.Substring(0, inStr.Length - toRemCount);
            return str;
        }

        public static string Tail(this string inStr, int tailCharCount) {

            tailCharCount = Mathf.Clamp(tailCharCount, 0, inStr.Length-1);
            var str = inStr.Substring(inStr.Length - tailCharCount, tailCharCount);

            return str;
        }

        public static Color GammaToLinear(Color inColor) {
            inColor.r = Mathf.GammaToLinearSpace(inColor.r);
            inColor.g = Mathf.GammaToLinearSpace(inColor.g);
            inColor.b = Mathf.GammaToLinearSpace(inColor.b);
            inColor.a = Mathf.GammaToLinearSpace(inColor.a);

            return inColor;
        }

        public static Color LinearToGamma(Color inColor) {
            inColor.r = Mathf.LinearToGammaSpace(inColor.r);
            inColor.g = Mathf.LinearToGammaSpace(inColor.g);
            inColor.b = Mathf.LinearToGammaSpace(inColor.b);
            inColor.a = Mathf.LinearToGammaSpace(inColor.a);

            return inColor;
        }

        public static bool BetterContains(this string wholeString, string stringToFind, bool returnTrueWhenFilterOrTargetEmpty = true) {

            if (string.IsNullOrEmpty(wholeString)) return returnTrueWhenFilterOrTargetEmpty;
            if (string.IsNullOrEmpty(stringToFind)) return returnTrueWhenFilterOrTargetEmpty;

            var indexof = wholeString.IndexOf(stringToFind, System.StringComparison.OrdinalIgnoreCase);

            if (indexof != -1) return true;
            else return false;
        }

        public static void MakeDirectoryForFilePath(string inFilePath) {
            var fileinf = new FileInfo(inFilePath);

            Directory.CreateDirectory(fileinf.Directory.FullName);
        }


        public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey,TValue> dict, TKey key, TValue value) {
            if(!dict.ContainsKey(key)) {
                dict.Add(key, value);
            }
            else {
                dict[key] = value;
            }
        }

        //make these distinct to reduce errors!
        public static TValue GetNullSafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
        {
            if (!dict.ContainsKey(key)) return null;
            return dict[key];
        }
        public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : struct
        {
            if (!dict.ContainsKey(key)) return default(TValue);
            return dict[key];
        }

        public static void ClearUnityNulledKeys<T1, T2>(this Dictionary<T1, T2> dict) where T1 : UnityEngine.Object {
            var badKeys = dict.Where(x => x.Key == null).ToList();
            foreach (var item in badKeys) {
                dict.Remove(item.Key);
            }
        }

        //note: found this from LINQ actually so abort (though could do the thing above still to ease complexity
        /*public static T GetAtOrNull<T>(this IEnumerable<T> ie, int indx) where T : class
        {
            if(indx >= ie.Count()) {
                return null;
            }
            else {
                return ie.ElementAtOrDefault(indx);
            }
        }*/


        //copypaste-y for maximum speeds
        public static T GetOrAddComponent<T>(this GameObject inGo) where T : UnityEngine.Component
        {
            var existing = inGo.GetComponent<T>();
            if (existing != null) {
                return existing;
            }
            else {
                var added = inGo.AddComponent<T>();
                if (added == null) Debug.LogError("GetOrAddComponent failed to add component", inGo);
                return added;
            }
        }
        public static T GetOrAddComponent<T>(this UnityEngine.Component inComp)  where T : UnityEngine.Component
        {
            var existing = inComp.GetComponent<T>();
            if(existing != null) {
                return existing;
            }
            else {
                var added = inComp.gameObject.AddComponent<T>();
                if (added == null) Debug.LogError("GetOrAddComponent failed to add component", inComp);
                return added;
            }
        }

        public static GameObject UICopy(GameObject template, Transform changeParentTo = null, bool ignSibling = false) {
            var copy = Object.Instantiate(template);
            copy.name += "_COPY";
            var origRekt = template.transform as RectTransform;
            var copyRekt = copy.transform as RectTransform;

            Transform prnt = null;
            if(changeParentTo != null) {
                prnt = changeParentTo;
            }
            else {
                prnt = origRekt.parent;
            }

            copyRekt.SetParent(prnt);
            copyRekt.localScale = origRekt.localScale;
            copyRekt.localPosition = origRekt.localPosition;

			if(!ignSibling)
				copyRekt.SetSiblingIndex(origRekt.GetSiblingIndex() + 1);

            return copy;
        }

        public static void ZeroOut(this Transform inTransform, bool setScaleToo = true) {
            inTransform.localPosition = Vector3.zero;
            inTransform.localEulerAngles = Vector3.zero;
            if(setScaleToo)inTransform.localScale = Vector3.one;
        }

        public static Bounds GetCombinedColliderBounds(Transform parent) {
            var colliders = parent.GetComponentsInChildren<Collider>();

            Bounds bounds = default(Bounds); //just satisfy compiler, will not be used (would expand from origin which would be incorrect)
            bool haveBounds = false;

            foreach (var item in colliders) {
                if (haveBounds) {
                    bounds.Encapsulate(item.bounds);
                }
                else {
                    bounds = item.bounds;
                    haveBounds = true;
                }
            }

            return bounds;
        }

    #if UNITY_EDITOR

        //[UnityEditor.MenuItem("CONTEXT/GameObject/CreateParentAndPutUnder")]
        [UnityEditor.MenuItem("GameObject/CreateParentAndPutUnder",priority = 0)]
        static void CreateParentAndPutUnder(UnityEditor.MenuCommand command) {
            //Rigidbody body = (Rigidbody)command.context;
            var sel = ((GameObject)command.context).transform;
            //var sel = UnityEditor.Selection.activeTransform;


            var createdParent = new GameObject(sel.name+"_parent").transform;
            createdParent.parent = sel.parent;
            createdParent.position = sel.position;
            createdParent.rotation = sel.rotation;
            createdParent.localScale = Vector3.one;

            UnityEditor.Undo.RegisterCreatedObjectUndo(createdParent.gameObject, "CreateParentAndPutUnder " + sel.name);

            UnityEditor.Undo.RecordObject(sel, "set sel under new parent");
            sel.parent = createdParent;

            sel.localPosition = Vector3.zero; //don't reset rotation, don't reset scale
        }

#endif

        public static bool IsInEditorModeAndAndNotSelectedInAnyWay(Transform inTrans) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                if(UnityEditor.Selection.activeGameObject == null) {
                    return true;
                }
                if (!inTrans.IsAnyChildOf(UnityEditor.Selection.activeTransform) && !UnityEditor.Selection.activeTransform.IsAnyChildOf(inTrans)) {
                    return true;
                }
            }
#endif
            return false;
        }




        /*[UnityEditor.MenuItem("Tools/CreateParentAndPutUnder")]
        public static void CreateParentAndPutUnder() {
            var sel = UnityEditor.Selection.activeTransform;


            var createdParent = new GameObject(sel.name+"_par").transform;
            createdParent.parent = sel.parent;
            createdParent.position = sel.position;
            createdParent.rotation = sel.rotation;
            createdParent.localScale = Vector3.one;

            UnityEditor.Undo.RegisterCreatedObjectUndo(createdParent.gameObject, "CreateParentAndPutUnder " + sel.name);

            UnityEditor.Undo.RecordObject(sel, "set sel under new parent");
            sel.parent = createdParent;

            sel.localPosition = Vector3.zero; //don't reset rotation, don't reset scale
        }*/


        public static bool Matches(byte[] arr, byte[] v) {
            if (arr.Length != v.Length) return false;
            for (int i = 0; i < arr.Length; i++) {
                if (arr[i] != v[i]) return false;
            }
            return true;
        }

        public static bool Matches<T>(IEnumerable<T> firstSeq, IEnumerable<T> secondSeq) {
            if (firstSeq.Count() != secondSeq.Count()) return false;

            for (int i = 0; i < firstSeq.Count(); i++) {
                T first = firstSeq.ElementAtOrDefault(i);
                T second = secondSeq.ElementAtOrDefault(i);

                if (!EqualityComparer<T>.Default.Equals(first, second)) {
                    return false;
                }
            }

            return true;
        }

		/// <summary> Only adds DIRECT children, not grandchildren</summary>
        public static List<Transform> GetChildren(this Transform trans)
		{
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < trans.childCount; i++) {
                children.Add(trans.GetChild(i));
            }
            return children;
        }

		public static T RandomItem<T>(this IList<T> list) {
			var count = list.Count;
			if (count == 0) throw new System.Exception("RandomItem: no items in list!!");
			return list[Random.Range(0, count)];
		}


		public struct ProgressInfo
        {
			public long processedLen;
			public long totalLen;
			public System.TimeSpan timeElapsed;
			public long perSec;
			public string tag;
            public long perSecSmoothed;

            public string ReportString {
				get {
					return ByteLenghtToHumanReadable(processedLen) + " / " + (totalLen == -1 ? "UNKNOWN" : ByteLenghtToHumanReadable(totalLen)+" ("+SpeedReportString+")");
                }
            }
			public string SpeedReportString {
				get {
					return ByteLenghtToHumanReadable(perSecSmoothed) + "/s";
				}
            }

            public float Progression {		
				get {
					return (float)processedLen / totalLen;
                }
            }

        }

		//copypasta from BEAM/max's steam clone
		public static long CopyStream(Stream fromStream, Stream toStream, string progressReportingTag, int limitByteCount = -1, System.Action<float> onAnalogProgress = null, System.Action<ProgressInfo> onProgressAdvanced = null) {

			/*fromStream.CopyTo(toStream);
			return;*/

			long streamLenOrLimit = 0;

			try {
				streamLenOrLimit = fromStream.Length;
			}
			catch (System.NotSupportedException) {
				streamLenOrLimit = -1;
			}

			if (limitByteCount != -1 && streamLenOrLimit != -1) {
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
			while (true) {				
				read = fromStream.Read(buffer, 0, buffer.Length);
				if (read == 0) break;

				toStream.Write(buffer, 0, read);

				goneWithoutReportCounter += read;
				allReadCount += read;
				if (goneWithoutReportCounter >= reportEveryBytes) {
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

					smoothedDeltaTime = Mathf.SmoothDamp(smoothedDeltaTime, (float)intervalTime.TotalSeconds, ref velSpeed, 5f, 10000000f, (float)intervalTime.TotalSeconds);
					rep.perSecSmoothed = (long)(advancedThisTick / smoothedDeltaTime);

					//Debug.Log("Streaming data " + progressReportingTag + " " + ByteLenghtToHumanReadable(allReadCount) + " / " + (streamLenOrLimit == -1 ? "UNKNOWN" : ByteLenghtToHumanReadable(streamLenOrLimit)));
					//Debug.Log("Progress " + rep.tag + " " + rep.ReportString);
					//Console.ReadKey();
					
					if(onProgressAdvanced != null) {
						onProgressAdvanced(rep);
                    }

					if (onAnalogProgress != null) {
						/*float toRep = 0f;
						if (streamLenOrLimit != -1) {
							toRep = (float)allReadCount / (float)streamLenOrLimit;
						}
						onAnalogProgress(toRep);*/
						onAnalogProgress(rep.Progression);
					}
				}

				if (limitByteCount != -1 && allReadCount > limitByteCount) {
					throw new System.Exception("read too long wtf no");
				}
				if (allReadCount == limitByteCount) {
					break;
				}
			}

			//Debug.Log("CopyStream " + progressReportingTag + " done, transferred:" + ByteLenghtToHumanReadable(allReadCount));
			return allReadCount;
		}

        public static int GetComponentIndex(this Component comp) {
            return System.Array.IndexOf(comp.GetComponents<Component>(),comp);
        }

		public static Bounds GetCombinedRendererBounds(Transform trans, bool workOnPrefab = false)
		{
			UnityEngine.Profiling.Profiler.BeginSample("GetCombinedRendererBounds");

			UnityEngine.Profiling.Profiler.BeginSample("GetComponentsInChildren");
			Renderer[] rends;
			if (workOnPrefab) {
				rends = trans.GetComponentsInChildren<Renderer>(true);
            }
            else {
                rends = trans.GetComponentsInChildren<Renderer>();
            }
			UnityEngine.Profiling.Profiler.EndSample();

			Bounds bounds = new Bounds();
			for (int i = 0; i < rends.Length; i++) {
				if (i == 0) {
					bounds = rends[i].bounds;
				}
				else {
					bounds.Encapsulate(rends[i].bounds);
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
			return bounds;
		}


		static Dictionary<Transform, Vector3> boundsToPivotDiffsCached = new Dictionary<Transform, Vector3>();

        public static Vector3 GetCombinedRendererBoundsCenter(Transform trans, bool rotationAgnostic = true, bool cacheObjectSizes = false, bool workOnPrefab = false) {
            
            Vector3 toRet = new Vector3();

			Vector3 boundsCenterToPivotDiff;

			if(rotationAgnostic) {
                if (cacheObjectSizes) {
					if(boundsToPivotDiffsCached.TryGetValue(trans, out Vector3 cachedDiff)) {
						boundsCenterToPivotDiff = cachedDiff;
                    }
                    else {
						boundsCenterToPivotDiff = GetBoundsCenterToPivotDiff(trans, rotationAgnostic, workOnPrefab:workOnPrefab);
						boundsToPivotDiffsCached.Add(trans, boundsCenterToPivotDiff);
                    }
                }
                else {
					boundsCenterToPivotDiff = GetBoundsCenterToPivotDiff(trans, rotationAgnostic, workOnPrefab: workOnPrefab);
				}
				toRet = trans.position + trans.rotation * boundsCenterToPivotDiff; //rotation adjusted
            }
            else {
				toRet = GetBoundsCenterToPivotDiff(trans, false, workOnPrefab: workOnPrefab);
            }

            return toRet;
        }

        private static Vector3 GetBoundsCenterToPivotDiff(Transform trans, bool rotationAgnostic, bool workOnPrefab)
		{
			Quaternion prevRot = trans.rotation;			
			Vector3 toRet = new Vector3();


			if (rotationAgnostic) {
				trans.rotation = Quaternion.identity;
			}

			var bounds = GetCombinedRendererBounds(trans, workOnPrefab);

			toRet = bounds.center - trans.position;			


			if (rotationAgnostic) {
				trans.rotation = prevRot;				
			}

			return toRet;
		}


        //static List<Component> tempResults = null;

        public static List<T> FindInterfacesOfType<T>()/* where T : Component*/
		{
			List<T> list = new List<T>();

			/*if(tempResults == null) {
				tempResults = new List<Component>(1024);
            }*/

            for (int i = 0; i < SceneManager.sceneCount; i++) {
				var sken = SceneManager.GetSceneAt(i);
				var roots = sken.GetRootGameObjects();
                foreach (var item in roots) {
					//item.GetComponentsInChildren<T>(tempResults);
					var temp = item.GetComponentsInChildren<T>();
					list.AddRange(temp);
                }				
            }

			return list;
		}

        public static Color WithAlphaOf(this Color inColor, float alpha) {
            inColor.a = alpha;
            return inColor;
        }

		public static Vector3 WithXOf(this Vector3 vec, float theX) {
			return new Vector3(theX, vec.y, vec.z);
        }
		public static Vector3 WithYOf(this Vector3 vec, float theY) {
			return new Vector3(vec.x, theY, vec.z);
		}
		public static Vector3 WithZOf(this Vector3 vec, float theZ) {
			return new Vector3(vec.x, vec.y, theZ);
		}

		public static string CreateMD5(string input) {
			// Use input string to calculate MD5 hash
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
				byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
				byte[] hashBytes = md5.ComputeHash(inputBytes);
				return GetMD5StringFromMD5Bytes(hashBytes);
			}
		}

		public static string CreateMD5FromFile(string path) {
			using (var md5 = System.Security.Cryptography.MD5.Create()) {
				using (var stream = File.OpenRead(path)) {
					var hashBytes = md5.ComputeHash(stream);
					return GetMD5StringFromMD5Bytes(hashBytes);				
				}
			}
		}

		public static string GetMD5StringFromMD5Bytes(byte[] hashBytes) {
			// Convert the byte array to hexadecimal string
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hashBytes.Length; i++) {
				sb.Append(hashBytes[i].ToString("X2"));
			}
			return sb.ToString();
		}

        public static string GetOnOffStr(this bool bol) {
            return bol ? "ON" : "OFF";
        }

        //4 params, 2 lines, still quite useful!
        public static void CrossCheckAndGetAddedAndRemovedItems<T>(List<T> oldList, List<T> newList, out List<T> newItems, out List<T> removedItems) {
			newItems = newList.Where(x => !oldList.Contains(x)).ToList();
			removedItems = oldList.Where(x => !newList.Contains(x)).ToList();
		}

#if UNITY_EDITOR

#pragma warning disable CS0618
        public static List<GameObject> FindAllPrefabInstances(UnityEngine.Object myPrefab) {
            var results = new List<GameObject>();
            var allObjects = (GameObject[])Object.FindObjectsOfType(typeof(GameObject));
            foreach (GameObject GO in allObjects) {
                if (UnityEditor.EditorUtility.GetPrefabType(GO) == UnityEditor.PrefabType.PrefabInstance) {
                    UnityEngine.Object GO_prefab = UnityEditor.EditorUtility.GetPrefabParent(GO);
                    if (myPrefab == GO_prefab)
                        results.Add(GO);
                }
            }
            return results;
        }
#pragma warning restore CS0618

        public static void MakeSureIsFirstComponent(this Component comp) {

			var prevIndx = System.Array.IndexOf(comp.gameObject.GetComponents<Component>(), comp);

			//if is 1, need 0. If is 3, need 2
			var needNudgesCount = prevIndx - 1;

            for (int i = 0; i < needNudgesCount; i++) {
				UnityEditorInternal.ComponentUtility.MoveComponentUp(comp);
            }

			//doesn't seem to work anymore:
			//It is not allowed to modify the m_Component property
			//UnityEditor.SerializedProperty:MoveArrayElement(Int32, Int32)

			/*var serObj = new UnityEditor.SerializedObject(comp.gameObject);
			var componentsSerProp = serObj.FindProperty("m_Component");

			for (int i = 0; i < componentsSerProp.arraySize; i++) {
				var elementAtIndex = componentsSerProp.GetArrayElementAtIndex(i);
				var actualComp = elementAtIndex.FindPropertyRelative("component");

				if (actualComp.objectReferenceValue == comp) {
					if (i != 1) {
						componentsSerProp.MoveArrayElement(i, 1);
						serObj.ApplyModifiedProperties();
					}
				}
			}*/
		}

        [UnityEditor.MenuItem("CONTEXT/Transform/CopyGOPath")]
        public static void CopyGOPathToClipBoard(UnityEditor.MenuCommand comm) {
            var trans = comm.context as Transform;
            var path = GetHieararchyVerbose(trans);
            GUIUtility.systemCopyBuffer = path;

            Debug.Log("Put to clipboard: " + path);
        }

        public static GameObject RecursiveFindRootPrefab(GameObject go) {
            while (true) {
                go = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (PrefabUtility.GetPrefabAssetType(go) == PrefabAssetType.Variant) {
                    Debug.Log("iterating " + go.name);
                    continue;
                }
                return go;
            }
        }

        public static void MoveComponentToIndexInEditor(Component component, int index, bool isInGUI = false)
        {
            var isPrefabInstance = UnityEditor.PrefabUtility.GetPrefabInstanceStatus(component.gameObject) != PrefabInstanceStatus.NotAPrefab;
            var isVariantPrefab = UnityEditor.PrefabUtility.GetPrefabAssetType(component.gameObject) == PrefabAssetType.Variant;

            int prevIndex = -1;
            while (true) {
                var currentIndex = System.Array.IndexOf(component.GetComponents<Component>(), component);
                if (currentIndex != index) {

                    if (prevIndex != -1 && currentIndex == prevIndex) {
                        Debug.LogWarning("Failed to move component " + component.GetType().Name + " to be first on the gameobject for some reason! " + component.transform.GetHieararchyPath(true, true));
                        return;
                    }

                    if (isPrefabInstance || isVariantPrefab) {
						if(isInGUI) {
							GUILayout.Label(MUtility.MaxinRandomUtils.GetRotatingChar() + "-This component should be first, but cannot change component sorting");
							GUILayout.Label("\n\ron this prefab instance! Please select the root prefab." + MUtility.MaxinRandomUtils.GetRotatingChar());
						}
						else {
							Debug.LogWarning(component + " This component should be first, but cannot change component sorting",component);
                        }
                        return;
                    }

                    prevIndex = currentIndex;
                    if(currentIndex > index) {
                        Debug.Log("Moving component " + component.GetType().Name + " upwards till its in index " + index + " on "+component.name);
                        UnityEditorInternal.ComponentUtility.MoveComponentUp(component);
                    }
                    else {
                        Debug.Log("Moving component " + component.GetType().Name + " downwards till its in index " + index + " on " + component.name);
                        UnityEditorInternal.ComponentUtility.MoveComponentDown(component);
                    }
                }
                else break;
            }
        }

#endif

        public static int GetCurrentPlasticChangesetIfApplicable() {
			try {
				var timer = System.Diagnostics.Stopwatch.StartNew();
				var procStart = new System.Diagnostics.ProcessStartInfo("cm", "status --cset");
				//var procStart = new System.Diagnostics.ProcessStartInfo("cmd", "/c cm status");
				procStart.CreateNoWindow = true;
				procStart.UseShellExecute = false;
				procStart.RedirectStandardOutput = true;
				var proc = System.Diagnostics.Process.Start(procStart);

				var outStr = proc.StandardOutput.ReadToEnd();
				//UnityEngine.Debug.Log(outStr);

				var csetStr = outStr.Substring(3, (outStr.IndexOf('@') - 3));
				//Debug.Log(csetStr);

				Debug.Log("GetCurrentPlasticChangesetIfApplicable took " + timer.ElapsedMilliseconds + "ms");

				return int.Parse(csetStr);
			}
			catch(System.Exception e) {
				Debug.LogError("GetCurrentPlasticChangesetIfApplicable fail:\n" + e);
				return -1;
            }
		}

		public static object ScrollEnum(object inEnum, int delta = 1) {
			var enumType = inEnum.GetType();
			var values = System.Enum.GetValues(enumType).Cast<int>();
			var max = values.Last();
			var curr = (int)inEnum;

			curr += delta;
			if(curr == -1) {
				curr = max;
            }
			else if(curr > max) {
				curr = 0;
            }

			//var currTyped = System.Convert.ChangeType(curr, enumType);
			var currTyped = System.Enum.ToObject(enumType, curr);
			return currTyped;
        }

        /*public static List<T> GetComponentsInChildrenFromPrefab<T>(this GameObject go) {
            var trans = go.transform;


            foreach (Transform item in trans) {
                Debug.Log(item);
            }

            return GetComponentsRecursive
            foreach (var item in collection) {

            }

            return new List<T>();
        }*/

        public static string[] EasySplit(this string input, string splitKey) {
            return input.Split(new[] { splitKey }, System.StringSplitOptions.None);
        }

        public static T GetRandomElement<T>(this IEnumerable<T> ienumerable) {
            var count = ienumerable.Count();
            var randomIndex = Random.Range(0, count);
            return ienumerable.ElementAt(randomIndex);
        }


        public static T GetComponentInParentEvenIfInactive<T>(this GameObject childGO) where T : Component {
            var trans = childGO.transform;

            while (true) {
                if (trans == null) return null;
                var comp = trans.GetComponent<T>();
                if (comp != null) return comp;
                trans = trans.parent;                
            }            
        }

		public static IEnumerator WaitingForCondition(System.Func<bool> conditionThatNeedsToPass, float timeout, string debugDescription)
		{
			var timer = 0f;
			int frames = 0;
			while (true) {
				if(frames % 1000 == 10)Debug.Log("WaitingForCondition poll "+ debugDescription+ " " + conditionThatNeedsToPass);
				if (conditionThatNeedsToPass()) {
					/*if(frames > 1)*/Debug.Log("WaitingForCondition "+ debugDescription + " PASS after "+ timer.ToString("0.000"+" sec, "+ frames+" frames"));
					break;
				}
				timer += Time.unscaledDeltaTime;
				if (timer > timeout) {
					Debug.LogError("WaitingForCondition hit timeout!!! " + debugDescription + " " + conditionThatNeedsToPass);
					break;
				}
				yield return null;
				frames++;
			}
		}

		public static void DoAfterConditionIsTrue(System.Func<bool> condition, System.Action whenPasses, float timeout, bool dontDestroyOnLoad, string debugDescription)
		{
			StartUndyingCoroutine(DoingAfterConditionPasses(condition, whenPasses, timeout, debugDescription),dontDestroyOnLoad:dontDestroyOnLoad);
		}

        private static IEnumerator DoingAfterConditionPasses(System.Func<bool> condition, System.Action whenPasses, float timeout, string debugDescription)
		{	
			yield return WaitingForCondition(condition, timeout, "DoingAfterConditionPasses:" + debugDescription);			
			whenPasses();
		}

		public static List<Scene> GetCurrentlyLoadedScenes() {
			List<Scene> currentlyLoadedScenes = new List<Scene>();
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				var scen = SceneManager.GetSceneAt(i);
				if (scen.IsValid() && scen.isLoaded) {
					currentlyLoadedScenes.Add(scen);
				}
			}

			return currentlyLoadedScenes;
		}

		public static string SanitizeStringToBeTCBlockCompatible(string inStr) {
			inStr = inStr.Replace(@"'", "_APOSTROPHE_");
			return inStr;
		}

		public static string GetTCOpenBlockString(string inTag) {
			inTag = SanitizeStringToBeTCBlockCompatible(inTag);
			var line = "##teamcity[blockOpened name='" + inTag + "']";
			return line;
		}

		public static string GetTCCloseBlockString(string inTag) {
			inTag = SanitizeStringToBeTCBlockCompatible(inTag);
			var line = "##teamcity[blockClosed name='" + inTag + "']";
			return line;
		}

		public static void HighlightPathInExplorer(string filePath)
		{
			filePath = filePath.Replace("/", "\\");
			var args = "/select," + "\"" + filePath + "\"" + " /separate, " + "\"" + filePath + "\"";
			UnityEngine.Debug.Log(args);
			System.Diagnostics.Process.Start("explorer.exe", args);
		}


		public static HashSet<T> MakeHashSet<T>(IEnumerable<T> things)
		{
			var set = new HashSet<T>();
            foreach (var item in things) {
				set.Add(item);
            }
			return set;
		}

		public static string ToMoreVerboseString(this Vector3 vec)
		{
			return vec.x.ToString("0.000") + ", " + vec.y.ToString("0.000") + ", " + vec.z.ToString("0.000");
		}

		public static Vector3 GetMiddleOfPoints(Vector3[] points)
		{			
			var bounds = new Bounds() { center = points[0] };

			foreach (var item in points) {
				bounds.Encapsulate(item);
			}

			return bounds.center;
		}

		public static float Range(this System.Random prng, float min, float max)
		{
			return (float)(min + (prng.NextDouble() * (max - min)));
		}



		public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
		{
			// Check arguments.
			if (plainText == null || plainText.Length <= 0)
				throw new System.ArgumentNullException("plainText");
			if (Key == null || Key.Length <= 0)
				throw new System.ArgumentNullException("Key");
			if (IV == null || IV.Length <= 0)
				throw new System.ArgumentNullException("IV");
			byte[] encrypted;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create()) {
				aesAlg.Key = Key;
				aesAlg.IV = IV;

				// Create an encryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (MemoryStream msEncrypt = new MemoryStream()) {
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}
						encrypted = msEncrypt.ToArray();
					}
				}
			}

			// Return the encrypted bytes from the memory stream.
			return encrypted;
		}

		public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV) {
			// Check arguments.
			if (cipherText == null || cipherText.Length <= 0)
				throw new System.ArgumentNullException("cipherText");
			if (Key == null || Key.Length <= 0)
				throw new System.ArgumentNullException("Key");
			if (IV == null || IV.Length <= 0)
				throw new System.ArgumentNullException("IV");

			// Declare the string used to hold
			// the decrypted text.
			string plaintext = null;

			// Create an Aes object
			// with the specified key and IV.
			using (Aes aesAlg = Aes.Create()) {
				aesAlg.Key = Key;
				aesAlg.IV = IV;

				// Create a decryptor to perform the stream transform.
				ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for decryption.
				using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
						using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {

							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadToEnd();
						}
					}
				}
			}

			return plaintext;
		}



		/// <summary>
		/// Encrypts a string
		/// </summary>
		/// <param name="PlainText">Text to be encrypted</param>
		/// <param name="Password">Password to encrypt with</param>
		/// <param name="Salt">Salt to encrypt with</param>
		/// <param name="HashAlgorithm">Can be either SHA1 or MD5</param>
		/// <param name="PasswordIterations">Number of iterations to do</param>
		/// <param name="InitialVector">Needs to be 16 ASCII characters long</param>
		/// <param name="KeySize">Can be 128, 192, or 256</param>
		/// <returns>An encrypted string</returns>
		public static string EncryptSomething(string PlainText, string Password,
			string Salt = "Kekkonen", string HashAlgorithm = "SHA1",
			int PasswordIterations = 2, string InitialVector = "OFRna73m*aze01xY",
			int KeySize = 256) {

			if (string.IsNullOrEmpty(PlainText))
				return "";

			byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(InitialVector);
			byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
			byte[] PlainTextBytes = Encoding.UTF8.GetBytes(PlainText);
			PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
			byte[] KeyBytes = DerivedPassword.GetBytes(KeySize / 8);
			RijndaelManaged SymmetricKey = new RijndaelManaged();
			SymmetricKey.Mode = CipherMode.CBC;
            SymmetricKey.Padding = PaddingMode.PKCS7;
            SymmetricKey.BlockSize = 128;
            SymmetricKey.KeySize = KeySize;

            byte[] CipherTextBytes = null;
			using (ICryptoTransform Encryptor = SymmetricKey.CreateEncryptor(KeyBytes, InitialVectorBytes)) {
				using (MemoryStream MemStream = new MemoryStream()) {
					using (CryptoStream CryptoStream = new CryptoStream(MemStream, Encryptor, CryptoStreamMode.Write)) {
						CryptoStream.Write(PlainTextBytes, 0, PlainTextBytes.Length);
						CryptoStream.FlushFinalBlock();
						CipherTextBytes = MemStream.ToArray();
						MemStream.Close();
						CryptoStream.Close();
					}
				}
			}
			SymmetricKey.Clear();
			return System.Convert.ToBase64String(CipherTextBytes);
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


		public static void PopOpenExplorer(string locationPathName)
		{
			var args = "/select," + "\"" + locationPathName + "\"" + "/separate, " + "\"" + locationPathName + "\"";
			UnityEngine.Debug.Log(args);
			System.Diagnostics.Process.Start("explorer.exe", args);
		}

#if UNITY_EDITOR

		public static List<T> FindOfTypeInAssetDataBase<T>(string extraQuery = "", params string[] folders) where T : UnityEngine.Object
		{
			var found = AssetDatabase.FindAssets("t:"+typeof(T).Name+ " " +extraQuery, folders);
			var objs = found.Select(x => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(x)));
			return objs.ToList();
		}
#endif

		public static Vector3 GetHorizonPlaneHitPos(float planeAltitude, Ray ray)
		{
			var plane = new Plane(Vector3.up, Vector3.up * planeAltitude);
			plane.Raycast(ray, out var dist);
			return ray.origin + ray.direction * dist;
		}


		static System.Random rng;

        //do the Fischer-Yates shuffle!
        public static void Shuffle<T>(IList<T> list, int seed = -1)
        {
			System.Random rngToUse;

			if (rng == null)
				rng = new System.Random();


			if (seed != -1)	{
				rngToUse = new System.Random(seed);
			}
			else rngToUse = rng; //use global rng by default, creating new is not just GC but can actually create same results if called too fast (I've seen it happen)


            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rngToUse.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

	public class MarkdownTable
    {
		public int columnWidth = 32;
		public List<string> headers = new List<string>();
		public List<List<string>> rows = new List<List<string>>();
				

		public string ToMarkdownString()
		{
			var sb = new StringBuilder();
            for (int i = 0; i < headers.Count; i++) {
				sb.Append("|");
				sb.Append(headers[i]);
				sb.Append(new string(' ',columnWidth - headers[i].Length));

				if(i == headers.Count - 1) {
					sb.Append("|");
				}
            }
			sb.AppendLine();

			for (int i = 0; i < headers.Count; i++) {
				sb.Append("|");				
				sb.Append(new string('-', columnWidth));

				if (i == headers.Count - 1) {
					sb.Append("|");
				}
			}
			sb.AppendLine();

			for (int j = 0; j < rows.Count; j++) {
				for (int i = 0; i < headers.Count; i++) {
					sb.Append("|");
					sb.Append(rows[j][i]);
					sb.Append(new string(' ', columnWidth - rows[j][i].Length));

					if (i == headers.Count - 1) {
						sb.Append("|");
					}
				}
				sb.AppendLine();
			}

			return sb.ToString();
		}
    }

	//unused but left for future consideration. Would like a version without regressions from Action and all generic overloads, with appending, direct calling etc
	public class MSafeAction<T>
    {
		private System.Action<T> internalAction;

		public void Call(T parameter)
		{
            try {
				internalAction(parameter);
            }
			catch(System.Exception e) {
				Debug.LogException(e);
            }
        }
    }


    public static class GcControl
    {
        // Unity engine function to disable the GC
        [DllImport("__Internal")]
        public static extern void GC_disable();

        // Unity engine function to enable the GC
        [DllImport("__Internal")]
        public static extern void GC_enable();
    }

    public class BidirectionalDictionary<T1,T2>
    {
        public Dictionary<T1, T2> t1ToT2 = new Dictionary<T1, T2>();
        public Dictionary<T2, T1> t2ToT1 = new Dictionary<T2, T1>();

        //use these when the types are the same
        public void AddT1T2(T1 key, T2 value) {
            Add(key, value);
        }
        public void AddT2T1(T2 value, T1 key) {
            Add(value, key);
        }

        public void Add(T1 key, T2 value) {
            t1ToT2.Add(key, value);
            t2ToT1.Add(value, key);
        }

        public void Add(T2 key, T1 value) {
            t2ToT1.Add(key, value);
            t1ToT2.Add(value, key);
        }

        public T2 this[T1 key] {
            get {
				if (!t1ToT2.ContainsKey(key)) throw new KeyNotFoundException("Key " + key + " was not found in dict! keys:" + t1ToT2.Keys.Select(x => x.ToString()).Aggregate((k, l) => k + " " + l));
                return t1ToT2[key];
            }
            set {
                t1ToT2[key] = value;
				t2ToT1[value] = key;
            }
        }
        public T1 this[T2 key] {
            get {
				if (!t2ToT1.ContainsKey(key)) throw new KeyNotFoundException("Key " + key + " was not found in dict! keys:" + t2ToT1.Keys.Select(x => x.ToString()).Aggregate((k, l) => k + " " + l));
				return t2ToT1[key];
            }
            set {
                t2ToT1[key] = value;
				t1ToT2[value] = key;
			}
        }


		public void Remove(T1 key) {
            var t2Val = this[key];
            t1ToT2.Remove(key);
            t2ToT1.Remove(t2Val);
        }
        public void Remove(T2 key) {
            var t1Val = this[key];
            t2ToT1.Remove(key);
            t1ToT2.Remove(t1Val);
        }

		//use these when the types are the same
		public void RemoveByT1Key(T1 key) {
			var t2Val = this[key];
			t1ToT2.Remove(key);
			t2ToT1.Remove(t2Val);
		}
		public void RemoveByT2Key(T2 key) {
			var t1Val = this[key];
			t2ToT1.Remove(key);
			t1ToT2.Remove(t1Val);
		}

		public bool ContainsKey(T1 key) {
            return t1ToT2.ContainsKey(key);
        }
        public bool ContainsKey(T2 key) {
            return t2ToT1.ContainsKey(key);
        }

        public void Clear() {
            t1ToT2.Clear();
            t2ToT1.Clear();
        }


        public void SetT1toT2(T1 t1, T2 t2)
        {
            t1ToT2[t1] = t2;
            t2ToT1[t2] = t1;
        }
        public void SetT2toT1(T2 t2, T1 t1)
        {
            t2ToT1[t2] = t1;
            t1ToT2[t1] = t2;
        }
    }

}