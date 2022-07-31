using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


namespace Rocky.Systems.KeyFrameAnimations
{
    public class KeyFrameAnimationSystem : MonoBehaviour
    {
        public static KeyFrameAnimationSystem Instance;

        private JobHandle _keyFrameJobHandle;
        private AnimationSystemJob _keyFrameJobLoop;

        // These two collections are one to one, in that the index of their list should be meaningfully the same thing.
        // I just can't pass references into the job system.  So maybe these two should belong in their own class, as its own custom collection
        public List<KeyFrameAnimator> Animators = new List<KeyFrameAnimator>();
        public NativeList<KeyFrameJobData> DataForJob;

        private NativeQueue<KeyFrameJob_FrameDataFromJob> NewFrameDataFromJob; 
        private NativeQueue<KeyFrameJob_EventDataFromJob> NewEventDataFromJob;

        private Queue<KeyFrameJobData> NewAnimators = new Queue<KeyFrameJobData>();
        private Queue<ChangedAnimationData> ChangedAnimations = new Queue<ChangedAnimationData>();
        private Queue<KeyFrameAnimator> ChangedAnimationsSync = new Queue<KeyFrameAnimator>();

        private void Update()
        {
            _keyFrameJobLoop = new AnimationSystemJob()
            {
                CurrentTime = Time.unscaledTime,
                DataforJob = DataForJob,

                Out_FrameDataFromJob = NewFrameDataFromJob.AsParallelWriter(),
                Out_EventDataFromJob = NewEventDataFromJob.AsParallelWriter()
            };

            _keyFrameJobHandle = _keyFrameJobLoop.Schedule(Animators.Count, 1);
        }

        [BurstCompile]
        private struct AnimationSystemJob : IJobParallelFor
        {

            [ReadOnly]
            public float CurrentTime;   // set by Time.time

            public NativeArray<KeyFrameJobData> DataforJob;

            // returned data
            [WriteOnly]
            public NativeQueue<KeyFrameJob_FrameDataFromJob>.ParallelWriter Out_FrameDataFromJob;
            [WriteOnly]
            public NativeQueue<KeyFrameJob_EventDataFromJob>.ParallelWriter Out_EventDataFromJob;

            public void Execute(int index)
            {
                var keyFrameData = DataforJob[index];

                // If this has never been updated yet, update a variable and just return right away
                if (keyFrameData.LastTimeUpdated < 0)
                {
                    keyFrameData.LastTimeUpdated = CurrentTime;
                    DataforJob[keyFrameData.RendererID] = keyFrameData;
                    return;
                }

                if (keyFrameData.Pause == true)
                {
                    return;
                }

                float timeSinceLastUpdate = CurrentTime - keyFrameData.LastTimeUpdated;
                keyFrameData.NextFrameTime -= timeSinceLastUpdate;
                keyFrameData.NextEventTime -= timeSinceLastUpdate;

                switch (keyFrameData.Style)
                {
                    case AnimationStyle.Loop:
                        #region Loop

                        if (keyFrameData.NextFrameTime < 0)
                        {
                            if (keyFrameData.CurrentFrameIndex == keyFrameData.MaxFrameIndex)
                                keyFrameData.CurrentFrameIndex = keyFrameData.LoopStartFrameIndex;
                            else
                                keyFrameData.CurrentFrameIndex += 1;
                            Out_FrameDataFromJob.Enqueue(new KeyFrameJob_FrameDataFromJob(keyFrameData.RendererID, keyFrameData.CurrentFrameIndex));
                        }
                        if (keyFrameData.NextEventTime < 0) // Note: If there are no events in the animation, NextEventTime will be float.max
                        {
                            Out_EventDataFromJob.Enqueue(new KeyFrameJob_EventDataFromJob(keyFrameData.RendererID, keyFrameData.NextEventIndex));
                            if (keyFrameData.NextEventIndex == keyFrameData.MaxEventIndex)
                                keyFrameData.NextEventIndex = 0;
                            else
                                keyFrameData.NextEventIndex += 1;
                        }

                        #endregion
                        break;
                    case AnimationStyle.YoYo:
                        #region YoYo
                        if (keyFrameData.NextFrameTime < 0)
                        {
                            if (keyFrameData.YoYoBackwards == false)
                            {
                                if (keyFrameData.CurrentFrameIndex == keyFrameData.MaxFrameIndex)
                                {
                                    keyFrameData.CurrentFrameIndex -= 1;
                                    keyFrameData.YoYoBackwards = true;
                                }
                                else
                                    keyFrameData.CurrentFrameIndex += 1;
                            }
                            else
                            {
                                if (keyFrameData.CurrentFrameIndex == keyFrameData.LoopStartFrameIndex)
                                {
                                    keyFrameData.CurrentFrameIndex += 1;
                                    keyFrameData.YoYoBackwards = false;
                                }
                                else
                                    keyFrameData.CurrentFrameIndex -= 1;
                            }
                            Out_FrameDataFromJob.Enqueue(new KeyFrameJob_FrameDataFromJob(keyFrameData.RendererID, keyFrameData.CurrentFrameIndex));
                        }
                        if (keyFrameData.NextEventTime < 0) // Note: If there are no events in the animation, NextEventTime will be float.max
                        {
                            Out_EventDataFromJob.Enqueue(new KeyFrameJob_EventDataFromJob(keyFrameData.RendererID, keyFrameData.NextEventIndex));
                            if (keyFrameData.NextEventIndex == keyFrameData.MaxEventIndex)
                                keyFrameData.NextEventIndex = 0;
                            else
                                keyFrameData.NextEventIndex += 1;
                        }
                        #endregion
                        break;
                    case AnimationStyle.OneShot:
                        #region OneShot

                        if (keyFrameData.CurrentFrameIndex != keyFrameData.MaxFrameIndex)
                        {
                            if (keyFrameData.NextFrameTime < 0)
                            {
                                keyFrameData.CurrentFrameIndex += 1;
                                Out_FrameDataFromJob.Enqueue(new KeyFrameJob_FrameDataFromJob(keyFrameData.RendererID, keyFrameData.CurrentFrameIndex));
                            }
                        }

                        if (keyFrameData.NextEventIndex != keyFrameData.MaxEventIndex)
                        {
                            if (keyFrameData.NextEventTime < 0)
                            {
                                Out_EventDataFromJob.Enqueue(new KeyFrameJob_EventDataFromJob(keyFrameData.RendererID, keyFrameData.NextEventIndex));
                                keyFrameData.NextEventIndex += 1;
                            }
                        }

                        #endregion
                        break;
                    default:
                        Debug.LogError("Default case hit, this should not be possible!");
                        break;
                }

                keyFrameData.LastTimeUpdated = CurrentTime;
                DataforJob[keyFrameData.RendererID] = keyFrameData;
            }
        }

