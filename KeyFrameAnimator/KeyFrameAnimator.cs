using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace Rocky.Systems.KeyFrameAnimations
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class KeyFrameAnimator : MonoBehaviour
    {
        [SerializeField] private KeyFrameAnimation _animation;

        private SpriteRenderer _renderer;
        private int _animatorID = -1;

        public SpriteRenderer Renderer { get => _renderer; }
        public KeyFrameAnimation Animation { get => _animation; }
        public int AnimatorID { get => _animatorID; }

        public bool SetAnimation(KeyFrameAnimation newAnimation, int startingFrame, bool maintainTiming)
        {
            // I don't know what purpose this check served
            //if (newAnimation.MaxFrameIndex != _animation.MaxFrameIndex)
                //return false;

            if (_animatorID == -1)
            {
                Initialize();
            }
            this._animation = newAnimation;
            KeyFrameAnimationFacade.ChangeAnimations(this, startingFrame, maintainTiming);
            return true;
        }

        public void Pause()
        {
            KeyFrameAnimationFacade.Pause(_animatorID);
            return;
        }

        public bool Resume()
        {
            KeyFrameAnimationFacade.Resume(_animatorID);
            return true;
        }

        public void Initialize()
        {
            if (_animatorID == -1)
            {
                _animatorID = KeyFrameAnimationFacade.GetAnimatorID();
                KeyFrameAnimationFacade.AddToCollections(this);
            }
        }

        private void Awake()
        {
            // The "KeyFrameAnimationSystem" needs to run some code in Awake, so don't access it until Start() to avoid a race condition

            _renderer = GetComponent<SpriteRenderer>();
        }


        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            KeyFrameAnimationFacade.RemoveFromCollections(this);
            KeyFrameAnimationFacade.ReturnAnimatorID(_animatorID);
        }
    }
}