namespace FuzzPhyte.Utility.Animation
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Splines;
    using System.Collections.Generic;
    /// <summary>
    /// Put this on the same GameObject as the PlayableDirector or anywhere reachable; 
    /// then assign it as a notification receiver on your FP_SplineFollowTrack in Timeline.
    /// </summary>
    public class FPSplineFollowerReceiver : MonoBehaviour, INotificationReceiver
    {
        [Tooltip("Target follower; if null, tries to find from the Playable's userData (track binding).")]
        public FPSplineFollower target;
        public List<SplineContainer> OrderedSplineContainers = new();
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is FPSplineCommandMarker cmd)
            {
                var follower = target;

                // Try to pull bound object if not explicitly set
                if (follower == null && context is PlayableDirector dir)
                {
                    // If this receiver is added to the same track in Timeline,
                    // Unity passes the bound object as context via the track.
                    // If not, you can manually assign 'target' in the inspector.
                    // (Different Unity versions differ in how context comes through
                    // for receivers. Being explicit is safer.)
                }

                if (follower == null) return;

                switch (cmd.command)
                {
                    case FPSplineCommand.Pause: follower.Pause(cmd.CommandAnimClip,cmd.ReturnToOriginalClip); break;
                    case FPSplineCommand.Resume: follower.Resume(cmd.CommandAnimClip, cmd.ReturnToOriginalClip, cmd.StopActiveInjection); break;
                    case FPSplineCommand.Stop: follower.Stop(cmd.CommandAnimClip,cmd.ReturnToOriginalClip); break;
                    case FPSplineCommand.Unstop: follower.Unstop(); break;
                    case FPSplineCommand.SetSpeedMultiplier: follower.SetSpeedMultiplier(cmd.value); break;
                    case FPSplineCommand.WarpToT: follower.WarpToNormalizedT(cmd.value); break;
                    case FPSplineCommand.SetT: follower.SetNormalizedT(cmd.value); break;
                    case FPSplineCommand.NewSpline:
                        if (cmd.SplineContainerIndex < OrderedSplineContainers.Count)
                        {
                            follower.HotSplineSwap(OrderedSplineContainers[cmd.SplineContainerIndex], cmd.value);
                        }
                        
                        break;
                }
            }
        }
    }
}
