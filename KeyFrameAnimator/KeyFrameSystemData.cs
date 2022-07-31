using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Rocky.Systems.KeyFrameAnimations
{
    [System.Serializable]
    public struct KeyFrameData
    {
        public Sprite Frame;
        public float FrameDurationInS;
    }

    [System.Serializable]
    public class KeyFrameAnimationEvent
    {
        public event Action Event;
        public string Name;
        public float TimeToInvokeInS;

        public void InvokeEvent()
        {
            Event?.Invoke();
        }
    }

    public struct KeyFrameJobData
    {
        public KeyFrameJobData(int animatorID, AnimationStyle style, UInt16 startingFrameIndex, UInt16 maxFrameIndex, UInt16 loopStartFrame, float nextFrameTime, UInt16 maxEventIndex, float nextEventTime)
        {
            RendererID = animatorID;
            LastTimeUpdated = -1f;              // -1 indiciates this has never been updated
            CurrentFrameIndex = startingFrameIndex;
            Style = style;
            YoYoBackwards = false;
            Pause = false;
            MaxFrameIndex = maxFrameIndex;
            LoopStartFrameIndex = loopStartFrame;
            NextFrameTime = nextFrameTime;      // set to float.max if there are no more frames                
            MaxEventIndex = maxEventIndex;
            NextEventIndex = 0;
            NextEventTime = nextEventTime;      // Set to float.max if there are no events
        }

        // Unique Identifer to the animator
        public int RendererID;
        // Time
        public float LastTimeUpdated;           // Used to track the Time.unscaledTime that this struct was last updated

        public UInt16 CurrentFrameIndex;        // Incremented in the job

        // Next frame index logic variables
        public AnimationStyle Style;
        public bool YoYoBackwards;             // False if the animation is playing forwards, True if the animation is playing backwards (yoyo)
        public bool Pause;
        public UInt16 MaxFrameIndex;            // Length of the sprite set -1.  Used to determine when to yoyo, bounce, or stop.
        public UInt16 LoopStartFrameIndex;         // The index that a loop animation should reset to
        public float NextFrameTime;             // Marks the point at which we should increment CurrentFrameIndex.  Must be added to when that action is performed

        // Next event logic variables
        public UInt16 MaxEventIndex;
        public UInt16 NextEventIndex;           // Incremented in the job
        public float NextEventTime;             // Marks the point at which we should increment NextEventIndex.  Must be added to when that action is performed
    };

    public struct KeyFrameJob_FrameDataFromJob
    {
        public KeyFrameJob_FrameDataFromJob(int uniqueID, UInt16 newFrameIndex)
        {
            AnimatorID = uniqueID;
            NewFrameIndex = newFrameIndex;
        }

        public int AnimatorID;
        public UInt16 NewFrameIndex;
    }

    public struct KeyFrameJob_EventDataFromJob
    {
        public KeyFrameJob_EventDataFromJob(int uniqueID, UInt16 newEventIndex)
        {
            AnimatorID = uniqueID;
            NewEventIndex = newEventIndex;
        }

        public int AnimatorID;
        public UInt16 NewEventIndex;
    }

    public struct ChangedAnimationData
    {
        public KeyFrameAnimator Animator;
        public int StartingFrame;
    }

    public enum AnimationStyle
    {
        Loop,
        YoYo,
        OneShot,
    }
}
