using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.Audio;
using OpenSage.Content;
using OpenSage.Data.Ini;
using OpenSage.FX;
using OpenSage.Mathematics;

namespace OpenSage.Logic.Object
{
    public sealed class WeaponTemplate : BaseAsset
    {
        internal static WeaponTemplate Parse(IniParser parser)
        {
            return parser.ParseNamedBlock(
                (x, name) => x.SetNameAndInstanceId("Weapon", name),
                FieldParseTable);
        }

        private static readonly IniParseTable<WeaponTemplate> FieldParseTable = new IniParseTable<WeaponTemplate>
        {
            // Legacy
            { "PrimaryDamage", (parser, x) => x.SetDamageNuggetDamage(0, parser.ParseFloat()) },
            { "PrimaryDamageRadius", (parser, x) => x.SetDamageNuggetRadius(0, parser.ParseFloat()) },
            { "SecondaryDamage", (parser, x) => x.SetDamageNuggetDamage(1, parser.ParseFloat()) },
            { "SecondaryDamageRadius", (parser, x) => x.SetDamageNuggetRadius(1, parser.ParseFloat()) },
            {
                "DamageType",
                (parser, x) =>
                {
                    var damageType = parser.ParseEnum<DamageType>();
                    foreach (var damageNugget in x.GetDamageNuggets())
                    {
                        damageNugget.DamageType = damageType;
                    }
                }
            },
            {
                "DamageStatusType",
                (parser, x) =>
                {
                    var damageStatusType = parser.ParseEnum<DamageStatusType>();
                    foreach (var damageNugget in x.GetDamageNuggets())
                    {
                        damageNugget.DamageStatusType = damageStatusType;
                    }
                }
            },
            {
                "DeathType",
                (parser, x) =>
                {
                    var deathType = parser.ParseEnum<DeathType>();
                    foreach (var damageNugget in x.GetDamageNuggets())
                    {
                        damageNugget.DeathType = deathType;
                    }
                }
            },
            {
                "ProjectileObject",
                (parser, x) =>
                {
                    var projectileTemplate = parser.ParseObjectReference();
                    if (projectileTemplate != null)
                    {
                        x.Nuggets.Add(new ProjectileNugget
                        {
                            IsConvertedFromLegacyData = true,
                            ParentWeaponTemplate = x,
                            ProjectileTemplate = projectileTemplate
                        });
                    }
                }
            },
            { "ShockWaveAmount", (parser, x) => x.EnsureMetaImpactNugget().ShockWaveAmount = parser.ParseFloat() },
            { "ShockWaveRadius", (parser, x) => x.EnsureMetaImpactNugget().ShockWaveRadius = parser.ParseFloat() },
            { "ShockWaveTaperOff", (parser, x) => x.EnsureMetaImpactNugget().ShockWaveTaperOff = parser.ParseFloat() },

            { "ScatterRadius", (parser, x) => x.ScatterRadius = parser.ParseFloat() },
            { "ScatterRadiusVsInfantry", (parser, x) => x.ScatterRadiusVsInfantry = parser.ParseFloat() },
            { "ScatterTargetScalar", (parser, x) => x.ScatterTargetScalar = parser.ParseFloat() },
            { "ScatterTarget", (parser, x) => x.ScatterTargets.Add(parser.ParseVector2()) },
            { "LeechRangeWeapon", (parser, x) => x.LeechRangeWeapon = parser.ParseBoolean() },
            { "AttackRange", (parser, x) => x.AttackRange = parser.ParseFloat() },
            { "MinimumAttackRange", (parser, x) => x.MinimumAttackRange = parser.ParseFloat() },
            { "MinTargetPitch", (parser, x) => x.MinTargetPitch = parser.ParseInteger() },
            { "MaxTargetPitch", (parser, x) => x.MaxTargetPitch = parser.ParseInteger() },
            { "WeaponSpeed", (parser, x) => x.WeaponSpeed = parser.ParseFloat() },
            { "MinWeaponSpeed", (parser, x) => x.MinWeaponSpeed = parser.ParseFloat() },
            { "MaxWeaponSpeed", (parser, x) => x.MaxWeaponSpeed = parser.ParseInteger() },
            { "ScaleWeaponSpeed", (parser, x) => x.ScaleWeaponSpeed = parser.ParseBoolean() },
            { "WeaponRecoil", (parser, x) => x.WeaponRecoil = parser.ParseInteger() },
            { "FireFX", (parser, x) => x.FireFX = parser.ParseFXListReference() },
            { "PlayFXWhenStealthed", (parser, x) => x.PlayFXWhenStealthed = parser.ParseBoolean() },
            { "FireOCL", (parser, x) => x.FireOCL = parser.ParseAssetReference() },
            { "VeterancyFireFX", (parser, x) => x.VeterancyFireFX = ParseVeterancyAssetReference(parser) },

            { "ProjectileStreamName", (parser, x) => x.ProjectileStreamName = parser.ParseAssetReference() },
            { "ProjectileDetonationFX", (parser, x) => x.ProjectileDetonationFX = parser.ParseFXListReference() },
            { "ProjectileDetonationOCL", (parser, x) => x.ProjectileDetonationOCL = parser.ParseAssetReference() },
            { "ProjectileExhaust", (parser, x) => x.ProjectileExhaust = parser.ParseAssetReference() },
            { "VeterancyProjectileExhaust", (parser, x) => x.VeterancyProjectileExhaust = ParseVeterancyAssetReference(parser) },
            { "ProjectileFilterInContainer", (parser, x) => x.ProjectileFilterInContainer = ObjectFilter.Parse(parser) },
            { "ProjectileSelf", (parser, x) => x.ProjectileSelf = parser.ParseBoolean() },
            { "ProjectileCollidesWith", (parser, x) => x.ProjectileCollidesWith = parser.ParseEnumFlags<WeaponCollideTypes>() },

            { "FireSound", (parser, x) => x.FireSound = parser.ParseAudioEventReference() },
            { "FireSoundLoopTime", (parser, x) => x.FireSoundLoopTime = parser.ParseInteger() },
            { "SuspendFXDelay", (parser, x) => x.SuspendFXDelay = parser.ParseInteger() },
            { "RadiusDamageAffects", (parser, x) => x.RadiusDamageAffects = ObjectFilter.Parse(parser) },
            { "DelayBetweenShots", (parser, x) => x.CoolDownDelayBetweenShots = RangeDuration.Parse(parser) },
            { "ShotsPerBarrel", (parser, x) => x.ShotsPerBarrel = parser.ParseInteger() },
            { "ClipSize", (parser, x) => x.ClipSize = parser.ParseInteger() },
            { "ClipReloadTime", (parser, x) => x.ClipReloadTime = RangeDuration.Parse(parser) },
            { "AutoReloadWhenIdle", (parser, x) => x.AutoReloadWhenIdle = new RangeDuration(parser.ParseTimeMilliseconds()) },
            { "AutoReloadsClip", (parser, x) => x.AutoReloadsClip = parser.ParseEnum<WeaponReloadType>() },
            { "ContinuousFireOne", (parser, x) => x.ContinuousFireOne = parser.ParseInteger() },
            { "ContinuousFireTwo", (parser, x) => x.ContinuousFireTwo = parser.ParseInteger() },
            { "ContinuousFireCoast", (parser, x) => x.ContinuousFireCoast = parser.ParseInteger() },
            { "PreAttackDelay", (parser, x) => x.PreAttackDelay = new RangeDuration(parser.ParseTimeMilliseconds()) },
            { "PreAttackType", (parser, x) => x.PreAttackType = parser.ParseEnum<WeaponPrefireType>() },
            { "ContinueAttackRange", (parser, x) => x.ContinueAttackRange = parser.ParseInteger() },
            { "AcceptableAimDelta", (parser, x) => x.AcceptableAimDelta = parser.ParseFloat() },
            { "HitPercentage", (parser, x) => x.HitPercentage = parser.ParsePercentage() },
            { "PreAttackRandomAmount", (parser, x) => x.PreAttackRandomAmount = parser.ParseInteger() },
            { "IsAimingWeapon", (parser, x) => x.IsAimingWeapon = parser.ParseBoolean() },
            { "FXTrigger", (parser, x) => x.FxTrigger = parser.ParseEnumFlags<ObjectKinds>() },
            { "HitStoredTarget", (parser, x) => x.HitStoredTarget = parser.ParseBoolean() },
            { "PreferredTargetBone", (parser, x) => x.PreferredTargetBone = parser.ParseAssetReference() },
            { "MeleeWeapon", (parser, x) => x.MeleeWeapon = parser.ParseBoolean() },
            { "IdleAfterFiringDelay", (parser, x) => x.IdleAfterFiringDelay = new RangeDuration(parser.ParseTimeMilliseconds()) },
            { "HitPassengerPercentage", (parser, x) => x.HitPassengerPercentage = parser.ParsePercentage() },
            { "CanBeDodged", (parser, x) => x.CanBeDodged = parser.ParseBoolean() },
            { "OverrideVoiceAttackSound", (parser, x) => x.OverrideVoiceAttackSound = parser.ParseAssetReference() },
            { "NoVictimNeeded", (parser, x) => x.NoVictimNeeded = parser.ParseBoolean() },
            { "CanFireWhileMoving", (parser, x) => x.CanFireWhileMoving = parser.ParseBoolean() },
            { "RequireFollowThru", (parser, x) => x.RequireFollowThru = parser.ParseBoolean() },
            { "ScatterIndependently", (parser, x) => x.ScatterIndependently = parser.ParseBoolean() },
            { "PreAttackFX", (parser, x) => x.PreAttackFX = parser.ParseAssetReference() },
            { "AimDirection", (parser, x) => x.AimDirection = parser.ParseFloat() },
            { "HoldAfterFiringDelay", (parser, x) => x.HoldAfterFiringDelay = parser.ParseInteger() },
            { "FinishAttackOnceStarted", (parser, x) => x.FinishAttackOnceStarted = parser.ParseBoolean() },
            { "ShareTimers", (parser, x) => x.ShareTimers = parser.ParseBoolean() },
            { "DisableScatterForTargetsOnWall", (parser, x) => x.DisableScatterForTargetsOnWall = parser.ParseBoolean() },
            { "ShouldPlayUnderAttackEvaEvent", (parser, x) => x.ShouldPlayUnderAttackEvaEvent = parser.ParseBoolean() },
            { "CanSwoop", (parser, x) => x.CanSwoop = parser.ParseBoolean() },
            { "PassengerProportionalAttack", (parser, x) => x.PassengerProportionalAttack = parser.ParseBoolean() },
            { "MaxAttackPassengers", (parser, x) => x.MaxAttackPassengers = parser.ParseInteger() },
            { "ChaseWeapon", (parser, x) => x.ChaseWeapon = parser.ParseBoolean() },
            { "CanFireWhileCharging", (parser, x) => x.CanFireWhileCharging = parser.ParseBoolean() },
            { "IgnoreLinearFirstTarget", (parser, x) => x.IgnoreLinearFirstTarget = parser.ParseBoolean() },
            { "LinearTarget", (parser, x) => x.LinearTargets.Add(LinearTarget.Parse(parser)) },
            { "ForceDisplayPercentReady", (parser, x) => x.ForceDisplayPercentReady = parser.ParseBoolean() },
            { "RotatingTurret", (parser, x) => x.RotatingTurret = parser.ParseBoolean() },
            { "RangeBonusMinHeight", (parser, x) => x.RangeBonusMinHeight = parser.ParseInteger() },
            { "RangeBonus", (parser, x) => x.RangeBonus = parser.ParseInteger() },
            { "RangeBonusPerFoot", (parser, x) => x.RangeBonusPerFoot = parser.ParseInteger() },
            { "FireFlankFX", (parser, x) => x.FireFlankFX = parser.ParseAssetReference() },
            { "InstantLoadClipOnActivate", (parser, x) => x.InstantLoadClipOnActivate = parser.ParseBoolean() },
            { "BombardType", (parser, x) => x.BombardType = parser.ParseBoolean() },
            { "OverrideVoiceEnterStateAttackSound", (parser, x) => x.OverrideVoiceEnterStateAttackSound = parser.ParseAssetReference() },
            { "HoldDuringReload", (parser, x) => x.HoldDuringReload = parser.ParseBoolean() },
            { "LockWhenUsing", (parser, x) => x.LockWhenUsing = parser.ParseBoolean() },
            { "UseInnateAttributes", (parser, x) => x.UseInnateAttributes = parser.ParseBoolean() },
            { "AntiMask", (parser, x) => x.AntiMask = parser.ParseEnumFlags<WeaponAntiFlags>() },
            { "ShowsAmmoPips", (parser, x) => x.ShowsAmmoPips = parser.ParseBoolean() },
            { "LaserName", (parser, x) => x.LaserName = parser.ParseAssetReference() },
            { "LaserBoneName", (parser, x) => x.LaserBoneName = parser.ParseBoneName() },
            { "DamageDealtAtSelfPosition", (parser, x) => x.DamageDealtAtSelfPosition = parser.ParseBoolean() },
            { "RequestAssistRange", (parser, x) => x.RequestAssistRange = parser.ParseInteger() },
            { "AllowAttackGarrisonedBldgs", (parser, x) => x.AllowAttackGarrisonedBldgs = parser.ParseBoolean() },
            { "CapableOfFollowingWaypoints", (parser, x) => x.CapableOfFollowingWaypoints = parser.ParseBoolean() },
            { "WeaponBonus", (parser, x) => x.WeaponBonuses.Parse(parser) },
            { "HistoricBonusTime", (parser, x) => x.HistoricBonusTime = parser.ParseInteger() },
            { "HistoricBonusCount", (parser, x) => x.HistoricBonusCount = parser.ParseInteger() },
            { "HistoricBonusRadius", (parser, x) => x.HistoricBonusRadius = parser.ParseInteger() },
            { "HistoricBonusWeapon", (parser, x) => x.HistoricBonusWeapon = parser.ParseAssetReference() },
            { "MissileCallsOnDie", (parser, x) => x.MissileCallsOnDie = parser.ParseBoolean() },
            { "FiringDuration", (parser, x) => x.FiringDuration = new RangeDuration(parser.ParseTimeMilliseconds()) },

            // Anti flags
            { "AntiSmallMissile", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiSmallMissile, parser.ParseBoolean()) },
            { "AntiProjectile", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiProjectile, parser.ParseBoolean()) },
            { "AntiAirborneVehicle", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiAirborneVehicle, parser.ParseBoolean()) },
            { "AntiAirborneInfantry", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiAirborneInfantry, parser.ParseBoolean()) },
            { "AntiGround", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiGround, parser.ParseBoolean()) },
            { "AntiBallisticMissile", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiBallisticMissile, parser.ParseBoolean()) },
            { "AntiMine", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiMine, parser.ParseBoolean()) },
            { "AntiStructure", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiStructure, parser.ParseBoolean()) },
            { "AntiAirborneMonster", (parser, x) => x.SetAntiMaskFlag(WeaponAntiFlags.AntiAirborneMonster, parser.ParseBoolean()) },

            // Nuggets
            { "ProjectileNugget", (parser, x) => x.Nuggets.Add(ProjectileNugget.Parse(parser)) },
            { "DamageNugget", (parser, x) => x.Nuggets.Add(DamageNugget.Parse(parser)) },
            { "MetaImpactNugget", (parser, x) => x.Nuggets.Add(MetaImpactNugget.Parse(parser)) },
            { "SpecialModelConditionNugget", (parser, x) => x.Nuggets.Add(SpecialModelConditionNugget.Parse(parser)) },
            { "ParalyzeNugget", (parser, x) => x.Nuggets.Add(ParalyzeNugget.Parse(parser)) },
            { "HordeAttackNugget", (parser, x) => x.Nuggets.Add(HordeAttackNugget.Parse(parser)) },
            { "SpawnAndFadeNugget", (parser, x) => x.Nuggets.Add(SpawnAndFadeNugget.Parse(parser)) },
            { "AttributeModifierNugget", (parser, x) => x.Nuggets.Add(AttributeModifierNugget.Parse(parser)) },
            { "DamageFieldNugget", (parser, x) => x.Nuggets.Add(DamageFieldNugget.Parse(parser)) },
            { "GrabNugget", (parser, x) => x.Nuggets.Add(GrabNugget.Parse(parser)) },
            { "DOTNugget", (parser, x) => x.Nuggets.Add(DOTNugget.Parse(parser)) },
            { "SlaveAttackNugget", (parser, x) => x.Nuggets.Add(SlaveAttackNugget.Parse(parser)) },
            { "FireLogicNugget", (parser, x) => x.Nuggets.Add(FireLogicNugget.Parse(parser)) },
            { "EmotionWeaponNugget", (parser, x) => x.Nuggets.Add(EmotionWeaponNugget.Parse(parser)) },
            { "OpenGateNugget", (parser, x) => x.Nuggets.Add(OpenGateNugget.Parse(parser)) },
            { "WeaponOCLNugget", (parser, x) => x.Nuggets.Add(WeaponOCLNugget.Parse(parser)) },
            { "LuaEventNugget", (parser, x) => x.Nuggets.Add(LuaEventNugget.Parse(parser)) },
            { "DamageContainedNugget", (parser, x) => x.Nuggets.Add(DamageContainedNugget.Parse(parser)) },
            { "StealMoneyNugget", (parser, x) => x.Nuggets.Add(StealMoneyNugget.Parse(parser)) },
            { "ClearNuggets", (parser, x) => x.Nuggets.Clear() },
        };

        private static string ParseVeterancyAssetReference(IniParser parser)
        {
            var tokenPosition = parser.CurrentPosition;
            var identifier = parser.ParseIdentifier();
            if (identifier != "HEROIC")
            {
                throw new IniParseException($"Unexpected identifier: {identifier}", tokenPosition);
            }
            return parser.ParseAssetReference();
        }

        private IEnumerable<DamageNugget> GetDamageNuggets()
        {
            return Nuggets.OfType<DamageNugget>();
        }

        private DamageNugget EnsureDammageNugget(int index)
        {
            var damageNuggets = GetDamageNuggets().ToList();
            while (damageNuggets.Count < index + 1)
            {
                var damageNugget = new DamageNugget();
                Nuggets.Add(damageNugget);
                damageNuggets.Add(damageNugget);
            }
            return damageNuggets[index];
        }

        private void SetDamageNuggetDamage(int index, float value)
        {
            var damageNugget = EnsureDammageNugget(index);
            damageNugget.Damage = value;
        }

        private void SetDamageNuggetRadius(int index, float value)
        {
            var damageNugget = EnsureDammageNugget(index);
            damageNugget.Radius = value;
        }

        private MetaImpactNugget EnsureMetaImpactNugget()
        {
            var metaImpactNugget = Nuggets.OfType<MetaImpactNugget>().FirstOrDefault();
            if (metaImpactNugget == null)
            {
                Nuggets.Add(metaImpactNugget = new MetaImpactNugget());
            }
            return metaImpactNugget;
        }

        public float ScatterRadius { get; private set; }
        public float ScatterRadiusVsInfantry { get; private set; }
        public float ScatterTargetScalar { get; private set; }
        public List<Vector2> ScatterTargets { get; } = new List<Vector2>();
        public bool LeechRangeWeapon { get; private set; }
        public float AttackRange { get; private set; }
        public float MinimumAttackRange { get; private set; }
        public int MinTargetPitch { get; private set; }
        public int MaxTargetPitch { get; private set; }

        public float WeaponSpeed { get; private set; }
        public float MinWeaponSpeed { get; private set; }
        public bool ScaleWeaponSpeed { get; private set; }
        public int WeaponRecoil { get; private set; }
        public LazyAssetReference<FXList> FireFX { get; private set; }
        public bool PlayFXWhenStealthed { get; private set; }
        public string FireOCL { get; private set; }
        public string VeterancyFireFX { get; private set; }
        public LazyAssetReference<FXList> ProjectileDetonationFX { get; private set; }
        public string ProjectileDetonationOCL { get; private set; }
        public string ProjectileExhaust { get; private set; }
        public string VeterancyProjectileExhaust { get; private set; }
        public string ProjectileStreamName { get; private set; }
        public LazyAssetReference<BaseAudioEventInfo> FireSound { get; private set; }
        public int FireSoundLoopTime { get; private set; }
        public int SuspendFXDelay { get; private set; }
        public ObjectFilter RadiusDamageAffects { get; private set; }
        public RangeDuration CoolDownDelayBetweenShots { get; private set; }
        public int ShotsPerBarrel { get; private set; }
        public int ClipSize { get; private set; }
        public RangeDuration ClipReloadTime { get; private set; }
        public RangeDuration AutoReloadWhenIdle { get; private set; }
        public WeaponReloadType AutoReloadsClip { get; private set; } = WeaponReloadType.Auto;
        public int ContinuousFireOne { get; private set; }
        public int ContinuousFireTwo { get; private set; }
        public int ContinuousFireCoast { get; private set; }
        public RangeDuration PreAttackDelay { get; private set; }
        public WeaponPrefireType PreAttackType { get; private set; }
        public int ContinueAttackRange { get; private set; }
        public float AcceptableAimDelta { get; private set; }
        public bool ShowsAmmoPips { get; private set; }
        public string LaserName { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public string LaserBoneName { get; private set; }

        public bool DamageDealtAtSelfPosition { get; private set; }
        public int RequestAssistRange { get; private set; }
        public bool AllowAttackGarrisonedBldgs { get; private set; }
        public bool CapableOfFollowingWaypoints { get; private set; }
        public WeaponBonusSet WeaponBonuses { get; } = new WeaponBonusSet();
        public WeaponCollideTypes ProjectileCollidesWith { get; internal set; }
        public int HistoricBonusTime { get; private set; }
        public int HistoricBonusCount { get; private set; }
        public int HistoricBonusRadius { get; private set; }
        public string HistoricBonusWeapon { get; private set; }

        [AddedIn(SageGame.CncGeneralsZeroHour)]
        public bool MissileCallsOnDie { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public RangeDuration FiringDuration { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public List<WeaponEffectNugget> Nuggets { get; } = new List<WeaponEffectNugget>();

        [AddedIn(SageGame.Bfme2)]
        public int MaxWeaponSpeed { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public Percentage HitPercentage { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public int PreAttackRandomAmount { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool IsAimingWeapon { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public ObjectKinds FxTrigger { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool HitStoredTarget { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public string PreferredTargetBone { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool MeleeWeapon { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public RangeDuration IdleAfterFiringDelay { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool ProjectileSelf { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public Percentage HitPassengerPercentage { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool CanBeDodged { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public string OverrideVoiceAttackSound { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public ObjectFilter ProjectileFilterInContainer { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool NoVictimNeeded { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool CanFireWhileMoving { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool RequireFollowThru { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool ScatterIndependently { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public string PreAttackFX { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public float AimDirection { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public int HoldAfterFiringDelay { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool FinishAttackOnceStarted { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool ShareTimers { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool DisableScatterForTargetsOnWall { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool ShouldPlayUnderAttackEvaEvent { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool CanSwoop { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool PassengerProportionalAttack { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public int MaxAttackPassengers { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool ChaseWeapon { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool CanFireWhileCharging { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool IgnoreLinearFirstTarget { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public List<LinearTarget> LinearTargets { get; } = new List<LinearTarget>();

        [AddedIn(SageGame.Bfme)]
        public bool ForceDisplayPercentReady { get; private set; }

        [AddedIn(SageGame.Bfme)]
        public bool RotatingTurret { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public int RangeBonusMinHeight { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public int RangeBonus { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public int RangeBonusPerFoot { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public string FireFlankFX { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool InstantLoadClipOnActivate { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool BombardType { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public string OverrideVoiceEnterStateAttackSound { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool HoldDuringReload { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool LockWhenUsing { get; private set; }

        [AddedIn(SageGame.Bfme2)]
        public bool UseInnateAttributes { get; private set; }

        public WeaponAntiFlags AntiMask { get; private set; } = WeaponAntiFlags.AntiGround;

        private void SetAntiMaskFlag(WeaponAntiFlags flag, bool set)
        {
            if (set)
            {
                AntiMask |= flag;
            }
            else
            {
                AntiMask &= ~flag;
            }
        }
    }

    public readonly struct RangeDuration
    {
        internal static RangeDuration Parse(IniParser parser)
        {
            var token = parser.GetNextToken(IniParser.SeparatorsColon);
            if (parser.IsFloat(token))
            {
                var value = TimeSpan.FromMilliseconds(parser.ScanFloat(token));
                return new RangeDuration(value);
            }

            if (token.Text.ToUpperInvariant() != "MIN")
            {
                throw new IniParseException($"Unexpected range duration: {token.Text}", token.Position);
            }

            var minValue = TimeSpan.FromMilliseconds(parser.ScanInteger(parser.GetNextToken()));

            return new RangeDuration(
                minValue,
                TimeSpan.FromMilliseconds(parser.ParseAttributeInteger("Max")));
        }

        public readonly TimeSpan Min;
        public readonly TimeSpan Max;

        public RangeDuration(TimeSpan value)
        {
            Min = Max = value;
        }

        public RangeDuration(TimeSpan min, TimeSpan max)
        {
            Min = min;
            Max = max;
        }
    }

    [AddedIn(SageGame.Bfme)]
    public class DamageScalar
    {
        internal static DamageScalar Parse(IniParser parser)
        {
            var result = new DamageScalar
            {
                Scalar = parser.ParsePercentage()
            };
            var token = parser.PeekNextTokenOptional();
            if (token.HasValue)
            {
                result.Targets = ObjectFilter.Parse(parser);
            }
            return result;
        }

        public Percentage Scalar { get; private set; }
        public ObjectFilter Targets { get; private set; }
    }

    [AddedIn(SageGame.Bfme)]
    public class LinearTarget
    {
        internal static LinearTarget Parse(IniParser parser)
        {
            var offset = parser.ParseVector2();
            var t = parser.ParseAttributeFloat("T");
            return new LinearTarget { Offset = offset, T = t};
        }

        public Vector2 Offset { get; private set; }
        public float T { get; private set; } //not sure what this is
    }


    public enum WeaponReloadType
    {
        [IniEnum("No")]
        None,

        [IniEnum("Yes")]
        Auto,

        [IniEnum("RETURN_TO_BASE")]
        ReturnToBase
    }

    public enum WeaponPrefireType
    {
        /// <summary>
        /// Do the delay each time a new target is attacked.
        /// </summary>
        [IniEnum("PER_ATTACK")]
        PerAttack,

        /// <summary>
        /// Do the delay after every clip reload.
        /// </summary>
        [IniEnum("PER_CLIP")]
        PerClip,

        /// <summary>
        /// Do the delay every single shot.
        /// </summary>
        [IniEnum("PER_SHOT")]
        PerShot,

        /// <summary>
        /// 
        /// </summary>
        [IniEnum("PER_POSITION"), AddedIn(SageGame.Bfme)]
        PerPosition
    }

    public enum DeathType
    {
        [IniEnum("NORMAL")]
        Normal,

        [IniEnum("EXPLODED")]
        Exploded,

        [IniEnum("LASERED")]
        Lasered,

        [IniEnum("BURNED")]
        Burned,

        [IniEnum("SUICIDED")]
        Suicided,

        [IniEnum("POISONED")]
        Poisoned,

        [IniEnum("POISONED_BETA")]
        PoisonedBeta,

        [IniEnum("POISONED_GAMMA"), AddedIn(SageGame.CncGeneralsZeroHour)]
        PoisonedGamma,

        [IniEnum("CRUSHED")]
        Crushed,

        [IniEnum("TOPPLED")]
        Toppled,

        [IniEnum("SPLATTED")]
        Splatted,

        [IniEnum("FLOODED")]
        Flooded,

        [IniEnum("DETONATED")]
        Detonated,

        [IniEnum("EXTRA_4"), AddedIn(SageGame.CncGeneralsZeroHour)]
        Extra4,

        [IniEnum("FADED"), AddedIn(SageGame.Bfme)]
        Faded,

        [IniEnum("KNOCKBACK"), AddedIn(SageGame.Bfme)]
        Knockback,

        [IniEnum("EXTRA_2"), AddedIn(SageGame.Bfme)]
        Extra2,

        [IniEnum("SUPERNATURAL"), AddedIn(SageGame.Bfme)]
        Supernatural,

        [IniEnum("SLAUGHTERED"), AddedIn(SageGame.Bfme2)]
        Slaughtered,

        [IniEnum("EXTRA_1"), AddedIn(SageGame.Bfme)]
        Extra1,

        [IniEnum("EXTRA_3"), AddedIn(SageGame.Bfme)]
        Extra3,

        [IniEnum("EXTRA_5"), AddedIn(SageGame.Bfme2Rotwk)]
        Extra5,

        [IniEnum("EXTRA_6"), AddedIn(SageGame.Bfme2Rotwk)]
        Extra6,

        [IniEnum("EXTRA_7"), AddedIn(SageGame.Bfme2Rotwk)]
        Extra7,

        [IniEnum("EXTRA_8"), AddedIn(SageGame.Bfme2Rotwk)]
        Extra8,
    }

    [Flags]
    public enum WeaponCollideTypes
    {
        None = 0,

        [IniEnum("STRUCTURES")]
        Structures = 1 << 0,
        
        [IniEnum("WALLS")]
        Walls = 1 << 1,

        [IniEnum("ENEMIES")]
        Enemies = 1 << 2,

        [IniEnum("SHRUBBERY")]
        Shrubbery = 1 << 3,

        [IniEnum("ALLIES")]
        Allies = 1 << 4,

        [IniEnum("NEUTRAL")]
        Neutral = 1 << 5,

        [IniEnum("MONSTERS")]
        Monsters = 1 << 6,
    }

    public enum DamageStatusType
    {
        [IniEnum("FAERIE_FIRE")]
        FaerieFire,
    }

    [AddedIn(SageGame.Bfme2)]
    public enum FireLogicType
    {
        [IniEnum("INCREASE_BURN_RATE")]
        IncreaseBurnRate,

        [IniEnum("INCREASE_FUEL")]
        IncreaseFuel,

        [IniEnum("INCREASE_FUEL_ON_EXISTING_FIRE")]
        IncreaseFuelOnExistingFire,

        [IniEnum("INCREASE_BURN_RATE_ON_EXISTING_FIRE")]
        IncreaseBurnRateOnExistingFire,

        [IniEnum("DECREASE_BURN_RATE")]
        DecreaseBurnRate,
    }

    [Flags]
    public enum WeaponAntiFlags
    {
        None                 = 0,
        AntiAirborneVehicle  = 1 << 0,
        AntiGround           = 1 << 1,
        AntiProjectile       = 1 << 2,
        AntiSmallMissile     = 1 << 3,
        AntiMine             = 1 << 4,
        AntiAirborneInfantry = 1 << 5,
        AntiBallisticMissile = 1 << 6,
        AntiParachute        = 1 << 7,
        AntiStructure        = 1 << 8,
        AntiAirborneMonster  = 1 << 9,
    }

    public static class WeaponAntiFlagsExtensions
    {
        public static bool CanAttackObject(this WeaponAntiFlags weapon, GameObject obj)
        {
            var kinds = obj.Definition.KindOf;
            if (obj.IsAirborne())
            {
                if (weapon.HasFlag(WeaponAntiFlags.AntiAirborneVehicle) && kinds.Get(ObjectKinds.Aircraft) &&
                    kinds.Get(ObjectKinds.Vehicle))
                {
                    return true;
                }
                if (weapon.HasFlag(WeaponAntiFlags.AntiGround) && !kinds.Get(ObjectKinds.Aircraft))
                {
                    return true;
                }

                if (weapon.HasFlag(WeaponAntiFlags.AntiProjectile) && kinds.Get(ObjectKinds.Projectile))
                {
                    return true;
                }

                if (weapon.HasFlag(WeaponAntiFlags.AntiSmallMissile) &&
                    kinds.Get(ObjectKinds.SmallMissile))
                {
                    return true;
                }

                // TODO: this probably isn't correct - investigate distinguishing paratroopers from regular infantry?
                if (weapon.HasFlag(WeaponAntiFlags.AntiAirborneInfantry) &&
                    kinds.Get(ObjectKinds.Infantry))
                {
                    return true;
                }

                if (weapon.HasFlag(WeaponAntiFlags.AntiBallisticMissile) &&
                    kinds.Get(ObjectKinds.BallisticMissile))
                {
                    return true;
                }

                if (weapon.HasFlag(WeaponAntiFlags.AntiParachute) && kinds.Get(ObjectKinds.Parachute))
                {
                    return true;
                }

                return false;
            }

            // this should handle when an aircraft is on the ground, since we previously checked if it was airborne
            if (weapon.HasFlag(WeaponAntiFlags.AntiGround) && (kinds.Get(ObjectKinds.Vehicle) ||
                                                               kinds.Get(ObjectKinds.Infantry) ||
                                                               kinds.Get(ObjectKinds.Structure)))
            {
                return true;
            }

            if (weapon.HasFlag(WeaponAntiFlags.AntiMine) && kinds.Get(ObjectKinds.Mine))
            {
                return true;
            }

            if (weapon.HasFlag(WeaponAntiFlags.AntiStructure) && kinds.Get(ObjectKinds.Structure))
            {
                return true;
            }

            return false;
        }
    }
}
