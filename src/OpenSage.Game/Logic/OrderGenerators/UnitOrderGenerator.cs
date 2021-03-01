using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.DataStructures;
using OpenSage.Graphics.Cameras;
using OpenSage.Graphics.Rendering;
using OpenSage.Input;
using OpenSage.Logic.Object;
using OpenSage.Logic.Orders;
using OpenSage.Mathematics;
using OpenSage.Mathematics.FixedMath;

namespace OpenSage.Logic.OrderGenerators
{
    internal sealed class UnitOrderGenerator : IOrderGenerator
    {
        private readonly Game _game;

        private Vector3 _worldPosition;
        private GameObject _worldObject;

        private IReadOnlyCollection<GameObject> SelectedUnits => _game.Scene3D.LocalPlayer.SelectedUnits;
        private Player LocalPlayer => _game.Scene3D.LocalPlayer;

        public bool CanDrag { get; } = false;

        public UnitOrderGenerator(Game game)
        {
            _game = game;
        }

        public void BuildRenderList(RenderList renderList, Camera camera, in TimeInterval gameTime) { }

        private static string GetCursorForForceFire(IReadOnlyCollection<GameObject> selectedUnits, GameObject target)
        {
            if (selectedUnits.All(u => u.Definition.KindOf.Get(ObjectKinds.Structure) && u.CanAttack))
            {
                // TODO: check range of target, or range to ground position if target is null
                const bool targetIsInRange = true;
                return targetIsInRange ? "AttackObj" : "GenericInvalid";
            }

            return selectedUnits.Any(u => u.CanAttackObject(target)) ? "AttackObj" : "GenericInvalid";
        }

        private static string GetCursorForWaypointModifier()
        {
            // pretty much just always return waypoint as long as we own the unit selected
            return "Waypoint";
        }

        public string GetCursor(KeyModifiers keyModifiers)
        {
            if (SelectedUnits.Count == 0 ||
               SelectedUnits.All(u => u.Owner != LocalPlayer))
            {
                return _worldObject != null
                    ? "Select" // TODO: Maybe shouldn't have this here.
                    : "Arrow";
            }

            // the local player has selected unit(s), and they are the owner of those unit(s)

            if (keyModifiers.HasFlag(KeyModifiers.Alt))
            {
                return GetCursorForWaypointModifier();
            }

            if (keyModifiers.HasFlag(KeyModifiers.Ctrl))
            {
                return GetCursorForForceFire(SelectedUnits, _worldObject);
            }

            // no modifier key has been applied

            // TODO: verify a garrisoned civilian structure is in fact player-owned
            if (SelectedUnits.All(u => u.Definition.KindOf.Get(ObjectKinds.Structure)))
            {
                // the selected object is a player-owned structure
                if (_worldObject != null)
                {
                    if (LocalPlayer.Enemies.Contains(_worldObject.Owner))
                    {
                        // TODO: determine if target is in range
                        const bool targetIsInRange = true;
                        // TODO: what about a garrisoned palace?
                        if (targetIsInRange && SelectedUnits.Any(u => u.CanAttackObject(_worldObject)))
                        {
                            return "AttackObj";
                        }

                        return "GenericInvalid";
                    }

                    return "Select";
                }

                return "GenericInvalid";
            }

            // the selected object is a player-owned unit (not structure)

            // TODO: check if terrain is under fog of war.
            const bool terrainIsRevealed = true;
            if (!terrainIsRevealed)
            {
                // In Zero Hour, if terrain is under fog of war we always return the Move cursor, no matter what
                // arguably we could improve upon this by looking at the terrain under the cursor, and ignoring any world objects
                return "Move";
            }

            if (_worldObject is null)
            {
                // TODO: should this be floor or ceiling?
                var terrainImpassable = _game.Scene3D.Terrain.Map.BlendTileData.Impassability[(int) _worldPosition.X, (int) _worldPosition.Y];
                if (!terrainImpassable)
                {
                    return "Move";
                }

                if (SelectedUnits.All(u => !u.IsAirborne() &&
                                           !u.Definition.LocomotorSets.Select(l => l.Locomotor.Value)
                                               .Any(l => l.Surfaces.Get(Surface.Cliff))))
                {
                    return "GenericInvalid";
                }

                return "Move";
            }

            // target is some game object

            if (LocalPlayer.Enemies.Contains(_worldObject.Owner))
            {
                // target is enemy
                // TODO: black lotus hack? check if behaviors contains lotus hack?
                if (SelectedUnits.Any(u => u.CanAttackObject(_worldObject)))
                {
                    return "AttackObj";
                }

                return "GenericInvalid";
            }

            // target is not enemy

            // TODO: check if capture ability is ready
            const bool captureIsReady = true;
            // TODO: There's probably a much easier way to figure out if a structure is neutral, but c'est la vie
            if (_worldObject.Owner != LocalPlayer &&
                !LocalPlayer.Allies.Contains(_worldObject.Owner) &&
                !LocalPlayer.Enemies.Contains(_worldObject.Owner) &&
                _worldObject.Definition.KindOf.Get(ObjectKinds.Capturable) &&
                SelectedUnits.Any(u => u.Definition.Behaviors.Values.Any(b =>
                    b is SpecialAbilityModuleData s && s.SpecialPowerTemplate.EndsWith("CaptureBuilding"))) && captureIsReady)
            {

                return "CaptureBuilding";
            }

            if (_worldObject.Definition.Behaviors.Values.FirstOrDefault(b =>
                b is GarrisonContainModuleData || b is TransportContainModuleData) is OpenContainModuleData g)
            {
                // TODO: prevent garrison if object is full
                const bool garrisonIsFull = false;
                if (!garrisonIsFull && !_worldObject.Destroyed &&
                    SelectedUnits.Select(u => u.Definition.KindOf).Any(k => g.AllowInsideKindOf?.Intersects(k) ?? true))
                {
                    return "EnterFriendly";
                }

                return "Select";
            }

            if (_worldObject.Definition.KindOf.Get(ObjectKinds.Structure))
            {
                // TODO: if target can heal unit and isn't full (e.g. barracks, airfield, war factory) show **EnterFriendly**
                return "Select";
            }

            if (SelectedUnits.Any(u => u.Definition.KindOf.Get(ObjectKinds.Dozer)))
            {
                if (_worldObject.Definition.KindOf.Get(ObjectKinds.Structure))
                {
                    if (_worldObject.BuildProgress < 1f && !_worldObject.IsBeingConstructed())
                    {
                        return "ResumeConstruction";
                    }

                    if (_worldObject.HealthPercentage < Fix64.One)
                    {
                        return "GetRepaired";
                    }
                }
            }

            return "Select";
        }

