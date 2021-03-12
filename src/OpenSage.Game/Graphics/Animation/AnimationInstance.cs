using System;
using System.Numerics;
using OpenSage.Logic.Object;
using OpenSage.Mathematics;

namespace OpenSage.Graphics.Animation
{
    public sealed class AnimationInstance
    {
        private readonly int[] _keyframeIndices;
        private readonly ModelBoneInstance[] _boneInstances;
        private readonly W3DAnimation _animation;

        private TimeSpan _currentTimeValue;

        private bool _playing;
        private readonly AnimationMode _mode;
        private readonly AnimationFlags _flags;

        private float _speedFactor;

        private bool Looping => _mode == AnimationMode.Loop || _mode == AnimationMode.LoopBackwards;
        private bool Reverse => _mode == AnimationMode.OnceBackwards || _mode == AnimationMode.LoopBackwards;
        private bool Manual => _mode == AnimationMode.Manual;

        public AnimationInstance(ModelBoneInstance[] modelBoneInstances, W3DAnimation animation,
            AnimationMode mode, AnimationFlags flags)
        {
            _animation = animation;
            _mode = mode;
            _flags = flags;
            _boneInstances = modelBoneInstances;
            _keyframeIndices = new int[animation.Clips.Length];
        }

        public void Play(float speedFactor = 1.0f)
        {
            _speedFactor = speedFactor;
            if (_playing)
            {
                return;
            }

            ResetTimeStamps();
            ResetBoneTransforms();

            _playing = true;
        }

        public bool IsPlaying => _playing;

        public void Stop()
        {
            if (!_playing)
            {
                return;
            }

            // TODO: Reset to original transforms and visibilities?

            _playing = false;
        }

        private void ResetTimeStamps()
        {
            if (_flags == AnimationFlags.None ||
                _flags.HasFlag(AnimationFlags.StartFrameFirst) ||
                _flags.HasFlag(AnimationFlags.StartFrameLast))
            {
                if (Reverse)
                {
                    _currentTimeValue = _animation.Duration;
                }
                else
                {
                    _currentTimeValue = TimeSpan.Zero;
                }
            }
            else
            {
                //TODO: implement other flags
                //throw new NotImplementedException();
            }
        }

        private void ResetBoneTransforms()
        {
            Array.Clear(_keyframeIndices, 0, _keyframeIndices.Length);

            if (_flags.HasFlag(AnimationFlags.StartFrameLast) || Reverse && !_flags.HasFlag(AnimationFlags.StartFrameFirst))
            {
                for (var i = 0; i < _keyframeIndices.Length; i++)
                {
                    _keyframeIndices[i] = _animation.Clips[i].Keyframes.Length - 1;
                }
            }

            foreach (var bone in _boneInstances)
            {
                bone.AnimatedOffset.Translation = Vector3.Zero;
                bone.AnimatedOffset.Rotation = Quaternion.Identity;

                bone.Visible = true;
            }
        }

        internal void Update(in TimeInterval gameTime)
        {
            if (!_playing)
            {
                return;
            }

            UpdateBoneTransforms(gameTime);
        }

        private void UpdateBoneTransforms(in TimeInterval gameTime)
        {
            //TODO: implement ping pong
            var time = _currentTimeValue;
            var deltaTime = gameTime.DeltaTime * _speedFactor;

            if (Manual)
            {
                time = _animation.Duration * _speedFactor;
            }
            else
            {
                if (Reverse)
                {
                    time -= deltaTime;
                }
                else
                {
                    time += deltaTime;
                }
            }

            var reachedEnd = Reverse
                ? time < TimeSpan.Zero
                : time >= _animation.Duration;

            if (reachedEnd)
            {
                // If we reached the end, loop back to the start or stay at the last frame
                if (Looping)
                {
                    if (Reverse)
                    {
                        while (time < TimeSpan.Zero)
                        {
                            time += _animation.Duration;
                        }

                        // If we've just moved forwards, reset the keyframe indices.
                        if (time > _currentTimeValue)
                        {
                            ResetBoneTransforms();
                        }
                    }
                    else
                    {
                        while (time >= _animation.Duration)
                        {
                            time -= _animation.Duration;
                        }

                        // If we've just moved backwards, reset the keyframe indices.
                        if (time < _currentTimeValue)
                        {
                            ResetBoneTransforms();
                        }
                    }
                }
                else
                {
                    _playing = false;
                    if (Reverse)
                    {
                        time = TimeSpan.Zero;
                    }
                    else
                    {
                        time = _animation.Duration;
                    }
                }
            }

            _currentTimeValue = time;

            Keyframe? previous;
            Keyframe? next;

            for (var i = 0; i < _animation.Clips.Length; i++)
            {
                next = null;

                var clip = _animation.Clips[i];

                if (clip.Bone >= _boneInstances.Length)
                {
                    continue;
                }

                // In case we're beyond the animation bounds,
                // default to the end.
                previous = clip.Keyframes[_keyframeIndices[i]];

                if (Reverse)
                {
                    for (var j = _keyframeIndices[i]; j >= 0; j--)
                    {
                        var keyframe = clip.Keyframes[j];

                        if (keyframe.Time < _currentTimeValue)
                        {
                            next = keyframe;
                            break;
                        }

                        previous = keyframe;
                        _keyframeIndices[i] = j;
                    }
                }
                else
                {
                    for (var j = _keyframeIndices[i]; j < clip.Keyframes.Length; j++)
                    {
                        var keyframe = clip.Keyframes[j];

                        if (keyframe.Time > _currentTimeValue)
                        {
                            next = keyframe;
                            break;
                        }

                        previous = keyframe;
                        _keyframeIndices[i] = j;
                    }
                }

                if (previous != null)
                {
                    Evaluate(
                        clip,
                        previous.Value,
                        next ?? previous.Value,
                        _currentTimeValue);
                }
            }
        }

        private void Evaluate(
            AnimationClip clip,
            in Keyframe previous,
            in Keyframe next,
            TimeSpan currentTime)
        {
            var progress = (float) (currentTime - previous.Time).TotalMilliseconds;
            var duration = (float) (next.Time - previous.Time).TotalMilliseconds;

            var amount = (duration != 0)
                ? progress / duration
                : 0;

            var animatedOffset = _boneInstances[clip.Bone].AnimatedOffset;

            switch (clip.ClipType)
            {
                case AnimationClipType.TranslationX:
                    animatedOffset.Translation = animatedOffset.Translation.WithX(MathUtility.Lerp(
                        previous.Value.FloatValue,
                        next.Value.FloatValue,
                        amount));
                    break;

                case AnimationClipType.TranslationY:
                    animatedOffset.Translation = animatedOffset.Translation.WithY(MathUtility.Lerp(
                        previous.Value.FloatValue,
                        next.Value.FloatValue,
                        amount));
                    break;

                case AnimationClipType.TranslationZ:
                    animatedOffset.Translation = animatedOffset.Translation.WithZ(MathUtility.Lerp(
                        previous.Value.FloatValue,
                        next.Value.FloatValue,
                        amount));
                    break;

                case AnimationClipType.Quaternion:
                    _boneInstances[clip.Bone].AnimatedOffset.Rotation = Quaternion.Slerp(
                        previous.Value.Quaternion,
                        next.Value.Quaternion,
                        amount);
                    break;

                case AnimationClipType.Visibility:
                    _boneInstances[clip.Bone].Visible = previous.Value.BoolValue;
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
