﻿using System;
using System.IO;
using System.Numerics;
using OpenSage.Content;
using OpenSage.Data.Ini;
using OpenSage.FileFormats;
using OpenSage.FX;

namespace OpenSage.Logic.Object
{
    public sealed class MissileAIUpdate : AIUpdate
    {
        private readonly MissileAIUpdateModuleData _moduleData;

        private MissileState _state;
        private TimeSpan _nextStateChangeTime;

        internal FXList DetonationFX { get; set; }

        internal MissileAIUpdate(GameObject gameObject, MissileAIUpdateModuleData moduleData)
            : base(gameObject, moduleData)
        {
            _moduleData = moduleData;

            _state = MissileState.Inactive;
        }

        internal override void Update(BehaviorUpdateContext context)
        {
            switch (_state)
            {
                case MissileState.Inactive:
                    _nextStateChangeTime = context.Time.TotalTime + _moduleData.IgnitionDelay;
                    _state = MissileState.WaitingForIgnition;
                    goto case MissileState.WaitingForIgnition;

                case MissileState.WaitingForIgnition:
                    if (context.Time.TotalTime > _nextStateChangeTime)
                    {
                        _moduleData.IgnitionFX?.Value?.Execute(
                            new FXListExecutionContext(
                                GameObject.Rotation,
                                GameObject.Translation,
                                context.GameContext));

                        if (_moduleData.DistanceToTravelBeforeTurning > 0)
                        {
                            var pointToReachBeforeTurning = context.GameObject.Translation
                                + Vector3.TransformNormal(Vector3.UnitX, context.GameObject.TransformMatrix) * _moduleData.DistanceToTravelBeforeTurning;
                            AddTargetPoint(pointToReachBeforeTurning);
                        }

                        // TODO: What to do if target doesn't exist anymore?
                        if (context.GameObject.CurrentWeapon.CurrentTarget != null)
                        {
                            AddTargetPoint(context.GameObject.CurrentWeapon.CurrentTarget.TargetPosition);
                        }

                        context.GameObject.Speed = _moduleData.InitialVelocity;

                        _state = MissileState.Moving;
                    }
                    break;

                case MissileState.Moving:
                    // TODO: TryToFollowTarget
                    BezierProjectileBehavior.CheckForHit(context, _moduleData.DetonateCallsKill, DetonationFX);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            base.Update(context);
        }

        internal override void Load(BinaryReader reader)
        {
            var version = reader.ReadVersion();
            if (version != 4)
            {
                throw new InvalidDataException();
            }

            base.Load(reader);

            var unknownPosition = reader.ReadVector3();

            var stateMaybe = reader.ReadUInt32();

            var frameSomething = reader.ReadUInt32();

            var unknownInt1 = reader.ReadInt32();
            if (unknownInt1 != int.MaxValue)
            {
                throw new InvalidDataException();
            }

            var launcherObjectId = reader.ReadUInt32();

            var objectIdSomething = reader.ReadUInt32();

            var unknownBool1 = reader.ReadBoolean();
            if (unknownBool1 != true)
            {
                throw new InvalidDataException();
            }

            var frameSomething2 = reader.ReadUInt32();

            var unknownFloat1 = reader.ReadSingle();

            var unknownFloat2 = reader.ReadSingle();
            if (unknownFloat2 != 99999.0f)
            {
                throw new InvalidDataException();
            }

            var weaponTemplateName = reader.ReadBytePrefixedAsciiString();
            var weaponTemplate = GameObject.GameContext.AssetLoadContext.AssetStore.WeaponTemplates.GetByName(weaponTemplateName);

            var exhaustParticleSystemTemplateName = reader.ReadBytePrefixedAsciiString();
            var exhaustParticleSystemTemplate = GameObject.GameContext.AssetLoadContext.AssetStore.FXParticleSystemTemplates.GetByName(exhaustParticleSystemTemplateName);

            var unknownBool2 = reader.ReadBoolean();

            var currentPositionMaybe = reader.ReadVector3();

            var unknownInt3 = reader.ReadInt32();
            if (unknownInt3 != 0)
            {
                throw new InvalidDataException();
            }

            var unknownInt4 = reader.ReadInt32(); // 1960
        }

        private enum MissileState
        {
            Inactive,
            WaitingForIgnition,
            Moving,
        }
    }

