namespace FuzzPhyte.Utility.Animation.Timeline
{
    using UnityEngine;
    using System;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [Serializable]
    public class FPActivationMarker : Marker, INotification,INotificationOptionProvider
    {
        // Required by INotification
        [SerializeField] private PropertyName _id = new PropertyName("FPActivationMarker");
        public PropertyName id => _id;

        [Header("Command")]
        public FPActivationCommand Command = FPActivationCommand.Activate;

        [Header("Targeting")]
        [Tooltip("If true, use the track binding GameObject as the target.")]
        public bool UseTrackBindingTarget = true;

        [Tooltip("If false, this marker will use OverrideTarget (ExposedReference).")]
        public ExposedReference<GameObject> OverrideTarget;

        [Header("Sticky State")]
        [Tooltip("If true, remember the last state set for this target (useful when scrubbing).")]
        public bool Sticky = true;

        [Header("Scrub / Evaluate Behavior")]
        [Tooltip("If true, this marker can apply when the timeline is evaluated (scrub/preview), not just played.")]
        public bool ApplyDuringEvaluate = true;

        NotificationFlags INotificationOptionProvider.flags
        {
            get
            {
                if (!ApplyDuringEvaluate) return 0;
                return NotificationFlags.TriggerInEditMode | NotificationFlags.Retroactive;
            }
        }
    }
}