        private void LateUpdate()
        {
            _keyFrameJobHandle.Complete();

            // Check the job data to see if we have any renderers to update
            while (NewFrameDataFromJob.Count > 0)
            {
                var frameData = NewFrameDataFromJob.Dequeue();
                var animator = Animators[frameData.AnimatorID];

                if (animator == null)
                    break;

                var animation = animator.Animation;

                // Set the new sprite
                animator.Renderer.sprite = animation.GetFrame(frameData.NewFrameIndex);

                // Set NextFrameTime
                var thing = DataForJob[frameData.AnimatorID];
                thing.NextFrameTime += animation.GetFrameDuration(frameData.NewFrameIndex);
                DataForJob[frameData.AnimatorID] = thing;
            }

            while (NewEventDataFromJob.Count > 0)
            {
                // call events
                var eventData = NewEventDataFromJob.Dequeue();
                var animator = Animators[eventData.AnimatorID];

                if (animator == null)
                    break;

                // invoke the animation event
                animator.Animation.InvokeAnimationEvent(eventData.NewEventIndex);

                // Set NextEventTime
                var thing = DataForJob[eventData.AnimatorID];
                thing.NextEventTime += animator.Animation.GetEventTimeFromStart(eventData.NewEventIndex);
                DataForJob[eventData.AnimatorID] = thing;
            }

            // Check a list of animators that had their animation changed
            // Process the list of ChangedAnimations
            while (ChangedAnimations.Count > 0)
            {
                var newAnimData = ChangedAnimations.Dequeue();
                var animator = newAnimData.Animator;

                if (animator == null)
                    break;

                var newAnimation = animator.Animation;

                if (newAnimation != null)
                    animator.Renderer.sprite = animator.Animation.GetFrame(newAnimData.StartingFrame);
                else
                    animator.Renderer.sprite = null;

                var thing = new KeyFrameJobData(
                    animator.AnimatorID, newAnimation.Style, (UInt16)newAnimData.StartingFrame,
                    newAnimation.MaxFrameIndex, newAnimation.LoopStartFrame, newAnimation.GetFrameDuration(newAnimData.StartingFrame),
                newAnimation.MaxEventIndex, newAnimation.GetNextEventTimeAfterFrame(newAnimData.StartingFrame));
                DataForJob[animator.AnimatorID] = thing;
            }

            // Check for animations that changed and want to be sync'd
            // Their events will need to be modified
            while (ChangedAnimationsSync.Count > 0)
            {
                var animator = ChangedAnimationsSync.Dequeue();

                if (animator == null)
                    break;

                var thing = DataForJob[animator.AnimatorID];
                thing.NextEventTime = animator.Animation.GetNextEventTimeAfterFrame(thing.CurrentFrameIndex);
                thing.NextEventTime -= thing.NextFrameTime; // yay figured out how to make it accurate :)
                thing.MaxEventIndex = animator.Animation.MaxEventIndex;

                thing.NextFrameTime += animator.Animation.GetFrameTimeFromStart(0);
                if (thing.CurrentFrameIndex > animator.Animation.MaxFrameIndex)
                {
                    thing.CurrentFrameIndex = 0;
                }
                thing.MaxFrameIndex = animator.Animation.MaxFrameIndex;

                thing.Style = animator.Animation.Style;

                DataForJob[animator.AnimatorID] = thing;
            }

            while (NewAnimators.Count > 0)
            {
                DataForJob.Add(NewAnimators.Dequeue());
            }
        }