    public sealed class MissileAIUpdateModuleData : AIUpdateModuleData
    {
        internal new static MissileAIUpdateModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private new static readonly IniParseTable<MissileAIUpdateModuleData> FieldParseTable = AIUpdateModuleData.FieldParseTable.Concat(new IniParseTable<MissileAIUpdateModuleData>
        {
            { "TryToFollowTarget", (parser, x) => x.TryToFollowTarget = parser.ParseBoolean() },
            { "FuelLifetime", (parser, x) => x.FuelLifetime = parser.ParseInteger() },
            { "DetonateOnNoFuel", (parser, x) => x.DetonateOnNoFuel = parser.ParseBoolean() },
            { "InitialVelocity", (parser, x) => x.InitialVelocity = parser.ParseInteger() },
            { "IgnitionDelay", (parser, x) => x.IgnitionDelay = parser.ParseTimeMilliseconds() },
            { "DistanceToTravelBeforeTurning", (parser, x) => x.DistanceToTravelBeforeTurning = parser.ParseInteger() },
            { "DistanceToTargetBeforeDiving", (parser, x) => x.DistanceToTargetBeforeDiving = parser.ParseInteger() },
            { "DistanceToTargetForLock", (parser, x) => x.DistanceToTargetForLock = parser.ParseInteger() },
            { "GarrisonHitKillRequiredKindOf", (parser, x) => x.GarrisonHitKillRequiredKindOf = parser.ParseEnum<ObjectKinds>() },
            { "GarrisonHitKillForbiddenKindOf", (parser, x) => x.GarrisonHitKillForbiddenKindOf = parser.ParseEnum<ObjectKinds>() },
            { "GarrisonHitKillCount", (parser, x) => x.GarrisonHitKillCount = parser.ParseInteger() },
            { "GarrisonHitKillFX", (parser, x) => x.GarrisonHitKillFX = parser.ParseFXListReference() },
            { "DetonateCallsKill", (parser, x) => x.DetonateCallsKill = parser.ParseBoolean() },
            { "IgnitionFX", (parser, x) => x.IgnitionFX = parser.ParseFXListReference() },
            { "KillSelfDelay", (parser, x) => x.KillSelfDelay = parser.ParseInteger() },
            { "DistanceScatterWhenJammed", (parser, x) => x.DistanceScatterWhenJammed = parser.ParseInteger() },
        });

        public bool TryToFollowTarget { get; private set; }
        public int FuelLifetime { get; private set; }
        public bool DetonateOnNoFuel { get; private set; }
        public int InitialVelocity { get; private set; }
        public TimeSpan IgnitionDelay { get; private set; }
        public int DistanceToTravelBeforeTurning { get; private set; }
        public int DistanceToTargetBeforeDiving { get; private set; }
        public int DistanceToTargetForLock { get; private set; }
        public ObjectKinds GarrisonHitKillRequiredKindOf { get; private set; }
        public ObjectKinds GarrisonHitKillForbiddenKindOf { get; private set; }
        public int GarrisonHitKillCount { get; private set; }
        public LazyAssetReference<FXList> GarrisonHitKillFX { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool DetonateCallsKill { get; private set; }
        public LazyAssetReference<FXList> IgnitionFX { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public int KillSelfDelay { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public int DistanceScatterWhenJammed { get; private set; }

        internal override AIUpdate CreateAIUpdate(GameObject gameObject)
        {
            return new MissileAIUpdate(gameObject, this);
        }
    }
}
