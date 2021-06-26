using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using OpenSage.Client;
using OpenSage.Content;
using OpenSage.Data.Ini;
using OpenSage.FileFormats;
using OpenSage.Graphics;
using OpenSage.Graphics.Animation;
using OpenSage.Graphics.Cameras;
using OpenSage.Graphics.ParticleSystems;
using OpenSage.Graphics.Rendering;
using OpenSage.Graphics.Shaders;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public class W3dModelDraw : DrawModule
    {
        private readonly W3dModelDrawModuleData _data;
        private readonly GameContext _context;

        private readonly List<IConditionState> _conditionStates;
        private readonly ModelConditionState _defaultConditionState;

        private readonly List<IConditionState> _animationStates;
        private readonly AnimationState _idleAnimationState;

        protected readonly List<GeneralsTransitionState> _generalsTransitionStates;
        protected readonly List<BfmeTransitionState> _bfmeTransitionStates;

        private readonly Dictionary<ModelConditionState, W3dModelDrawConditionState> _cachedModelDrawConditionStates;

        private ModelConditionState _activeConditionState;
        protected AnimationState _activeAnimationState;

        private W3dModelDrawConditionState _activeModelDrawConditionState;

        protected ModelInstance ActiveModelInstance => _activeModelDrawConditionState.Model;
 
        public override IEnumerable<BitArray<ModelConditionFlag>> ModelConditionStates
        {
            get
            {
                yield return _defaultConditionState.ConditionFlags;

                foreach (var conditionState in _conditionStates)
                {
                    yield return conditionState.ConditionFlags;
                }

                foreach (var animationState in _animationStates)
                {
                    yield return animationState.ConditionFlags;
                }
            }
        }

        internal override string GetWeaponFireFXBone(WeaponSlot slot)
            => _defaultConditionState?.WeaponFireFXBones.Find(x => x.WeaponSlot == slot)?.BoneName;

        internal override string GetWeaponLaunchBone(WeaponSlot slot)
            => _defaultConditionState?.WeaponLaunchBones.Find(x => x.WeaponSlot == slot)?.BoneName;

        internal W3dModelDraw(
            W3dModelDrawModuleData data,
            Drawable drawable,
            GameContext context)
        {
            _data = data;
            Drawable = drawable;
            _context = context;

            _conditionStates = new List<IConditionState>();

            if (data.DefaultConditionState != null)
            {
                _defaultConditionState = data.DefaultConditionState;
            }

            foreach (var conditionState in data.ConditionStates)
            {
                _conditionStates.Add(conditionState);
            }

            if (_defaultConditionState == null)
            {
                _defaultConditionState = (ModelConditionState)_conditionStates.Find(x => !x.ConditionFlags.AnyBitSet);

                if (_defaultConditionState != null)
                {
                    _conditionStates.Remove(_defaultConditionState);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            _cachedModelDrawConditionStates = new Dictionary<ModelConditionState, W3dModelDrawConditionState>();

            SetActiveConditionState(_defaultConditionState, context.Random);

            _animationStates = new List<IConditionState>();

            if (data.IdleAnimationState != null)
            {
                _idleAnimationState = data.IdleAnimationState;
            }

            foreach (var animationState in data.AnimationStates)
            {
                _animationStates.Add(animationState);
            }

            _generalsTransitionStates = data.GeneralsTransitionStates;
            _bfmeTransitionStates = data.BfmeTransitionStates;
        }

        private bool ShouldWaitForRunningAnimationsToFinish(ModelConditionState conditionState)
        {
            return _activeConditionState != null
                && conditionState.WaitForStateToFinishIfPossible != null
                && _activeConditionState.TransitionKey == conditionState.WaitForStateToFinishIfPossible
                && (_activeModelDrawConditionState?.StillActive() ?? false);
        }

        private void SetActiveConditionState(ModelConditionState conditionState, Random random)
        {
            if (_activeConditionState == conditionState || ShouldWaitForRunningAnimationsToFinish(conditionState))
            {
                return;
            }

            _activeModelDrawConditionState?.Deactivate();

            if (!_cachedModelDrawConditionStates.TryGetValue(conditionState, out var modelDrawConditionState))
            {
                modelDrawConditionState = AddDisposable(CreateModelDrawConditionStateInstance(conditionState, random));
                _cachedModelDrawConditionStates.Add(conditionState, modelDrawConditionState);
            }

            _activeConditionState = conditionState;
            _activeModelDrawConditionState = modelDrawConditionState;

            var speedFactor = conditionState.AnimationSpeedFactorRange.GetValue(random);
            _activeModelDrawConditionState?.Activate(speedFactor);
        }

        protected virtual bool SetActiveAnimationState(AnimationState animationState, Random random)
        {
            if (animationState == _activeAnimationState && (_activeModelDrawConditionState?.StillActive() ?? false))
            {
                return false;
            }

            if (animationState == null
                || animationState.Animations.Count == 0
                || _activeModelDrawConditionState == null)
            {
                return true;
            }

            var modelInstance = _activeModelDrawConditionState.Model;
            modelInstance.AnimationInstances.Clear();
            _activeAnimationState = animationState;

            var animationBlock = animationState.Animations[random.Next(0, animationState.Animations.Count - 1)];
            if (animationBlock != null)
            {
                foreach (var animation in animationBlock.Animations)
                {
                    var anim = animation.Value;
                    //Check if the animation does really exist
                    if (anim != null)
                    {
                        var flags = animationState.Flags;
                        var mode = animationBlock.AnimationMode;
                        var animationInstance = new AnimationInstance(modelInstance.ModelBoneInstances, anim, mode, flags);
                        modelInstance.AnimationInstances.Add(animationInstance);
                        animationInstance.Play();
                        break;
                    }
                }
            }
            return true;
        }

        private IConditionState FindBestFittingConditionState(IConditionState defaultState, List<IConditionState> conditionStates, BitArray<ModelConditionFlag> flags)
        {
            var bestConditionState = defaultState;
            var bestIntersections = 0;

            foreach (var conditionState in conditionStates)
            {
                var numStateBits = conditionState.ConditionFlags.NumBitsSet;
                var numIntersectionBits = conditionState.ConditionFlags.CountIntersectionBits(flags);

                if (numIntersectionBits < bestIntersections
                    || numStateBits > numIntersectionBits)
                {
                    continue;
                }

                bestConditionState = conditionState;
                bestIntersections = numIntersectionBits;
            }

            return bestConditionState;
        }

        public override void UpdateConditionState(BitArray<ModelConditionFlag> flags, Random random)
        {
            if (!flags.BitsChanged)
            {
                if (!(_activeModelDrawConditionState?.StillActive() ?? false)
                    && _activeAnimationState != null
                    && ReferenceEquals(_activeAnimationState, _idleAnimationState))
                {
                    SetActiveAnimationState(_activeAnimationState, random);
                }
                return;
            }

            var bestConditionState = (ModelConditionState)FindBestFittingConditionState(_defaultConditionState, _conditionStates, flags);
            SetActiveConditionState(bestConditionState, random);

            foreach (var weaponMuzzleFlash in bestConditionState.WeaponMuzzleFlashes)
            {
                var visible = flags.Get(ModelConditionFlag.FiringA);
                for (var i = 0; i < _activeModelDrawConditionState.Model.ModelBoneInstances.Length; i++)
                {
                    var bone = _activeModelDrawConditionState.Model.ModelBoneInstances[i];
                    // StartsWith is a bit awkward here, but for instance AVCommance has WeaponMuzzleFlashes = { TurretFX }, and Bones = { TURRETFX01 }
                    if (bone.Name.StartsWith(weaponMuzzleFlash.BoneName, StringComparison.OrdinalIgnoreCase))
                    {
                        _activeModelDrawConditionState.Model.BoneVisibilities[i] = visible;
                    }
                }
            };

            var bestAnimationState = (AnimationState) FindBestFittingConditionState(_idleAnimationState, _animationStates, flags);
            SetActiveAnimationState(bestAnimationState, random);
        }

        private W3dModelDrawConditionState CreateModelDrawConditionStateInstance(ModelConditionState conditionState, Random random)
        {
            // Load model, fallback to default model.
            var model = conditionState.Model?.Value ?? _defaultConditionState.Model?.Value;
            var modelInstance = model?.CreateInstance(_context.AssetLoadContext) ?? null;

            if (modelInstance == null)
            {
                return null;
            }

            // TODO: since the ModelDrawConditionStates are cached we should find a way to change the animation afterwards
            var animations = conditionState.ConditionAnimations;
            if (!conditionState.ConditionFlags.Get(ModelConditionFlag.Moving))
            {
                animations.AddRange(conditionState.IdleAnimations);
            }

            if (animations.Count > 0)
            {
                var animation = animations[random.Next(0, animations.Count - 1)]?.Animation.Value;
                if (animation != null)
                {
                    var mode = conditionState.AnimationMode;
                    var flags = conditionState.Flags;
                    var animationInstance = new AnimationInstance(modelInstance.ModelBoneInstances, animation, mode, flags);
                    modelInstance.AnimationInstances.Add(animationInstance);
                }
            }

            var particleSystems = new List<ParticleSystem>();

            foreach (var particleSysBone in conditionState.ParticleSysBones)
            {
                var particleSystemTemplate = particleSysBone.ParticleSystem.Value;
                if (particleSystemTemplate == null)
                {
                    throw new InvalidOperationException();
                }

                var bone = modelInstance.Model.BoneHierarchy.Bones.FirstOrDefault(x => string.Equals(x.Name, particleSysBone.BoneName, StringComparison.OrdinalIgnoreCase));
                if (bone == null)
                {
                    // TODO: Should this ever happen?
                    continue;
                }

                particleSystems.Add(_context.ParticleSystems.Create(
                    particleSystemTemplate,
                    () => ref modelInstance.AbsoluteBoneTransforms[bone.Index]));
            }

            if (conditionState.HideSubObject != null)
            {
                foreach (var hideSubObject in conditionState.HideSubObject)
                {
                    var item = modelInstance.ModelBoneInstances.Select((value, i) => new { i, value }).FirstOrDefault(x => x.value.Name.EndsWith(hideSubObject.ToUpper()));
                    if (item != null)
                    {
                        modelInstance.BoneVisibilities[item.i] = false;
                    }
                }
            }
            if (conditionState.ShowSubObject != null)
            {
                foreach (var showSubObject in conditionState.ShowSubObject)
                {
                    var item = modelInstance.ModelBoneInstances.Select((value, i) => new { i, value }).FirstOrDefault(x => x.value.Name.EndsWith(showSubObject.ToUpper()));
                    if (item != null)
                    {
                        modelInstance.BoneVisibilities[item.i] = true;
                    }
                }
            }

            return new W3dModelDrawConditionState(modelInstance, particleSystems, _context);
        }

        internal override (ModelInstance, ModelBone) FindBone(string boneName)
        {
            return (ActiveModelInstance, ActiveModelInstance.Model.BoneHierarchy.Bones.FirstOrDefault(x => string.Equals(x.Name, boneName, StringComparison.OrdinalIgnoreCase)));
        }

        internal ModelBoneInstance FindBoneInstance(string name)
        {
            foreach (var bone in ActiveModelInstance.Model.BoneHierarchy.Bones)
            {
                if (bone.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return ActiveModelInstance.ModelBoneInstances[bone.Index];
                }
            }

            return null;
        }

        internal override void Update(in TimeInterval gameTime)
        {
            if (_activeConditionState.Flags.HasFlag(AnimationFlags.AdjustHeightByConstructionPercent))
            {
                var progress = GameObject.BuildProgress;
                GameObject.VerticalOffset = -((1.0f - progress) * GameObject.RoughCollider.Height);
            }

            _activeModelDrawConditionState?.Update(gameTime);
        }

        internal override void SetWorldMatrix(in Matrix4x4 worldMatrix)
        {
            if (GameObject.VerticalOffset != 0)
            {
                var mat = worldMatrix * Matrix4x4.CreateTranslation(Vector3.UnitZ * GameObject.VerticalOffset);
                _activeModelDrawConditionState?.SetWorldMatrix(mat);
            }
            else
            {
                _activeModelDrawConditionState?.SetWorldMatrix(worldMatrix);
            }
        }

        internal override void BuildRenderList(
                RenderList renderList,
                Camera camera,
                bool castsShadow,
                MeshShaderResources.RenderItemConstantsPS renderItemConstantsPS,
                Dictionary<string, bool> shownSubObjects = null,
                Dictionary<string, bool> hiddenSubObjects = null)
        {
            _activeModelDrawConditionState?.BuildRenderList(
                renderList,
                camera,
                castsShadow,
                renderItemConstantsPS,
                shownSubObjects,
                hiddenSubObjects);
        }

        internal override void DrawInspector()
        {
            ImGui.LabelText("Model", _activeModelDrawConditionState?.Model.Model.Name ?? "<null>");
        }

        internal override void Load(BinaryReader reader)
        {
            var version = reader.ReadVersion();
            if (version != 2)
            {
                throw new InvalidDataException();
            }

            base.Load(reader);

            for (var i = 0; i < 2; i++)
            {
                var unknownCount = reader.ReadByte();
                for (var j = 0; j < unknownCount; j++)
                {
                    var unknown1 = reader.ReadUInt32();
                    if (unknown1 != 0)
                    {
                        throw new InvalidDataException();
                    }

                    var unknown2 = reader.ReadUInt32();
                    if (unknown2 != 0)
                    {
                        throw new InvalidDataException();
                    }

                    var unknown3 = reader.ReadUInt32();
                    if (unknown3 != 0)
                    {
                        throw new InvalidDataException();
                    }
                }
            }

            var unknownBool1 = reader.ReadBooleanChecked();
            var unknownBool2 = reader.ReadBooleanChecked();

            if (unknownBool1 || unknownBool2)
            {
                throw new InvalidDataException();
            }
        }
    }

    internal sealed class W3dModelDrawConditionState : DisposableBase
    {
        private readonly IEnumerable<ParticleSystem> _particleSystems;
        private readonly GameContext _context;

        public readonly ModelInstance Model;

        public W3dModelDrawConditionState(
            ModelInstance modelInstance,
            IEnumerable<ParticleSystem> particleSystems,
            GameContext context)
        {
            Model = AddDisposable(modelInstance);

            _particleSystems = particleSystems;
            _context = context;
        }

        public void Activate(float speedFactor = 1.0f)
        {
            foreach (var animationInstance in Model.AnimationInstances)
            {
                animationInstance.Play(speedFactor);
            }

            foreach (var particleSystem in _particleSystems)
            {
                particleSystem.Activate();
            }
        }

        public bool StillActive() => Model.AnimationInstances.Any(x => x.IsPlaying);

        public void Deactivate()
        {
            foreach (var animationInstance in Model.AnimationInstances)
            {
                animationInstance.Stop();
            }

            foreach (var particleSystem in _particleSystems)
            {
                particleSystem.Deactivate();
            }
        }

        public void Update(in TimeInterval gameTime)
        {
            Model.Update(gameTime);
        }

        public void SetWorldMatrix(in Matrix4x4 worldMatrix)
        {
            Model.SetWorldMatrix(worldMatrix);
        }

        public void BuildRenderList(
            RenderList renderList,
            Camera camera,
            bool castsShadow,
            MeshShaderResources.RenderItemConstantsPS renderItemConstantsPS,
            Dictionary<string, bool> shownSubObjects = null,
            Dictionary<string, bool> hiddenSubObjects = null)
        {
            Model.BuildRenderList(
                renderList,
                camera,
                castsShadow,
                renderItemConstantsPS,
                shownSubObjects,
                hiddenSubObjects);
        }

        protected override void Dispose(bool disposeManagedResources)
        {
            foreach (var particleSystem in _particleSystems)
            {
                _context.ParticleSystems.Remove(particleSystem);
            }

            base.Dispose(disposeManagedResources);
        }
    }

    public class W3dModelDrawModuleData : DrawModuleData
    {
        internal static W3dModelDrawModuleData ParseModel(IniParser parser) => parser.ParseBlock(FieldParseTable);

        internal static readonly IniParseTable<W3dModelDrawModuleData> FieldParseTable = new IniParseTable<W3dModelDrawModuleData>
        {
            { "DefaultConditionState", (parser, x) => parser.Temp = x.DefaultConditionState = ModelConditionState.ParseDefault(parser) },
            { "DefaultModelConditionState", (parser, x) => parser.Temp = x.DefaultConditionState = ModelConditionState.ParseDefault(parser) },

            {
                "ConditionState",
                (parser, x) =>
                {
                    var conditionState = ModelConditionState.Parse(parser, x.DefaultConditionState);
                    x.ConditionStates.Add(conditionState);
                    parser.Temp = conditionState;
                }
            },
            {
                "ModelConditionState",
                (parser, x) =>
                {
                    var conditionState = ModelConditionState.Parse(parser, x.DefaultConditionState);
                    x.ConditionStates.Add(conditionState);
                    parser.Temp = conditionState;
                }
            },

            { "IgnoreConditionStates", (parser, x) => x.IgnoreConditionStates = parser.ParseEnumBitArray<ModelConditionFlag>() },
            { "AliasConditionState", (parser, x) => x.ParseAliasConditionState(parser) },
            { "TransitionState", (parser, x) => x.ParseTransitionState(parser) },
            { "OkToChangeModelColor", (parser, x) => x.OkToChangeModelColor = parser.ParseBoolean() },
            { "ReceivesDynamicLights", (parser, x) => x.ReceivesDynamicLights = parser.ParseBoolean() },
            { "ProjectileBoneFeedbackEnabledSlots", (parser, x) => x.ProjectileBoneFeedbackEnabledSlots = parser.ParseEnumBitArray<WeaponSlot>() },
            { "AnimationsRequirePower", (parser, x) => x.AnimationsRequirePower = parser.ParseBoolean() },
            { "ParticlesAttachedToAnimatedBones", (parser, x) => x.ParticlesAttachedToAnimatedBones = parser.ParseBoolean() },
            { "MinLODRequired", (parser, x) => x.MinLodRequired = parser.ParseEnum<ModelLevelOfDetail>() },
            { "ExtraPublicBone", (parser, x) => x.ExtraPublicBones.Add(parser.ParseBoneName()) },
            { "AttachToBoneInAnotherModule", (parser, x) => x.AttachToBoneInAnotherModule = parser.ParseBoneName() },
            { "TrackMarks", (parser, x) => x.TrackMarks = parser.ParseTextureReference() },
            { "TrackMarksLeftBone", (parser, x) => x.TrackMarksLeftBone = parser.ParseAssetReference() },
            { "TrackMarksRightBone", (parser, x) => x.TrackMarksRightBone = parser.ParseAssetReference() },
            { "InitialRecoilSpeed", (parser, x) => x.InitialRecoilSpeed = parser.ParseFloat() },
            { "MaxRecoilDistance", (parser, x) => x.MaxRecoilDistance = parser.ParseFloat() },
            { "RecoilSettleSpeed", (parser, x) => x.RecoilSettleSpeed = parser.ParseFloat() },

            { "IdleAnimationState", (parser, x) => x.IdleAnimationState = AnimationState.Parse(parser) },
            { "AnimationState", (parser, x) => x.AnimationStates.Add(AnimationState.Parse(parser)) },
        };

        public BitArray<ModelConditionFlag> IgnoreConditionStates { get; private set; }
        public ModelConditionState DefaultConditionState { get; private set; }
        public List<ModelConditionState> ConditionStates { get; } = new List<ModelConditionState>();
        public List<GeneralsTransitionState> GeneralsTransitionStates { get; } = new List<GeneralsTransitionState>();
        public List<BfmeTransitionState> BfmeTransitionStates { get; } = new List<BfmeTransitionState>();

        public bool OkToChangeModelColor { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool ReceivesDynamicLights { get; private set; }

        public BitArray<WeaponSlot> ProjectileBoneFeedbackEnabledSlots { get; private set; }
        public bool AnimationsRequirePower { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool ParticlesAttachedToAnimatedBones { get; private set; }

        /// <summary>
        /// Minimum level of detail required before this object appears in the game.
        /// </summary>
        public ModelLevelOfDetail MinLodRequired { get; private set; }

        public List<string> ExtraPublicBones { get; } = new List<string>();
        public string AttachToBoneInAnotherModule { get; private set; }

        public LazyAssetReference<TextureAsset> TrackMarks { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public string TrackMarksLeftBone { get; private set; }
        [AddedIn(SageGame.Bfme)]
        public string TrackMarksRightBone { get; private set; }

        public float InitialRecoilSpeed { get; private set; } = 2.0f;
        public float MaxRecoilDistance { get; private set; } = 3.0f;
        public float RecoilSettleSpeed { get; private set; } = 0.065f;

        public AnimationState IdleAnimationState { get; private set; }
        public List<AnimationState> AnimationStates { get; } = new List<AnimationState>();

        private void ParseAliasConditionState(IniParser parser)
        {
            if (parser.Temp is not ModelConditionState lastConditionState)
            {
                throw new IniParseException("Cannot use AliasConditionState if there are no preceding ConditionStates", parser.CurrentPosition);
            }

            var conditionFlags = parser.ParseEnumBitArray<ModelConditionFlag>();

            var aliasedConditionState = lastConditionState.Clone(conditionFlags);

            ConditionStates.Add(aliasedConditionState);
        }

        private void ParseTransitionState(IniParser parser)
        {
            if (parser.SageGame == SageGame.CncGenerals || parser.SageGame == SageGame.CncGeneralsZeroHour)
            {
                GeneralsTransitionStates.Add(GeneralsTransitionState.Parse(parser));
            }
            else
            {
                BfmeTransitionStates.Add(BfmeTransitionState.Parse(parser));
            }
        }


        internal override DrawModule CreateDrawModule(Drawable drawable, GameContext context)
        {
            return new W3dModelDraw(this, drawable, context);
        }
    }

    public enum ModelLevelOfDetail
    {
        [IniEnum("LOW")]
        Low,

        [IniEnum("MEDIUM")]
        Medium,

        [IniEnum("HIGH")]
        High,
    }
}
