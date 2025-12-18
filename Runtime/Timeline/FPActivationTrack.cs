namespace FuzzPhyte.Utility.Animation.Timeline
{
    using UnityEngine;
    using UnityEngine.Timeline;

    public enum FPActivationCommand
    {
        Activate = 0,
        Deactivate = 1
    }

    // the director track binding is a game object = default target
    [TrackBindingType(typeof(FPActivationReceiver))]
    [TrackColor(0.15f,0.55f,0.95f)]
    public class FPActivationTrack:MarkerTrack
    {
       
    }
}
