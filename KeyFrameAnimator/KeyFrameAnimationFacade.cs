using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Rocky.Systems.KeyFrameAnimations
{
    public static class KeyFrameAnimationFacade
    {
        public static bool Enabled { get => _enabled; }
        private static KeyFrameAnimationSystem _instance = null;
        private static bool _enabled = false;
        private static UniqueIDPool _idPool = new UniqueIDPool();

        public static void ChangeAnimations(KeyFrameAnimator animator, int startingFrame, bool maintainTiming)
        {
            if (_enabled)
                _instance.ChangeAnimation(animator, startingFrame, maintainTiming);
        }

        public static void Pause(int id)
        {
            if (_enabled)
                _instance.PauseAnimator(id);
        }

        public static void Resume(int id)
        {
            if (_enabled)
                _instance.ResumeAnimator(id);
        }

        public static bool AddToCollections(KeyFrameAnimator animator)
        {
            if (animator.AnimatorID == -1)
            {
                Debug.LogError("Animator " + animator.name + " has no animatorID set!");
                return false;
            }
            if (animator.Animation == null)
            {
                Debug.LogError("Animator " + animator.name + " has no animation!");
                return false;
            }
            if (_enabled)
                return _instance.AddToCollections(animator);
            return true;
        }

        public static void RemoveFromCollections(KeyFrameAnimator animator)
        {
            if (animator == null)
            {
                Debug.LogWarning("tried removing a null animator!");
                return;
            }
            if (_enabled)
                _instance.RemoveFromCollections(animator);
        }

        public static int GetAnimatorID()
        {
            return _idPool.BorrowID();
        }

        public static void ReturnAnimatorID(int id)
        {
            _idPool.ReturnID(id);
        }

        public static void SetInstance(KeyFrameAnimationSystem newInstance)
        {
            _instance = newInstance;
            _enabled = true;
        }

        public static void RemoveInstance()
        {
            _instance = null;
            _enabled = false;
        }
    }
}
