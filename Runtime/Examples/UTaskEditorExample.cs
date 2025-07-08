#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using THEBADDEST.Tasks;

namespace THEBADDEST.Tasks.Examples
{
    [InitializeOnLoad]
    public static class UTaskEditorExample
    {
        static UTaskEditorExample()
        {
            // Only run in editor and not in play mode
            if (!Application.isPlaying)
            {
                Debug.Log("[UTaskEditorExample] Scheduling UTask in editor mode...");
                UTaskScheduler.Schedule(() =>
                {
                    Debug.Log($"[UTaskEditorExample] UTask executed in editor! Time: {DateTime.Now:HH:mm:ss.fff}");
                });

                // Schedule a delayed task
                UTask.Delay(1f).ContinueWith(() =>
                {
                    Debug.Log($"[UTaskEditorExample] Delayed UTask executed in editor! Time: {DateTime.Now:HH:mm:ss.fff}");
                });
            }
        }
    }
}
#endif