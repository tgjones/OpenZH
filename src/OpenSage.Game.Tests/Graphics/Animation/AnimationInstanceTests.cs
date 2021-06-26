using System;
using System.Numerics;
using OpenSage.Graphics;
using OpenSage.Graphics.Animation;
using OpenSage.Logic.Object;
using Xunit;

namespace OpenSage.Tests.Graphics.Animation
{
    public class AnimationInstanceTests
    {
        [Fact]
        public void OnceForwardsTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.Once,
                AnimationFlags.StartFrameFirst);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 0; i < 5; i++)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                Assert.Equal(i, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void OnceBackwardsTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.OnceBackwards,
                AnimationFlags.StartFrameLast);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 4; i >= 0; i--)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                Assert.Equal(i, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void StartAtEndForwardsTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.Once,
                AnimationFlags.StartFrameLast);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 4; i >= 0; i--)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                // we should always show the last frame
                Assert.Equal(4, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void StartAtBeginningBackwardsTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.OnceBackwards,
                AnimationFlags.StartFrameFirst);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 4; i >= 0; i--)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                // we should always show the first frame
                Assert.Equal(0, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void StartAtBeginningManualTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.Manual,
                AnimationFlags.StartFrameFirst);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 0; i < 5; i++)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                // always show the first frame, since the mode is manual
                Assert.Equal(0, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void StartAtEndManualTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.Manual,
                AnimationFlags.StartFrameLast);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 0; i < 5; i++)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                // always show the last frame
                Assert.Equal(4, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void LoopForwardsTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.Loop,
                AnimationFlags.StartFrameFirst);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 0; i < 15; i++)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                Assert.Equal(i % 5, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        [Fact]
        public void LoopBackwardsTest()
        {
            var modelBoneInstances = NewDefaultModelBoneInstances();
            var animation = NewBasicAnimationInstance();

            var instance = new AnimationInstance(modelBoneInstances, animation, AnimationMode.LoopBackwards,
                AnimationFlags.StartFrameLast);

            instance.Play();
            var time = TimeInterval.Zero;
            for (var i = 14; i >= 0; i--)
            {
                instance.Update(time);
                time = new TimeInterval(time.TotalTime + TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));

                Assert.Equal(i % 5, modelBoneInstances[0].AnimatedOffset.Translation.X);
            }
        }

        private static ModelBoneInstance[] NewDefaultModelBoneInstances()
        {
            var testBone = new ModelBone(0, "myBone", null, Vector3.Zero, Quaternion.Identity);
            return new[]
            {
                new ModelBoneInstance(testBone)
            };
        }

        private static W3DAnimation NewBasicAnimationInstance()
        {
            var animationClips = new[]
            {
                new AnimationClip(AnimationClipType.TranslationX, 0, new []
                {
                    new Keyframe(TimeSpan.FromSeconds(0), new KeyframeValue
                    {
                        FloatValue = 0,
                    }),
                    new Keyframe(TimeSpan.FromSeconds(1), new KeyframeValue
                    {
                        FloatValue = 1,
                    }),
                    new Keyframe(TimeSpan.FromSeconds(2), new KeyframeValue
                    {
                        FloatValue = 2,
                    }),
                    new Keyframe(TimeSpan.FromSeconds(3), new KeyframeValue
                    {
                        FloatValue = 3,
                    }),
                    new Keyframe(TimeSpan.FromSeconds(4), new KeyframeValue
                    {
                        FloatValue = 4,
                    }),
                })
            };

            return new W3DAnimation(string.Empty, TimeSpan.FromSeconds(5), animationClips);
        }
    }
}
