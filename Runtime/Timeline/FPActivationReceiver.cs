namespace FuzzPhyte.Utility.Animation.Timeline
{
    using UnityEngine;
    using System.Linq;
    using UnityEngine.Timeline;
    using System.Collections.Generic;
    using UnityEngine.Playables;
    public class FPActivationReceiver : MonoBehaviour,INotificationReceiver
    {
        [Tooltip("If true, notifications are applied while scrubbing/evaluating in editor too.")]
        public bool ApplyDuringEvaluate = true;
        public PlayableDirector Director;

        public bool DefaultActiveWhenNoMarker = false;

        #region Public Methods for Editor Scrubbing
        /// <summary>
        /// Deterministically rebuild activation state up to a time (Editor scrubbing).
        /// Resets touched targets to their initial active state, then applies markers <= time.
        /// </summary>
        public void RebuildToTime(double timeSeconds)
        {
            if (Director == null) return;
            if (Director.playableAsset is not UnityEngine.Timeline.TimelineAsset timeline) return;

            // Map: target -> last command marker time <= timeSeconds
            var lastMarkerForTarget = new Dictionary<int, FPActivationMarker>();
            var targetLookup = new Dictionary<int, GameObject>();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not FPActivationTrack) continue;

                foreach (var m in track.GetMarkers())
                {
                    if (m is not FPActivationMarker marker) continue;
                    if (!marker.ApplyDuringEvaluate) continue;

                    var target = ResolveTarget(Director, marker);
                    if (target == null) continue;

                    int id = target.GetInstanceID();
                    targetLookup[id] = target;

                    // Only markers at or before timeSeconds participate
                    if (marker.time > timeSeconds) continue;

                    if (!lastMarkerForTarget.TryGetValue(id, out var existing) || marker.time >= existing.time)
                    {
                        lastMarkerForTarget[id] = marker;
                    }
                }
            }

            // Apply deterministic state per target
            foreach (var kvp in targetLookup)
            {
                int id = kvp.Key;
                var target = kvp.Value;

                if (lastMarkerForTarget.TryGetValue(id, out var marker))
                {
                    // Last marker wins: Activate => true, Deactivate => false
                    bool next = marker.Command == FPActivationCommand.Activate;
                    target.SetActive(next);
                }
                else
                {
                    // No markers before this time => default state
                    target.SetActive(DefaultActiveWhenNoMarker);
                }
            }
        }
        #endregion

        /// <summary>
        /// INotificationReceiver implementation
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="notification"></param>
        /// <param name="context"></param>
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            
            //var director = context as PlayableDirector ?? GetComponentInParent<PlayableDirector>();
            if (notification is not FPActivationMarker marker) return;

            //var director = context as PlayableDirector;
           
            if (Director == null) return;

            // Gate evaluate-time application (scrubbing/preview/paused).
            // When the director isn't actively "Playing", treat as evaluation mode.
            bool isPlaying = Director.state == PlayState.Playing;
            // Gate evaluate-time application (scrubbing/preview/paused).
            if (!isPlaying)
            {
                if (!ApplyDuringEvaluate || !marker.ApplyDuringEvaluate)
                {
                    
                    return;
                }
            }

            

            GameObject target = ResolveTarget(Director, marker);

            if (target == null)
            {
                
                return;
            }

            ApplyCommand(target, marker);
        }

        protected GameObject ResolveTarget(PlayableDirector director, FPActivationMarker marker)
        {
            if (director == null) return null;

            // 1) Track binding target (Activate Track-style)
            if (marker.UseTrackBindingTarget)
            {
                var track = marker.parent; // Marker -> parent TrackAsset
                if (track != null)
                {
                    var binding = director.GetGenericBinding(track);

                    if (binding is GameObject go)
                    {
                        
                        return go;
                    }

                    if (binding is Component c)
                    {
                       
                        return c.gameObject;
                    }    
                }
                

                // Fallback: if user enabled track binding but didn’t bind, try override (if set)
                var fallback = marker.OverrideTarget.Resolve(director);
                if (fallback != null)
                {
                    return fallback;
                }

                return null;
            }

            // 2) Per-marker override target
            var overrideTarget = marker.OverrideTarget.Resolve(director);

            return overrideTarget;
        }

        
        protected void ApplyCommand(GameObject target, FPActivationMarker marker)
        {
            int key = target.GetInstanceID();

            bool current = target.activeSelf;
            
            bool next = current;
            switch (marker.Command)
            {
                case FPActivationCommand.Activate:
                    next = true;
                    break;
                case FPActivationCommand.Deactivate:
                    next = false;
                    break;
            }

            target.SetActive(next);
        }
    }
}
