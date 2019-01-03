﻿using System;
using System.Collections.Generic;
using System.Numerics;
using OpenSage.Data.Ini.Parser;
using OpenSage.Graphics.ParticleSystems;
using OpenSage.Mathematics;

namespace OpenSage.Data.Ini
{
    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleSystemTemplate
    {
        internal static FXParticleSystemTemplate Parse(IniParser parser)
        {
            return parser.ParseTopLevelNamedBlock(
                (x, name) => x.Name = name,
                FieldParseTable);
        }

        private static readonly IniParseTable<FXParticleSystemTemplate> FieldParseTable = new IniParseTable<FXParticleSystemTemplate>
        {
            { "System", (parser, x) => parser.ParseBlockContent(x, SystemFieldParseTable) },

            { "Color", (parser, x) => x.Colors = ParseModule(parser, ColorModuleParseTable) },
            { "Alpha", (parser, x) => x.Alpha = ParseModule(parser, AlphaModuleParseTable) },
            { "Update", (parser, x) => x.Update = ParseModule(parser, UpdateModuleParseTable) },
            { "Physics", (parser, x) => x.Physics = ParseModule(parser, PhysicsModuleParseTable) },
            { "Draw", (parser, x) => x.Draw = ParseModule(parser, DrawModuleParseTable) },
            { "Wind", (parser, x) => x.Wind = ParseModule(parser, WindModuleParseTable) },
            { "EmissionVelocity", (parser, x) => x.EmissionVelocity = ParseModule(parser, EmissionVelocityModuleParseTable) },
            { "EmissionVolume", (parser, x) => x.EmissionVolume = ParseModule(parser, EmissionVolumeModuleParseTable) },
            { "Event", (parser, x) => x.Event = ParseModule(parser, EventModuleParseTable) }
        };

        private static readonly IniParseTable<FXParticleSystemTemplate> SystemFieldParseTable = new IniParseTable<FXParticleSystemTemplate>
        {
            { "Priority", (parser, x) => x.Priority = parser.ParseEnum<ParticleSystemPriority>() },
            { "IsOneShot", (parser, x) => x.IsOneShot = parser.ParseBoolean() },
            { "Shader", (parser, x) => x.Shader = parser.ParseEnum<ParticleSystemShader>() },
            { "Type", (parser, x) => x.Type = parser.ParseEnum<ParticleSystemType>() },
            { "ParticleName", (parser, x) => x.ParticleName = parser.ParseFileName() },
            { "PerParticleAttachedSystem", (parser, x) => x.PerParticleAttachedSystem = parser.ParseAssetReference() },
            { "SlaveSystem", (parser, x) => x.SlaveSystem = parser.ParseAssetReference() },
            { "SlavePosOffset", (parser, x) => x.SlavePosOffset = parser.ParseVector3() },
            { "Lifetime", (parser, x) => x.Lifetime = RandomVariable.Parse(parser) },
            { "SystemLifetime", (parser, x) => x.SystemLifetime = parser.ParseInteger() },
            { "SortLevel", (parser, x) => x.SortLevel = parser.ParseInteger() },
            { "Size", (parser, x) => x.Size = RandomVariable.Parse(parser) },
            { "StartSizeRate", (parser, x) => x.StartSizeRate = RandomVariable.Parse(parser) },
            { "IsGroundAligned", (parser, x) => x.IsGroundAligned = parser.ParseBoolean() },
            { "IsEmitAboveGroundOnly", (parser, x) => x.IsEmitAboveGroundOnly = parser.ParseBoolean() },
            { "IsParticleUpTowardsEmitter", (parser, x) => x.IsParticleUpTowardsEmitter = parser.ParseBoolean() },
            { "BurstDelay", (parser, x) => x.BurstDelay = RandomVariable.Parse(parser) },
            { "BurstCount", (parser, x) => x.BurstCount = RandomVariable.Parse(parser) },
            { "InitialDelay", (parser, x) => x.InitialDelay = RandomVariable.Parse(parser) },
            { "UseMaximumHeight", (parser, x) => x.UseMaximumHeight = parser.ParseBoolean() },
        };

        private static T ParseModule<T>(IniParser parser, Dictionary<string, Func<IniParser, T>> moduleParseTable)
        {
            var moduleType = parser.GetNextToken();

            if (!moduleParseTable.TryGetValue(moduleType.Text, out var moduleParser))
            {
                throw new IniParseException($"Unknown module type: {moduleType.Text}", moduleType.Position);
            }

            return moduleParser(parser);
        }

        private static readonly Dictionary<string, Func<IniParser, FXParticleColor>> ColorModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleColor>>
        {
            { "DefaultColor", FXParticleColor.Parse }
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleAlpha>> AlphaModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleAlpha>>
        {
            { "DefaultAlpha", FXParticleAlpha.Parse }
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleUpdateBase>> UpdateModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleUpdateBase>>
        {
            { "DefaultUpdate", FXParticleUpdateDefault.Parse },
            { "RenderObjectUpdate", FXParticleUpdateRenderObject.Parse },
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticlePhysicsBase>> PhysicsModuleParseTable = new Dictionary<string, Func<IniParser, FXParticlePhysicsBase>>
        {
            { "DefaultPhysics", FXParticleDefaultPhysics.Parse }
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleDrawBase>> DrawModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleDrawBase>>
        {
            { "ButterflyDraw", FXParticleDrawButterfly.Parse },
            { "DefaultDraw", FXParticleDrawDefault.Parse },
            { "LightningDraw", FXParticleDrawLightning.Parse },
            { "QuadDraw", FXParticleDrawQuad.Parse },
            { "RenderObjectDraw", FXParticleDrawRenderObject.Parse },
            { "StreakDraw", FXParticleDrawStreak.Parse },
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleWind>> WindModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleWind>>
        {
            { "DefaultWind", FXParticleWind.Parse }
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleEmissionVelocityBase>> EmissionVelocityModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleEmissionVelocityBase>>
        {
            { "CylindricalEmissionVelocity", FXParticleEmissionVelocityCylinder.Parse },
            { "HemisphericalEmissionVelocity", FXParticleEmissionVelocityHemisphere.Parse },
            { "OrthoEmissionVelocity", FXParticleEmissionVelocityOrtho.Parse },
            { "OutwardEmissionVelocity", FXParticleEmissionVelocityOutward.Parse },
            { "SphericalEmissionVelocity", FXParticleEmissionVelocitySphere.Parse },
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleEmissionVolumeBase>> EmissionVolumeModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleEmissionVolumeBase>>
        {
            { "BoxEmissionVolume", FXParticleEmissionVolumeBox.Parse },
            { "CylinderEmissionVolume", FXParticleEmissionVolumeCylinder.Parse },
            { "LightningEmission", FXParticleEmissionVolumeLightning.Parse },
            { "LineEmissionVolume", FXParticleEmissionVolumeLine.Parse },
            { "PointEmissionVolume", FXParticleEmissionVolumePoint.Parse },
            { "SphereEmissionVolume", FXParticleEmissionVolumeSphere.Parse },
        };

        private static readonly Dictionary<string, Func<IniParser, FXParticleEventBase>> EventModuleParseTable = new Dictionary<string, Func<IniParser, FXParticleEventBase>>
        {
            { "TerrainCollision", FXParticleEventCollision.Parse },
        };

        public string Name { get; internal set; }

        public ParticleSystemPriority Priority { get; internal set; }
        public bool IsOneShot { get; internal set; }
        public ParticleSystemShader Shader { get; internal set; } = ParticleSystemShader.Additive;
        public ParticleSystemType Type { get; internal set; } = ParticleSystemType.Particle;
        public string ParticleName { get; internal set; }
        public string PerParticleAttachedSystem { get; internal set; }
        public string SlaveSystem { get; internal set; }
        public Vector3 SlavePosOffset { get; internal set; }
        public RandomVariable Lifetime { get; internal set; }
        public int SystemLifetime { get; internal set; }
        public int SortLevel { get; private set; }
        public RandomVariable Size { get; internal set; }
        public RandomVariable StartSizeRate { get; internal set; }
        public bool IsGroundAligned { get; internal set; }
        public bool IsEmitAboveGroundOnly { get; internal set; }
        public bool IsParticleUpTowardsEmitter { get; internal set; }
        public RandomVariable BurstDelay { get; internal set; }
        public RandomVariable BurstCount { get; internal set; }
        public RandomVariable InitialDelay { get; internal set; }
        public bool UseMaximumHeight { get; private set; }

        public FXParticleColor Colors { get; internal set; }
        public FXParticleAlpha Alpha { get; internal set; }
        public FXParticleUpdateBase Update { get; internal set; }
        public FXParticlePhysicsBase Physics { get; internal set; }
        public FXParticleDrawBase Draw { get; internal set; }
        public FXParticleWind Wind { get; internal set; }
        public FXParticleEmissionVelocityBase EmissionVelocity { get; internal set; }
        public FXParticleEmissionVolumeBase EmissionVolume { get; internal set; }
        public FXParticleEventBase Event { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleColor
    {
        internal static FXParticleColor Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleColor> FieldParseTable = new IniParseTable<FXParticleColor>
        {
            { "Color1", (parser, x) => x.Color1 = RgbColorKeyframe.Parse(parser) },
            { "Color2", (parser, x) => x.Color2 = RgbColorKeyframe.Parse(parser) },
            { "Color3", (parser, x) => x.Color3 = RgbColorKeyframe.Parse(parser) },
            { "Color4", (parser, x) => x.Color4 = RgbColorKeyframe.Parse(parser) },
            { "Color5", (parser, x) => x.Color5 = RgbColorKeyframe.Parse(parser) },
            { "Color6", (parser, x) => x.Color6 = RgbColorKeyframe.Parse(parser) },
            { "Color7", (parser, x) => x.Color7 = RgbColorKeyframe.Parse(parser) },
            { "Color8", (parser, x) => x.Color8 = RgbColorKeyframe.Parse(parser) },
            { "ColorScale", (parser, x) => x.ColorScale = RandomVariable.Parse(parser) },
        };

        public RgbColorKeyframe Color1 { get; internal set; } = new RgbColorKeyframe();
        public RgbColorKeyframe Color2 { get; internal set; }
        public RgbColorKeyframe Color3 { get; internal set; }
        public RgbColorKeyframe Color4 { get; internal set; }
        public RgbColorKeyframe Color5 { get; internal set; }
        public RgbColorKeyframe Color6 { get; internal set; }
        public RgbColorKeyframe Color7 { get; internal set; }
        public RgbColorKeyframe Color8 { get; internal set; }

        public RandomVariable ColorScale { get; internal set; }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleAlpha
    {
        internal static FXParticleAlpha Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleAlpha> FieldParseTable = new IniParseTable<FXParticleAlpha>
        {
            { "Alpha1", (parser, x) => x.Alpha1 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha2", (parser, x) => x.Alpha2 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha3", (parser, x) => x.Alpha3 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha4", (parser, x) => x.Alpha4 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha5", (parser, x) => x.Alpha5 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha6", (parser, x) => x.Alpha6 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha7", (parser, x) => x.Alpha7 = RandomAlphaKeyframe.Parse(parser) },
            { "Alpha8", (parser, x) => x.Alpha8 = RandomAlphaKeyframe.Parse(parser) },
        };

        public RandomAlphaKeyframe Alpha1 { get; internal set; }
        public RandomAlphaKeyframe Alpha2 { get; internal set; }
        public RandomAlphaKeyframe Alpha3 { get; internal set; }
        public RandomAlphaKeyframe Alpha4 { get; internal set; }
        public RandomAlphaKeyframe Alpha5 { get; internal set; }
        public RandomAlphaKeyframe Alpha6 { get; internal set; }
        public RandomAlphaKeyframe Alpha7 { get; internal set; }
        public RandomAlphaKeyframe Alpha8 { get; internal set; }
    }

    [AddedIn(SageGame.Bfme)]
    public abstract class FXParticleUpdateBase
    {

    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleUpdateDefault : FXParticleUpdateBase
    {
        internal static FXParticleUpdateDefault Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleUpdateDefault> FieldParseTable = new IniParseTable<FXParticleUpdateDefault>
        {
            { "SizeRate", (parser, x) => x.SizeRate = RandomVariable.Parse(parser) },
            { "SizeRateDamping", (parser, x) => x.SizeRateDamping = RandomVariable.Parse(parser) },
            { "AngleZ", (parser, x) => x.AngleZ = RandomVariable.Parse(parser) },
            { "AngularRateZ", (parser, x) => x.AngularRateZ = RandomVariable.Parse(parser) },
            { "AngularDamping", (parser, x) => x.AngularDamping = RandomVariable.Parse(parser) },
            { "Rotation", (parser, x) => x.Rotation = parser.ParseEnum<FXParticleSystemRotationType>() },
            { "AngularDampingXY", (parser, x) => x.AngularDampingXY = RandomVariable.Parse(parser) },
        };

        public RandomVariable SizeRate { get; internal set; }
        public RandomVariable SizeRateDamping { get; internal set; }
        public RandomVariable AngleZ { get; internal set; }
        public RandomVariable AngularRateZ { get; internal set; }
        public RandomVariable AngularDamping { get; internal set; }
        public FXParticleSystemRotationType Rotation { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public RandomVariable AngularDampingXY { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleUpdateRenderObject : FXParticleUpdateBase
    {
        internal static FXParticleUpdateRenderObject Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleUpdateRenderObject> FieldParseTable = new IniParseTable<FXParticleUpdateRenderObject>
        {
            { "AngleX", (parser, x) => x.AngleX = RandomVariable.Parse(parser) },
            { "AngularRateX", (parser, x) => x.AngularRateX = RandomVariable.Parse(parser) },
            { "AngleY", (parser, x) => x.AngleY = RandomVariable.Parse(parser) },
            { "AngularRateY", (parser, x) => x.AngularRateY = RandomVariable.Parse(parser) },
            { "AngleZ", (parser, x) => x.AngleZ = RandomVariable.Parse(parser) },
            { "AngularRateZ", (parser, x) => x.AngularRateZ = RandomVariable.Parse(parser) },
            { "AngularDamping", (parser, x) => x.AngularDamping = RandomVariable.Parse(parser) },
            { "StartSizeX", (parser, x) => x.StartSizeX = RandomVariable.Parse(parser) },
            { "StartSizeY", (parser, x) => x.StartSizeY = RandomVariable.Parse(parser) },
            { "StartSizeZ", (parser, x) => x.StartSizeZ = RandomVariable.Parse(parser) },
            { "SizeRateX", (parser, x) => x.SizeRateX = RandomVariable.Parse(parser) },
            { "SizeRateY", (parser, x) => x.SizeRateY = RandomVariable.Parse(parser) },
            { "SizeRateZ", (parser, x) => x.SizeRateZ = RandomVariable.Parse(parser) },
            { "SizeDampingX", (parser, x) => x.SizeDampingX = RandomVariable.Parse(parser) },
            { "SizeDampingY", (parser, x) => x.SizeDampingY = RandomVariable.Parse(parser) },
            { "SizeDampingZ", (parser, x) => x.SizeDampingZ = RandomVariable.Parse(parser) },
            { "Rotation", (parser, x) => x.Rotation = parser.ParseEnum<FXParticleSystemRotationType>() },
        };

        public RandomVariable AngleX { get; private set; }
        public RandomVariable AngularRateX { get; private set; }
        public RandomVariable AngleY { get; private set; }
        public RandomVariable AngularRateY { get; private set; }
        public RandomVariable AngleZ { get; private set; }
        public RandomVariable AngularRateZ { get; private set; }
        public RandomVariable AngularDamping { get; private set; }
        public RandomVariable StartSizeX { get; private set; }
        public RandomVariable StartSizeY { get; private set; }
        public RandomVariable StartSizeZ { get; private set; }
        public RandomVariable SizeRateX { get; private set; }
        public RandomVariable SizeRateY { get; private set; }
        public RandomVariable SizeRateZ { get; private set; }
        public RandomVariable SizeDampingX { get; private set; }
        public RandomVariable SizeDampingY { get; private set; }
        public RandomVariable SizeDampingZ { get; private set; }
        public FXParticleSystemRotationType Rotation { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public enum FXParticleSystemRotationType
    {
        [IniEnum("ROTATE_X")]
        RotateX,

        [IniEnum("ROTATE_Y")]
        RotateY,

        [IniEnum("ROTATE_Z")]
        RotateZ,

        [IniEnum("ROTATE_V")]
        RotateV
    }

    [AddedIn(SageGame.Bfme)]
    public abstract class FXParticlePhysicsBase
    {

    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDefaultPhysics : FXParticlePhysicsBase
    {
        internal static FXParticleDefaultPhysics Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDefaultPhysics> FieldParseTable = new IniParseTable<FXParticleDefaultPhysics>
        {
            { "Gravity", (parser, x) => x.Gravity = parser.ParseFloat() },
            { "VelocityDamping", (parser, x) => x.VelocityDamping = RandomVariable.Parse(parser) },
            { "DriftVelocity", (parser, x) => x.DriftVelocity = parser.ParseVector3() },
        };

        public float Gravity { get; internal set; }
        public RandomVariable VelocityDamping { get; internal set; }
        public Vector3 DriftVelocity { get; internal set; }
    }

    [AddedIn(SageGame.Bfme)]
    public abstract class FXParticleDrawBase
    {
        internal static readonly IniParseTable<FXParticleDrawBase> BaseFieldParseTable = new IniParseTable<FXParticleDrawBase>();
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDrawDefault : FXParticleDrawBase
    {
        internal static FXParticleDrawDefault Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDrawDefault> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleDrawDefault>());
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDrawRenderObject : FXParticleDrawBase
    {
        internal static FXParticleDrawRenderObject Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDrawRenderObject> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleDrawRenderObject>());
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDrawStreak : FXParticleDrawBase
    {
        internal static FXParticleDrawStreak Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDrawStreak> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleDrawStreak>());
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDrawButterfly : FXParticleDrawBase
    {
        internal static FXParticleDrawButterfly Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDrawButterfly> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleDrawButterfly>());
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDrawLightning : FXParticleDrawBase
    {
        internal static FXParticleDrawLightning Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDrawLightning> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleDrawLightning>
        {
            { "OffsetX", (parser, x) => x.OffsetX = RandomVariable.Parse(parser) },
            { "OffsetY", (parser, x) => x.OffsetY = RandomVariable.Parse(parser) },
            { "OffsetZ", (parser, x) => x.OffsetZ = RandomVariable.Parse(parser) },
        });

        public RandomVariable OffsetX { get; private set; }
        public RandomVariable OffsetY { get; private set; }
        public RandomVariable OffsetZ { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleDrawQuad : FXParticleDrawBase
    {
        internal static FXParticleDrawQuad Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleDrawQuad> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleDrawQuad>());
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleWind
    {
        internal static FXParticleWind Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleWind> FieldParseTable = new IniParseTable<FXParticleWind>
        {
            { "WindMotion", (parser, x) => x.WindMotion = parser.ParseEnum<ParticleSystemWindMotion>() },
            { "WindStrength", (parser, x) => x.WindStrength = parser.ParseFloat() },
            { "WindFullStrengthDist", (parser, x) => x.WindFullStrengthDist = parser.ParseFloat() },
            { "WindZeroStrengthDist", (parser, x) => x.WindZeroStrengthDist = parser.ParseFloat() },
            { "WindAngleChangeMin", (parser, x) => x.WindAngleChangeMin = parser.ParseFloat() },
            { "WindAngleChangeMax", (parser, x) => x.WindAngleChangeMax = parser.ParseFloat() },
            { "WindPingPongStartAngleMin", (parser, x) => x.WindPingPongStartAngleMin = parser.ParseFloat() },
            { "WindPingPongStartAngleMax", (parser, x) => x.WindPingPongStartAngleMax = parser.ParseFloat() },
            { "WindPingPongEndAngleMin", (parser, x) => x.WindPingPongEndAngleMin = parser.ParseFloat() },
            { "WindPingPongEndAngleMax", (parser, x) => x.WindPingPongEndAngleMax = parser.ParseFloat() },
            { "TurbulenceAmplitude", (parser, x) => x.TurbulenceAmplitude = parser.ParseFloat() },
            { "TurbulenceFrequency", (parser, x) => x.TurbulenceFrequency = parser.ParseFloat() }
        };

        public ParticleSystemWindMotion WindMotion { get; internal set; }
        public float WindStrength { get; internal set; }
        public float WindFullStrengthDist { get; internal set; }
        public float WindZeroStrengthDist { get; internal set; }
        public float WindAngleChangeMin { get; internal set; }
        public float WindAngleChangeMax { get; internal set; }
        public float WindPingPongStartAngleMin { get; internal set; }
        public float WindPingPongStartAngleMax { get; internal set; }
        public float WindPingPongEndAngleMin { get; internal set; }
        public float WindPingPongEndAngleMax { get; internal set; }
        public float TurbulenceAmplitude { get; internal set; }
        public float TurbulenceFrequency { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public abstract class FXParticleEmissionVelocityBase
    {
        public abstract Vector3 GetVelocity(in Vector3 direction, FXParticleEmissionVolumeBase volume);
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVelocityCylinder : FXParticleEmissionVelocityBase
    {
        internal static FXParticleEmissionVelocityCylinder Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVelocityCylinder> FieldParseTable = new IniParseTable<FXParticleEmissionVelocityCylinder>
        {
            { "Radial", (parser, x) => x.Radial = RandomVariable.Parse(parser) },
            { "Normal", (parser, x) => x.Normal = RandomVariable.Parse(parser) },
        };

        public RandomVariable Radial { get; internal set; }
        public RandomVariable Normal { get; internal set; }

        public override Vector3 GetVelocity(in Vector3 direction, FXParticleEmissionVolumeBase volume)
        {
            var velocity = Vector3.UnitX * Radial.GetRandomFloat();

            velocity = Vector3.Transform(velocity, Matrix4x4.CreateRotationZ(ParticleSystemUtility.GetRandomAngle()));

            velocity.Z = Normal.GetRandomFloat();

            return velocity;
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVelocityOrtho : FXParticleEmissionVelocityBase
    {
        internal static FXParticleEmissionVelocityOrtho Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVelocityOrtho> FieldParseTable = new IniParseTable<FXParticleEmissionVelocityOrtho>
        {
            { "X", (parser, x) => x.X = RandomVariable.Parse(parser) },
            { "Y", (parser, x) => x.Y = RandomVariable.Parse(parser) },
            { "Z", (parser, x) => x.Z = RandomVariable.Parse(parser) },
        };

        public RandomVariable X { get; internal set; }
        public RandomVariable Y { get; internal set; }
        public RandomVariable Z { get; internal set; }

        public override Vector3 GetVelocity(in Vector3 direction, FXParticleEmissionVolumeBase volume)
        {
            return new Vector3(
                X.GetRandomFloat(),
                Y.GetRandomFloat(),
                Z.GetRandomFloat());
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVelocityOutward : FXParticleEmissionVelocityBase
    {
        internal static FXParticleEmissionVelocityOutward Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVelocityOutward> FieldParseTable = new IniParseTable<FXParticleEmissionVelocityOutward>
        {
            { "Speed", (parser, x) => x.Speed = RandomVariable.Parse(parser) },
            { "OtherSpeed", (parser, x) => x.OtherSpeed = RandomVariable.Parse(parser) },
        };

        public RandomVariable Speed { get; internal set; }
        public RandomVariable OtherSpeed { get; internal set; }

        public override Vector3 GetVelocity(in Vector3 direction, FXParticleEmissionVolumeBase volume)
        {
            switch (volume)
            {
                case FXParticleEmissionVolumeCylinder _:
                    {
                        var velocity = direction;
                        velocity *= Speed.GetRandomFloat();
                        velocity += Vector3.UnitZ * OtherSpeed.GetRandomFloat();
                        return velocity;
                    }

                case FXParticleEmissionVolumeLine _:
                    {
                        var up = Vector3.UnitZ;
                        if (Vector3.Dot(direction, up) <= 0.001f)
                        {
                            up = Vector3.UnitY;
                        }
                        var dir1 = Vector3.Cross(direction, up);
                        var dir2 = Vector3.Cross(dir1, dir1);
                        dir1 *= Speed.GetRandomFloat();
                        dir2 *= OtherSpeed.GetRandomFloat();
                        return dir1 + dir2;
                    }

                case FXParticleEmissionVolumePoint _:
                    {
                        return ParticleSystemUtility.GetRandomDirection3D()
                            * Speed.GetRandomFloat();
                    }

                case FXParticleEmissionVolumeBox _:
                case FXParticleEmissionVolumeSphere _:
                    {
                        return direction * Speed.GetRandomFloat();
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [AddedIn(SageGame.Bfme)]
    public class FXParticleEmissionVelocitySphere : FXParticleEmissionVelocityBase
    {
        internal static FXParticleEmissionVelocitySphere Parse(IniParser parser) => parser.ParseBlock(SphereFieldParseTable);

        internal static readonly IniParseTable<FXParticleEmissionVelocitySphere> SphereFieldParseTable = new IniParseTable<FXParticleEmissionVelocitySphere>
        {
            { "Speed", (parser, x) => x.Speed = RandomVariable.Parse(parser) },
        };

        public RandomVariable Speed { get; internal set; }

        public override Vector3 GetVelocity(in Vector3 direction, FXParticleEmissionVolumeBase volume)
        {
            var velocity = ParticleSystemUtility.GetRandomDirection3D();
            velocity *= Speed.GetRandomFloat();
            return velocity;
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVelocityHemisphere : FXParticleEmissionVelocitySphere
    {
        internal new static FXParticleEmissionVelocityHemisphere Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVelocityHemisphere> FieldParseTable = SphereFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVelocityHemisphere>());

        public override Vector3 GetVelocity(in Vector3 direction, FXParticleEmissionVolumeBase volume)
        {
            var velocity = ParticleSystemUtility.GetRandomDirection3D();
            velocity.Z = Math.Abs(velocity.Z);

            velocity *= Speed.GetRandomFloat();

            return velocity;
        }
    }

    [AddedIn(SageGame.Bfme)]
    public abstract class FXParticleEmissionVolumeBase
    {
        internal static readonly IniParseTable<FXParticleEmissionVolumeBase> BaseFieldParseTable = new IniParseTable<FXParticleEmissionVolumeBase>
        {
            { "IsHollow", (parser, x) => x.IsHollow = parser.ParseBoolean() },
        };

        public bool IsHollow { get; internal set; }

        public abstract Ray GetRay();
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVolumeCylinder : FXParticleEmissionVolumeBase
    {
        internal static FXParticleEmissionVolumeCylinder Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVolumeCylinder> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVolumeCylinder>
        {
            { "Radius", (parser, x) => x.Radius = parser.ParseFloat() },
            { "Length", (parser, x) => x.Length = parser.ParseFloat() },
            { "Offset", (parser, x) => x.Offset = parser.ParseVector3() },
        });

        public float Radius { get; internal set; }
        public float Length { get; internal set; }
        public Vector3 Offset { get; private set; }

        public override Ray GetRay()
        {
            var angle = ParticleSystemUtility.GetRandomAngle();

            var radius = IsHollow
                ? Radius
                : ParticleSystemUtility.GetRandomFloat(0, Radius);

            var z = ParticleSystemUtility.GetRandomFloat(0, Length);

            var direction = Vector3.Transform(
                Vector3.UnitX,
                Matrix4x4.CreateRotationZ(angle));

            return new Ray(
                new Vector3(direction.X * radius, direction.Y * radius, z),
                direction);
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVolumeLine : FXParticleEmissionVolumeBase
    {
        internal static FXParticleEmissionVolumeLine Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVolumeLine> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVolumeLine>
        {
            { "StartPoint", (parser, x) => x.StartPoint = parser.ParseVector3() },
            { "EndPoint", (parser, x) => x.EndPoint = parser.ParseVector3() },
        });

        public Vector3 StartPoint { get; internal set; }
        public Vector3 EndPoint { get; internal set; }

        public override Ray GetRay()
        {
            var x = ParticleSystemUtility.GetRandomFloat(StartPoint.X, EndPoint.X);
            var y = ParticleSystemUtility.GetRandomFloat(StartPoint.Y, EndPoint.Y);
            var z = ParticleSystemUtility.GetRandomFloat(StartPoint.Z, EndPoint.Z);

            var position = new Vector3(x, y, z);

            var direction = Vector3.Normalize(EndPoint - StartPoint);

            return new Ray(position, direction);
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVolumeSphere : FXParticleEmissionVolumeBase
    {
        internal static FXParticleEmissionVolumeSphere Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVolumeSphere> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVolumeSphere>
        {
            { "Radius", (parser, x) => x.Radius = parser.ParseFloat() }
        });

        public float Radius { get; internal set; }

        public override Ray GetRay()
        {
            var direction = ParticleSystemUtility.GetRandomDirection3D();

            var radius = IsHollow
                ? Radius
                : ParticleSystemUtility.GetRandomFloat(0, Radius);

            return new Ray(
                direction * radius,
                direction);
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVolumeBox : FXParticleEmissionVolumeBase
    {
        internal static FXParticleEmissionVolumeBox Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVolumeBox> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVolumeBox>
        {
            { "HalfSize", (parser, x) => x.HalfSize = parser.ParseVector3() }
        });

        public Vector3 HalfSize { get; internal set; }

        public override Ray GetRay()
        {
            var x = ParticleSystemUtility.GetRandomFloat(-HalfSize.X, HalfSize.X);
            var y = ParticleSystemUtility.GetRandomFloat(-HalfSize.Y, HalfSize.Y);
            var z = ParticleSystemUtility.GetRandomFloat(0, HalfSize.Z * 2);

            var position = new Vector3(x, y, z);

            return new Ray(
                position,
                Vector3.Normalize(position));
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVolumePoint : FXParticleEmissionVolumeBase
    {
        internal static FXParticleEmissionVolumePoint Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVolumePoint> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVolumePoint>());

        public override Ray GetRay()
        {
            return new Ray(Vector3.Zero, Vector3.Zero);
        }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEmissionVolumeLightning : FXParticleEmissionVolumeBase
    {
        internal static FXParticleEmissionVolumeLightning Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEmissionVolumeLightning> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEmissionVolumeLightning>
        {
            { "EndPoint", (parser, x) => x.EndPoint = parser.ParseVector3() },
            { "Amplitude1", (parser, x) => x.Amplitude1 = RandomVariable.Parse(parser) },
            { "Frequency1", (parser, x) => x.Frequency1 = RandomVariable.Parse(parser) },
            { "Phase1", (parser, x) => x.Phase1 = RandomVariable.Parse(parser) },
            { "Phase2", (parser, x) => x.Phase2 = RandomVariable.Parse(parser) },
            { "Phase3", (parser, x) => x.Phase3 = RandomVariable.Parse(parser) },
        });

        public Vector3 EndPoint { get; private set; }
        public RandomVariable Amplitude1 { get; private set; }
        public RandomVariable Frequency1 { get; private set; }
        public RandomVariable Phase1 { get; private set; }
        public RandomVariable Phase2 { get; private set; }
        public RandomVariable Phase3 { get; private set; }

        public override Ray GetRay()
        {
            throw new NotImplementedException();
        }
    }

    [AddedIn(SageGame.Bfme)]
    public abstract class FXParticleEventBase
    {
        internal static readonly IniParseTable<FXParticleEventBase> BaseFieldParseTable = new IniParseTable<FXParticleEventBase>
        {
            { "EventFX", (parser, x) => x.EventFX = parser.ParseAssetReference() },
            { "PerParticle", (parser, x) => x.PerParticle = parser.ParseBoolean() },
            { "KillAfterEvent", (parser, x) => x.KillAfterEvent = parser.ParseBoolean() },
        };

        public string EventFX { get; private set; }
        public bool PerParticle { get; private set; }
        public bool KillAfterEvent { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public sealed class FXParticleEventCollision : FXParticleEventBase
    {
        internal static FXParticleEventCollision Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<FXParticleEventCollision> FieldParseTable = BaseFieldParseTable.Concat(new IniParseTable<FXParticleEventCollision>
        {
            { "HeightOffset", (parser, x) => x.HeightOffset = RandomVariable.Parse(parser) },
            { "OrientFXToTerrain", (parser, x) => x.OrientFXToTerrain = parser.ParseBoolean() },
        });

        public RandomVariable HeightOffset { get; private set; }
        public bool OrientFXToTerrain { get; private set; }
    }
}
