using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class DouyinSDKManager : MonoBehaviour
{
    // These functions link to a .jslib file in your Plugins folder
    // e.g. Plugins/DouyinBridge.jslib
    
    [DllImport("__Internal")]
    private static extern void DouyinStartRecording();

    [DllImport("__Internal")]
    private static extern void DouyinStopRecording();

    [DllImport("__Internal")]
    private static extern void DouyinShareVideo();

    [DllImport("__Internal")]
    private static extern void DouyinShowRewardedAd();

    private Action onAdSuccess;
    private Action onAdFail;

    public void StartRecording()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DouyinStartRecording();
#else
        Debug.Log("[Mock] Douyin Recording Started");
#endif
    }

    public void StopRecording()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DouyinStopRecording();
#else
        Debug.Log("[Mock] Douyin Recording Stopped");
#endif
    }

    public void ShareVideo()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        DouyinShareVideo();
#else
        Debug.Log("[Mock] Douyin Video Shared");
#endif
    }

    public void ShowRewardedAd(Action onSuccess, Action onFail)
    {
        onAdSuccess = onSuccess;
        onAdFail = onFail;

#if UNITY_WEBGL && !UNITY_EDITOR
        DouyinShowRewardedAd();
#else
        Debug.Log("[Mock] Ad Watched Successfully");
        OnAdCompleted("success"); // Mock callback
#endif
    }

    // Called from JS via SendMessage("DouyinSDKManager", "OnAdCompleted", "success");
    public void OnAdCompleted(string result)
    {
        if (result == "success")
        {
            onAdSuccess?.Invoke();
        }
        else
        {
            onAdFail?.Invoke();
        }
    }
}