        private void FirstInitialize()
        {
            if (KeyFrameAnimationFacade.Enabled)
            {
                Debug.LogError("Multiple KeyFrameAnimationSystem instances found. This new instance will be destroyed.");
                Destroy(this);
                return;
            }

            KeyFrameAnimationFacade.SetInstance(this);
            DontDestroyOnLoad(gameObject);
        }

        private void Awake()
        {
            FirstInitialize();

            DataForJob = new NativeList<KeyFrameJobData>(256, Allocator.Persistent);
            NewFrameDataFromJob = new NativeQueue<KeyFrameJob_FrameDataFromJob>(Allocator.Persistent);
            NewEventDataFromJob = new NativeQueue<KeyFrameJob_EventDataFromJob>(Allocator.Persistent);
    }

        private void OnDestroy()
        {
            KeyFrameAnimationFacade.RemoveInstance();
            DataForJob.Dispose();
            NewFrameDataFromJob.Dispose();
            NewEventDataFromJob.Dispose();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeSystem()
        {
            GameObject go = new GameObject();
            go.name = "KeyFrameAnimationSystem";
            go.AddComponent<KeyFrameAnimationSystem>();
        }

        #region Interface
        // ------------   INTERFACE   -----------------
        // interface functions, I want to move these to a different class if I can..
        public bool AddToCollections(KeyFrameAnimator animator)
        {
            // Add to a list of Animators and a list of data for a job to use.  These can't be in the same struct because I can't have reference data in data I pass to a struct
            if (Animators.Count <= animator.AnimatorID)
            {
                Animators.Add(animator);
            }
            else
            {
                Animators[animator.AnimatorID] = animator;
            }
            //Animators[animator.AnimatorID] = animator;
            //Animators.Add(animator.AnimatorID, animator);
            var anim = animator.Animation;
            NewAnimators.Enqueue(new KeyFrameJobData(animator.AnimatorID, anim.Style, anim.StartingFrameIndex,
                           anim.MaxFrameIndex, anim.LoopStartFrame, anim.GetFrameTimeFromStart(anim.StartingFrameIndex),
                           anim.MaxEventIndex, anim.GetNextEventTimeAfterFrame(anim.StartingFrameIndex)));
            //DataForJob.Add(new KeyFrameJobData(animator.AnimatorID, anim.Style, anim.StartingFrameIndex, 
            //               anim.MaxFrameIndex, anim.LoopStartFrame, anim.GetFrameTimeFromStart(anim.StartingFrameIndex),
            //               anim.MaxEventIndex, anim.GetNextEventTimeAfterFrame(anim.StartingFrameIndex))); // GetEventTime should actually get the time from the first event that occurs after GetFrameTime()............
            return true;
        }

        public KeyFrameJobData RemoveFromCollections(KeyFrameAnimator animator)
        {
            // if animator is in this collection, remove it
            var data = DataForJob[animator.AnimatorID];
            data.Pause = true;

            DataForJob[animator.AnimatorID] = data;


            return DataForJob[animator.AnimatorID];
            // I haven't found a way to remove by an animatorID index and not muck it all
            //if (Animators != null)
                //Animators.Remove(animator.AnimatorID);
            //if (DataForJob.IsCreated)
               // DataForJob.Remove(animator.AnimatorID);
        }

        public void ChangeAnimation(KeyFrameAnimator animator, int startingFrame, bool maintainTiming)
        {
            // Don't call this function from a Start method, unless I'm really sure the object in question will be created after this system is created.
            //  if I need it, I can make a callback function somewhere

            var newAnimData = new ChangedAnimationData()
            {
                StartingFrame = startingFrame,
                Animator = animator
            };

            if (!maintainTiming)
                ChangedAnimations.Enqueue(newAnimData);
            //else if (animator.Animation.MaxEventIndex > 0 && maintainTiming)
            else if(maintainTiming)
                ChangedAnimationsSync.Enqueue(animator);
        }

        public void PauseAnimator(int id)
        {
            var data = DataForJob[id];
            data.Pause = true;
            DataForJob[id] = data;
        }

        public void ResumeAnimator(int id)
        {
            var data = DataForJob[id];
            data.Pause = false;
            DataForJob[id] = data;
        }
        #endregion
    }
}