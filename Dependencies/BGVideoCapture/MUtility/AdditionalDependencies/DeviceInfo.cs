#if !DISABLE_MBUG
using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using MUtility;

//This class is basically a dump of Unity's SystemInfo class, where everything is static
[System.Serializable]
public class DeviceInfo {
	public string deviceModel;	
	public string deviceName;	
	public DeviceType deviceType;	
	public string deviceUniqueIdentifier;	
	public int graphicsDeviceID;	
	public string graphicsDeviceName;	
	public GraphicsDeviceType graphicsDeviceType;	
	public string graphicsDeviceVendor;	
	public int graphicsDeviceVendorID;	
	public string graphicsDeviceVersion;	
	public int graphicsMemorySize;	
	public bool graphicsMultiThreaded;	
	public int graphicsShaderLevel;	
	public int maxTextureSize;	
	public NPOTSupport npotSupport;	
	public string operatingSystem;	
	public int processorCount;	
	public string processorType;	
	public int supportedRenderTargetCount;	
	public bool supports3DTextures;	
	public bool supportsAccelerometer;	
	public bool supportsComputeShaders;	
	public bool supportsGyroscope;	

	public bool supportsInstancing;	
	public bool supportsLocationService;	


	public bool supportsShadows;	
	public bool supportsSparseTextures;	

	public bool supportsVibration;	
	public int systemMemorySize;

	public DeviceInfo() {} //default constructor for JSON.net so it doesn't call the one with side effects

	public DeviceInfo (bool unusedBool) {

		var intervalTimer = IntervalTimer.Start("Systeminfodump_DeviceInfo");

		deviceModel = SystemInfo.deviceModel;
		intervalTimer.Interval("deviceModel");
		deviceName = SystemInfo.deviceName;
		intervalTimer.Interval("deviceName");
		deviceType = SystemInfo.deviceType;
		intervalTimer.Interval("deviceType");
		deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
		intervalTimer.Interval("deviceUniqueIdentifier");
		graphicsDeviceID = SystemInfo.graphicsDeviceID;
		intervalTimer.Interval("graphicsDeviceID");
		graphicsDeviceName = SystemInfo.graphicsDeviceName;
		intervalTimer.Interval("graphicsDeviceName");
		graphicsDeviceType = SystemInfo.graphicsDeviceType;
		intervalTimer.Interval("graphicsDeviceType");
		graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;
		intervalTimer.Interval("graphicsDeviceVendor");
		graphicsDeviceVendorID = SystemInfo.graphicsDeviceVendorID;
		intervalTimer.Interval("graphicsDeviceVendorID");
		graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
		intervalTimer.Interval("graphicsDeviceVersion");
		graphicsMemorySize = SystemInfo.graphicsMemorySize;
		intervalTimer.Interval("graphicsMemorySize");
		graphicsMultiThreaded = SystemInfo.graphicsMultiThreaded;
		intervalTimer.Interval("graphicsMultiThreaded");
		graphicsShaderLevel = SystemInfo.graphicsShaderLevel;
		intervalTimer.Interval("graphicsShaderLevel");
		maxTextureSize = SystemInfo.maxTextureSize;
		intervalTimer.Interval("maxTextureSize");
		npotSupport = SystemInfo.npotSupport;
		intervalTimer.Interval("npotSupport");
		operatingSystem = SystemInfo.operatingSystem;
		intervalTimer.Interval("operatingSystem");
		processorCount = SystemInfo.processorCount;
		intervalTimer.Interval("processorCount");
		processorType = SystemInfo.processorType;
		intervalTimer.Interval("processorType");
		supportedRenderTargetCount = SystemInfo.supportedRenderTargetCount;
		intervalTimer.Interval("supportedRenderTargetCount");
		supports3DTextures = SystemInfo.supports3DTextures;
		intervalTimer.Interval("supports3DTextures");
		supportsAccelerometer = SystemInfo.supportsAccelerometer;
		intervalTimer.Interval("supportsAccelerometer");
		supportsComputeShaders = SystemInfo.supportsComputeShaders;
		intervalTimer.Interval("supportsComputeShaders");
		supportsGyroscope = SystemInfo.supportsGyroscope;
		intervalTimer.Interval("supportsGyroscope");
		supportsInstancing = SystemInfo.supportsInstancing;
		intervalTimer.Interval("supportsInstancing");
		supportsLocationService = SystemInfo.supportsLocationService;
		intervalTimer.Interval("supportsLocationService");
		supportsShadows = SystemInfo.supportsShadows;
		intervalTimer.Interval("supportsShadows");
		supportsSparseTextures = SystemInfo.supportsSparseTextures;
		intervalTimer.Interval("supportsSparseTextures");
		supportsVibration = SystemInfo.supportsVibration;
		intervalTimer.Interval("supportsVibration");
		systemMemorySize = SystemInfo.systemMemorySize;
		intervalTimer.Interval("systemMemorySize");

		intervalTimer.Stop(!Application.isEditor);
	}
}

#endif