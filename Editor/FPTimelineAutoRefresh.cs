namespace FuzzPhyte.Utility.Animation.Editor
{
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Playables;
    using System.Linq;
    using UnityEditor.SceneManagement;
    using UnityEngine.Timeline;

    /*
    [InitializeOnLoad]
    public static class FPTimelineAutoRefresh
    {
        const string kBootKey = "FP_TimelineBootRefresh_DidBoot";
        static FPTimelineAutoRefresh()
        {
            //first idle after editor launch
            EditorApplication.update += OnFirstEditorIdle;
            //scene is opened
            EditorSceneManager.sceneOpened += (_, __) => RefreshOpenSceneTimelines();
            //after scripts reload
            AssemblyReloadEvents.afterAssemblyReload += RefreshOpenSceneTimelines;
            //return to edit mode
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    RefreshOpenSceneTimelines();
                }
            };
        }
        // refresh all
        [MenuItem("FuzzPhyte/Animation/Spline Follow Timeline/Refresh All Directors", priority = 400)]
        public static void RefreshAllDirectors()
        {
            var directors = Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            foreach (var d in directors) RebuildAndEvaluate(d);

            // Nudge Timeline window (if open)
            TryTimelineEditorRefresh();
        }
        /// <summary>
        /// refresh selected
        /// </summary>
        [MenuItem("FuzzPhyte/Animation/Spline Follow Timeline/Refresh Selected Director", priority = 401)]
        public static void RefreshSelected()
        {
            var dir = Selection.activeGameObject
                ? Selection.activeGameObject.GetComponentInParent<PlayableDirector>(true)
                : null;
            if (dir) RebuildAndEvaluate(dir);
            TryTimelineEditorRefresh();
        }
        
        static void OnFirstEditorIdle()
        {
            if (SessionState.GetBool(kBootKey, false))
            {
                EditorApplication.update -= OnFirstEditorIdle;
                return;
            }

            // Defer one more tick to ensure windows (incl. Timeline) are initialized
            EditorApplication.delayCall += () =>
            {
                RefreshOpenSceneTimelines();
                SessionState.SetBool(kBootKey, true);
            };

            EditorApplication.update -= OnFirstEditorIdle;
        }

        static void RefreshOpenSceneTimelines()
        {
            // Find all directors in loaded scenes (incl. disabled)
            //var directors = Object.FindObjectsOfType<PlayableDirector>(true);
            var directors = Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
            foreach (var dir in directors)
            {
                if (!dir || dir.playableAsset == null) continue;

                // Re-import the asset to ensure types are loaded (safe & cheap)
                var path = AssetDatabase.GetAssetPath(dir.playableAsset);
                if (!string.IsNullOrEmpty(path))
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                dir.RebuildGraph();  // rebuild playable graph
                dir.Evaluate();      // display current frame in the editor
            }
        }
        #region Utilities
        // -------- Utilities --------
        static void RebuildAndEvaluate(PlayableDirector dir)
        {
            if (!dir || dir.playableAsset == null) return;

            // Make sure the asset is imported (type resolution at startup)
            var path = AssetDatabase.GetAssetPath(dir.playableAsset);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            dir.RebuildGraph();
            dir.Evaluate();
        }

        static double GetLastClipEnd(TimelineAsset asset)
        {
            double last = 0;
            foreach (var track in asset.GetOutputTracks())
                foreach (var clip in track.GetClips())
                    if (clip.end > last) last = clip.end;
            return last;
        }
        static void TryTimelineEditorRefresh()
        {
            try
            {
                var tEditor = System.Type.GetType("UnityEditor.Timeline.TimelineEditor, Unity.Timeline.Editor");
                var refresh = tEditor?.GetMethod("Refresh",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                refresh?.Invoke(null, null);
            }
            catch {  }
        }

        #endregion
    }
          */
#endif
}


