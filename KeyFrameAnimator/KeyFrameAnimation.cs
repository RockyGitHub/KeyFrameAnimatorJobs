using System;
using UnityEngine;


namespace Rocky.Systems.KeyFrameAnimations
{
    [CreateAssetMenu(fileName = "New Key Frame Animation", menuName = "Scriptable Objects/Key Frame Animation", order = 1)]
    public class KeyFrameAnimation : ScriptableObject
    {
        [SerializeField] private KeyFrameData[] _frames;
        [SerializeField] private UInt16 _startingFrame;
        [SerializeField] private AnimationStyle _style;
        [SerializeField] private UInt16 loopStartFrame;
        [SerializeField] private int loopEndFrame;

        [SerializeField] private KeyFrameAnimationEvent[] _animEvents;

        public AnimationStyle Style { get => _style; }
        public UInt16 LoopStartFrame { get => loopStartFrame; }
        public UInt16 MaxEventIndex { get => _animEvents.Length == 0 ? (UInt16)0 : (UInt16)(_animEvents.Length - 1); }
        public UInt16 MaxFrameIndex { get => _frames.Length == 0 ? (UInt16)0 : (UInt16)(_frames.Length - 1); }
        public UInt16 StartingFrameIndex { get => _startingFrame; }


        // ----------- INTERFACE FUNCTIONS --------------
        #region Interface
        public Sprite GetFrame(int index)
        {
            if (index > _frames.Length - 1) // - 1 because a length of 3 goes to frames[2].
                return null;
            return _frames[index].Frame;
        }

        // Gets the frame that should occur *after* the index that is passed in
        public Sprite GetNextFrame(int index)
        {
            Sprite nextFrameSprite = null;
            switch (_style)
            {
                case AnimationStyle.Loop:
                    if ((index + 1) > loopEndFrame)
                    {
                        nextFrameSprite = _frames[loopStartFrame].Frame;
                    }
                    else
                    {
                        nextFrameSprite = _frames[index + 1].Frame;
                    }
                    break;
                case AnimationStyle.YoYo:
                    break;
                case AnimationStyle.OneShot:
                    break;
                default:
                    break;
            }
            return nextFrameSprite;
        }

        // Returns the time this frame occurs referencing time0
        public float GetFrameTimeFromStart(int index)
        {
            float timeSum = 0;
            if (index > _frames.Length - 1)
                return 0;
            for (int i = 0; i < index; i++)
            {
                timeSum += GetFrameDuration(i);
            }
            return timeSum;
        }

        public float GetFrameDuration(int index)
        {
            if (index > _frames.Length - 1)
                return 0;
            return _frames[index].FrameDurationInS;
        }

        public float GetEventTimeFromStart(int index)
        {
            if (_animEvents.Length == 0)
                return float.MaxValue;
            if (index > _animEvents.Length - 1)
                return -1;
            return _animEvents[index].TimeToInvokeInS;
        }

        // Returns the next Event time.  Time reference from the frameIndex passed in
        public float GetNextEventTimeAfterFrame(int frameIndex)
        {
            if (frameIndex > _frames.Length - 1)
                return -1;
            if (_animEvents.Length == 0)
                return float.MaxValue;

            var frameTime = GetFrameTimeFromStart(frameIndex);

            // Look for the animation event who's time exceeds that of the frame's time we just got
            for (int i = 0; i < _animEvents.Length; i++)
            {
                if (_animEvents[i].TimeToInvokeInS > frameTime)
                    return _animEvents[i].TimeToInvokeInS;
            }
            // if we didn't find any, we're passed all events, so let's get the first one
            return _animEvents[0].TimeToInvokeInS;
        }

        public float GetEventDuration(int index)
        {
            if (index > _animEvents.Length - 1)
                return -1;
            if (index == 0)
                return _animEvents[index].TimeToInvokeInS;
            return _animEvents[index].TimeToInvokeInS - _animEvents[index - 1].TimeToInvokeInS;
        }

        public KeyFrameAnimationEvent GetEvent(int index)
        {
            if (index > _animEvents.Length - 1)
                return null;
            return _animEvents[index];
        }
        public KeyFrameAnimationEvent GetEvent(string eventName)
        {
            for (int i = 0; i < _animEvents.Length; i++)
            {
                if (string.Compare(eventName, _animEvents[i].Name) == 0)
                    return _animEvents[i];
            }
            return null;
        }

        public void InvokeAnimationEvent(int index)
        {
            if (index > _animEvents.Length - 1)
                return;
            _animEvents[index].InvokeEvent();
        }
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_startingFrame > _frames.Length)
                _startingFrame = (UInt16)_frames.Length;
        }
#endif
    }
}