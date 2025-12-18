namespace FuzzPhyte.Utility.Animation.Editor
{
    using UnityEditor;
    using UnityEditor.Timeline;
    using UnityEngine;
    using UnityEngine.Playables;

    [InitializeOnLoad]
    public static class FPActivationTimelineScrubDriver
    {
        private static PlayableDirector _lastDirector;
        private static double _lastTime = double.NaN;

        // Helps avoid hammering rebuilds when Timeline repaints
        private const double kEpsilon = 0.0001;

        static FPActivationTimelineScrubDriver()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            // Only works if the Timeline window is open
            var window = TimelineEditor.GetWindow();
            if (window == null) return;

            var director = TimelineEditor.inspectedDirector;
            if (director == null) return;

            // Query current playhead time from the Timeline window playback controls
            double time;
            try
            {
                time = window.playbackControls.GetCurrentTime();
            }
            catch
            {
                // Window can be in a transient state (domain reload/closing)
                return;
            }

            bool directorChanged = director != _lastDirector;
            bool timeChanged = double.IsNaN(_lastTime) || System.Math.Abs(time - _lastTime) > kEpsilon;

            if (!directorChanged && !timeChanged)
                return;

            _lastDirector = director;
            _lastTime = time;

            RebuildAllReceiversForDirector(director, time);
        }

        private static void RebuildAllReceiversForDirector(PlayableDirector director, double time)
        {
            // Find receivers in loaded scenes (including inactive)
            var receivers = Object.FindObjectsByType<FuzzPhyte.Utility.Animation.Timeline.FPActivationReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach (var r in receivers)
            {
                if (r == null) continue;


                if (r.Director != director) continue;

                if (!r.ApplyDuringEvaluate) continue;

                r.RebuildToTime(time);
            }
        }
    }
}
