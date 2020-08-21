﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.Data.Ini;
using OpenSage.Logic.Object.Production;

namespace OpenSage.Logic.Object
{
    public sealed class ParkingPlaceBehaviour : BehaviorModule, IProductionExit
    {
        private readonly ParkingPlaceBehaviorModuleData _moduleData;
        private readonly GameObject _gameObject;
        private readonly GameContext _gameContext;
        public GameObject[] ParkingSlots { get; }
        private readonly Dictionary<string, bool> _blockedBones;

        internal ParkingPlaceBehaviour(ParkingPlaceBehaviorModuleData moduleData, GameObject gameObject, GameContext context)
        {
            _moduleData = moduleData;
            _gameObject = gameObject;
            _gameContext = context;
            ParkingSlots = new GameObject[_moduleData.NumRows * _moduleData.NumCols];
            _blockedBones = new Dictionary<string, bool>();
        }

        public bool CanProduceObject(ObjectDefinition definition, IReadOnlyList<ProductionJob> productionQueue)
        {
            if (ProducedAtHelipad(definition)) return true;

            var numInQueue = productionQueue.Count(job => ((JetAIUpdateModuleData)job.ObjectDefinition.AIUpdate).NeedsRunway);
            var slotsAvailable = ParkingSlots.Count(x => x == null);
            return slotsAvailable > numInQueue;
        }

        public bool ProducedAtHelipad(ObjectDefinition definition)
        {
            return definition.KindOf.Get(ObjectKinds.ProducedAtHelipad);
        }

        public int NextFreeSlot()
        {
            for (var index = 0; index < ParkingSlots.Length; index++)
            {
                if (ParkingSlots[index] == null)
                {
                    return index;
                }
            }

            return -1;
        }

        public int GetCorrespondingSlot(GameObject gameObject)
        {
            for (var index = 0; index < ParkingSlots.Length; index++)
            {
                if (ParkingSlots[index] == gameObject)
                {
                    return index;
                }
            }

            return -1;
        }

        public void ClearObjectFromSlot(GameObject gameObject)
        {
            for (var index = 0; index < ParkingSlots.Length; index++)
            {
                if (ParkingSlots[index] == gameObject)
                {
                    ParkingSlots[index] = null;
                    return;
                }
            }
        }

        public Vector3 GetUnitCreatePoint() => throw new InvalidOperationException("not supported here");

        public Transform GetUnitCreateTransform(bool producedAtHelipad)
        {
            if (producedAtHelipad) return GetBoneTransform($"HELIPARK01");

            var freeSlot = NextFreeSlot();
            var runway = SlotToRunway(freeSlot);
            var hangar = SlotToHangar(freeSlot);
            return GetBoneTransform($"RUNWAY{runway}PARK{hangar}HAN");
        }

        public Transform GetUnitCreateTransform(GameObject gameObject)
        {
            var slot = GetCorrespondingSlot(gameObject);
            var runway = SlotToRunway(slot);
            var hangar = SlotToHangar(slot);
            return GetBoneTransform($"RUNWAY{runway}PARK{hangar}HAN");
        }

        public void AddVehicle(GameObject vehicle)
        {
            var freeSlot = NextFreeSlot();
            ParkingSlots[freeSlot] = vehicle;

            vehicle.AIUpdate.SetLocomotor(LocomotorSetType.Taxiing);
            var jetAIUpdate = vehicle.AIUpdate as JetAIUpdate;
            jetAIUpdate.Base = _gameObject;
            jetAIUpdate.CurrentJetAIState = JetAIUpdate.JetAIState.Parked;
        }

        public void ParkVehicle(GameObject vehicle)
        {
            var jetAIUpdate = vehicle.AIUpdate as JetAIUpdate;
            jetAIUpdate.AddTargetPoint(_gameObject.ToWorldspace(GetParkingPoint(vehicle)));
            jetAIUpdate.CurrentJetAIState = JetAIUpdate.JetAIState.MovingOut;
        }

        public Vector3? GetNaturalRallyPoint() => null;

        public Transform GetParkingTransform(GameObject gameObject)
        {
            var slot = GetCorrespondingSlot(gameObject);
            var runway = SlotToRunway(slot);
            var hangar = SlotToHangar(slot);

            return GetBoneTransform($"RUNWAY{runway}PARKING{hangar}");
        }

        public Vector3 GetParkingPoint(GameObject gameObject) => GetParkingTransform(gameObject).Translation;

        public bool IsPointBlocked(string boneName) => _blockedBones.ContainsKey(boneName) && _blockedBones[boneName];
        public void SetPointBlocked(string boneName, bool value) => _blockedBones[boneName] = value;