        public OrderGeneratorResult TryActivate(Scene3D scene, KeyModifiers keyModifiers)
        {
            if (scene.LocalPlayer.SelectedUnits.Count == 0)
            {
                return OrderGeneratorResult.Inapplicable();
            }

            Order order;

            // We choose the sound based on the most-recently-selected unit.
            var unit = scene.LocalPlayer.SelectedUnits.Last();

            // TODO: Use ini files for this, don't hardcode it.
            if (keyModifiers.HasFlag(KeyModifiers.Ctrl))
            {
                // TODO: Check whether clicked point is an object, or empty ground.
                // TODO: handle hordes properly
                unit.OnLocalAttack(_game.Audio);
                if (_worldObject != null)
                {
                    var objectId = scene.GameObjects.GetObjectId(_worldObject);

                    order = Order.CreateAttackObject(scene.GetPlayerIndex(scene.LocalPlayer), (uint) objectId, true);
                }
                else
                {
                    order = Order.CreateAttackGround(scene.GetPlayerIndex(scene.LocalPlayer), _worldPosition);
                }
            }
            else
            {
                if (_worldObject != null)
                {
                    var objectId = scene.GameObjects.GetObjectId(_worldObject);

                    // TODO: Should take allies into account.
                    if (_worldObject.Owner != _game.Scene3D.LocalPlayer)
                    {
                        // TODO: handle hordes properly
                        unit.OnLocalAttack(_game.Audio);
                        order = Order.CreateAttackObject(scene.GetPlayerIndex(scene.LocalPlayer), (uint) objectId, false);
                    }
                    else if (_worldObject.Definition.KindOf.Get(ObjectKinds.Transport))
                    {
                        // SoundEnter
                        // VoiceEnter
                        // TODO: Also need to check TransportSlotCount, Slots, etc.
                        order = Order.CreateEnter(scene.GetPlayerIndex(scene.LocalPlayer), objectId);
                    }
                    else
                    {
                        return OrderGeneratorResult.Inapplicable();
                    }
                }
                else
                {
                    // TODO: Check whether at least one of the selected units can actually be moved.
                    // TODO: handle hordes properly
                    unit.OnLocalMove(_game.Audio);
                    order = Order.CreateMoveOrder(scene.GetPlayerIndex(scene.LocalPlayer), _worldPosition);
                }
            }

            return OrderGeneratorResult.SuccessAndContinue(new[] { order });
        }

        public void UpdateDrag(Vector3 position)
        {
            
        }

        public void UpdatePosition(Vector2 mousePosition, Vector3 worldPosition)
        {
            _worldPosition = worldPosition;
            _worldObject = _game.Selection.FindClosestObject(mousePosition);
        }
    }
}
