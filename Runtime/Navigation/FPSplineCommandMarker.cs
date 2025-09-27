namespace FuzzPhyte.Utility.Animation
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Splines;
    using UnityEngine.Timeline;

    [System.Serializable]
    public enum FPSplineCommand
    {
        Pause,
        Resume,
        Stop,
        Unstop,
        SetSpeedMultiplier,
        WarpToT,        // immediate jump (teleport)
        SetT,           // sets current t respecting pause/stop gates
        NewSpline
    }
    /// <summary>
    /// Setting up the marker for our spline follower timeline portion
    /// </summary>
    [System.Serializable]
    public class FPSplineCommandMarker : Marker,INotification
    {
        public FPSplineCommand command = FPSplineCommand.Pause;
        [Tooltip("Optional float payload (e.g., speed multiplier or t).")]
        public float value = 1f;

        [Space]
        [Tooltip("For NewSpline Command")]
        public int SplineContainerIndex = 0;
        public PropertyName id => new PropertyName(nameof(FPSplineCommandMarker));

        [Space]
        [Tooltip("If you pass an animation clip, and we find an animator, we will inject this")]
        public AnimationClip CommandAnimClip;
        [Tooltip("If we want to return to the other clip we are writing over")]
        public bool ReturnToOriginalClip = false;
        [Tooltip("If this command wants to stop an injection")]
        public bool StopActiveInjection;
    }
}