        public Queue<string> GetPathToStart(GameObject vehicle)
        {
            var result = new Queue<string>();
            var slot = GetCorrespondingSlot(vehicle);
            var runway = SlotToRunway(slot);
            var hangar = SlotToHangar(slot);

            var parkingPoint = $"RUNWAY{runway}PARKING{hangar}";

            switch (parkingPoint)
            {
                case "RUNWAY1PARKING1":
                    result.Enqueue($"RUNWAY2PREP2");
                    result.Enqueue($"RUNWAY1PREP1");
                    result.Enqueue($"RUNWAYSTART1");
                    break;
                case "RUNWAY2PARKING1":
                    result.Enqueue($"RUNWAY2PREP1");
                    result.Enqueue($"RUNWAYSTART2");
                    break;
                case "RUNWAY1PARKING2":
                    result.Enqueue($"RUNWAY1PREP2");
                    result.Enqueue($"RUNWAY1PREP1");
                    result.Enqueue($"RUNWAYSTART1");
                    break;
                case "RUNWAY2PARKING2":
                    result.Enqueue($"RUNWAY2PREP2");
                    result.Enqueue($"RUNWAY2PREP1");
                    result.Enqueue($"RUNWAYSTART2");
                    break;
            }
            return result;
        }

        // TODO: merge this with GetPathToStart?
        public Queue<string> GetPathToParking(GameObject vehicle)
        {
            var result = new Queue<string>();
            var slot = GetCorrespondingSlot(vehicle);
            var runway = SlotToRunway(slot);
            var hangar = SlotToHangar(slot);

            var parkingPoint = $"RUNWAY{runway}PARKING{hangar}";

            switch (parkingPoint)
            {
                case "RUNWAY1PARKING1":
                    result.Enqueue($"RUNWAYSTART1");
                    result.Enqueue($"RUNWAY1PREP1");
                    result.Enqueue($"RUNWAY2PREP2");
                    break;
                case "RUNWAY2PARKING1":
                    result.Enqueue($"RUNWAYSTART2");
                    result.Enqueue($"RUNWAY2PREP1");
                    break;
                case "RUNWAY1PARKING2":
                    result.Enqueue($"RUNWAYSTART1");
                    result.Enqueue($"RUNWAY1PREP1");
                    result.Enqueue($"RUNWAY1PREP2");
                    break;
                case "RUNWAY2PARKING2":
                    result.Enqueue($"RUNWAYSTART2");
                    result.Enqueue($"RUNWAY2PREP1");
                    result.Enqueue($"RUNWAY2PREP2");
                    break;
            }

            result.Enqueue(parkingPoint);
            return result;
        }

        public Vector3 GetRunwayEndPoint(GameObject vehicle)
        {
            var slot = GetCorrespondingSlot(vehicle);
            var runway = SlotToRunway(slot);

            return GetBoneTranslation($"RUNWAYEND{runway}");
        }

        public Transform GetBoneTransform(string name)
        {
            var (_, bone) = _gameObject.FindBone(name);
            if (bone == null)
            {
                throw new InvalidOperationException("Could not find runway start point bone");
            }
            return bone.Transform;
        }

        public Vector3 GetBoneTranslation(string name) => GetBoneTransform(name).Translation;

        private int SlotToHangar(int slot) => slot / 2 + 1;
        private int SlotToRunway(int slot) => slot % 2 + 1;
    }

    /// <summary>
    /// Used by FS_AIRFIELD KindOfs. If <see cref="HasRunways"/> is set then the model requires 
    /// RunwayStartN, RunwayEndN, RunwayNPrepN, RunwayNParkingN and RunwayNParkNHan bones where N 
    /// corresponds to rows and columns. Module will only use the HeliPark01 bone for helicopters.
    /// </summary>
    public sealed class ParkingPlaceBehaviorModuleData : BehaviorModuleData
    {
        internal static ParkingPlaceBehaviorModuleData Parse(IniParser parser) => parser.ParseBlock(FieldParseTable);

        private static readonly IniParseTable<ParkingPlaceBehaviorModuleData> FieldParseTable = new IniParseTable<ParkingPlaceBehaviorModuleData>
        {
            { "HealAmountPerSecond", (parser, x) => x.HealAmountPerSecond = parser.ParseInteger() },
            { "NumRows", (parser, x) => x.NumRows = parser.ParseInteger() },
            { "NumCols", (parser, x) => x.NumCols = parser.ParseInteger() },
            { "HasRunways", (parser, x) => x.HasRunways = parser.ParseBoolean() },
            { "ApproachHeight", (parser, x) => x.ApproachHeight = parser.ParseInteger() },
            { "ParkInHangars", (parser, x) => x.ParkInHangars = parser.ParseBoolean() },
        };

        public int HealAmountPerSecond { get; private set; }
        public int NumRows { get; private set; }
        public int NumCols { get; private set; }
        public bool HasRunways { get; private set; }
        public int ApproachHeight { get; private set; }
        public bool ParkInHangars { get; private set; }

        internal override BehaviorModule CreateModule(GameObject gameObject, GameContext context)
        {
            return new ParkingPlaceBehaviour(this, gameObject, context);
        }
    }
}
