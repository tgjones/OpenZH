using System;
using System.IO;
using System.Numerics;
using OpenSage.Data.Sav;
using OpenSage.Mathematics;
using OpenSage.Terrain;

namespace OpenSage.Logic.Object
{
    /// <summary>
    /// Locomotors control how an object moves towards a target position.
    /// (Locomotors are not the only way an object can move; some behaviors
    /// can do this too, without using a locomotor, for example DumbProjectileBehavior.)
    ///
    /// Locomotor "appearance" controls many aspects of their movement:
    ///
    /// - TREADS
    ///   This is supposed to move like a tank with treads. The object turns while stationary,
    ///   and moves only in a straight line.
    ///
    /// - WINGS
    ///
    /// - HOVER
    ///
    /// - THRUST
    ///
    /// - TWO_LEGS
    ///
    /// - FOUR_WHEELS
    ///
    /// Other Locomotor properties allow more fine-grained control:
    ///
    /// - ForwardAccelerationPitchFactor and LateralAccelerationRollFactor
    ///   These control how much acceleration and cornering, respectively, will cause
    ///   the chassis to pitch or roll.
    /// </summary>
    public sealed class Locomotor
    {
        private readonly GameObject _gameObject;
        private readonly float _baseSpeed;
        public readonly LocomotorTemplate LocomotorTemplate;

        public float LiftFactor;

        public Locomotor(GameObject gameObject, LocomotorTemplate locomotorTemplate, float baseSpeed)
        {
            _gameObject = gameObject;
            LocomotorTemplate = locomotorTemplate;
            _baseSpeed = baseSpeed;
            LiftFactor = 1.0f;
        }

        //TODO: check if the damaged values exists
        public float GetAcceleration()
        {
            return _gameObject.IsDamaged
                ? GetScaledLocomotorValue(x => x.AccelerationDamaged)
                : GetScaledLocomotorValue(x => x.Acceleration);
        }

        private float GetFrontWheelTurnAngle()
        {
            return GetScaledLocomotorValue(x => x.FrontWheelTurnAngle);
        }

        private float GetTurnRate()
        {
            return _gameObject.IsDamaged
                ? GetScaledLocomotorValue(x => x.TurnRateDamaged)
                : GetScaledLocomotorValue(x => x.TurnRate);
        }

        public float GetSpeed()
        {
            if (LocomotorTemplate.Speed.HasValue)
            {
                return _gameObject.IsDamaged
                    ? GetScaledLocomotorValue(x => x.SpeedDamaged)
                    : GetScaledLocomotorValue(x => x.Speed.Value);
            }
            // TODO: How do we get the damaged speed for BFME?
            return _baseSpeed;
        }

        public float GetLift()
        {
            var currentLift = _gameObject.IsDamaged
                ? GetScaledLocomotorValue(x => x.LiftDamaged)
                : GetScaledLocomotorValue(x => x.Lift);
            return currentLift * LiftFactor;
        }

        public bool RotateToTargetDirection(in TimeInterval gameTime, in Vector3 targetDirection)
        {
            var deltaTime = (float) gameTime.DeltaTime.TotalSeconds;

            var currentYaw = _gameObject.Yaw;

            var targetYaw = MathUtility.GetYawFromDirection(new Vector2(targetDirection.X, targetDirection.Y));
            var angleDelta = MathUtility.CalculateAngleDelta(targetYaw, currentYaw);

            if (MathF.Abs(angleDelta) < 0.1f)
            {
                return true;
            }

            var d = MathUtility.ToRadians(GetTurnRate()) * deltaTime;
            var newDelta = -MathF.Sign(angleDelta) * MathF.Min(MathF.Abs(angleDelta), MathF.Abs(d));
            var yaw = currentYaw + newDelta;

            _gameObject.SetRotation(Quaternion.CreateFromYawPitchRoll(0.0f, 0.0f, yaw));
            return false;
        }

        public bool MoveTowardsPosition(in TimeInterval gameTime, in Vector3 targetPoint, HeightMap heightMap, in Vector3? nextPoint)
        {
            var deltaTime = (float) gameTime.DeltaTime.TotalSeconds;

            var translation = _gameObject.Translation;
            var x = translation.X;
            var y = translation.Y;
            var z = translation.Z;

            var oldSpeed = _gameObject.Speed;

            var delta = targetPoint - translation;

            // Distance is 2D
            var distanceRemaining = delta.Vector2XY().Length();
            var braking = GetScaledLocomotorValue(_ => _.Braking);

            bool canExecuteSlide = false;
            switch (LocomotorTemplate.Appearance)
            {
                case LocomotorAppearance.Treads:
                    break;
                case LocomotorAppearance.Wings:
                    braking = 0; // TODO: aircrafts should only brake while landing (do they?)
                    break;
                default:
                    if (nextPoint != null)
                    {
                        braking = 0;
                    }

                    var circumference = 360.0f / GetTurnRate() * oldSpeed;
                    var radius = circumference / MathUtility.TwoPi;

                    if (distanceRemaining < (radius + 0.25f))
                    {
                        if (nextPoint != null)
                        {
                            // turn towards next point
                            return true;
                        }
                        else
                        {
                            canExecuteSlide = true;
                        }
                    }
                    break;
            }

            if (distanceRemaining < 0.25f)
            {
                return true;
            }

            var minimumBrakingDistance = braking > 0
                ? (oldSpeed * oldSpeed) / (braking * 2.0f) // s = v² / 2a
                : 0;

            // Are we braking or accelerating?
            var accelerating = distanceRemaining > minimumBrakingDistance;
            var currentAcceleration = accelerating
                ? GetAcceleration()
                : -braking;

            var deltaSpeed = currentAcceleration * deltaTime;

            var newSpeed = oldSpeed + deltaSpeed;
            var reachedTurnSpeed = newSpeed >= GetScaledLocomotorValue(_ => _.MinTurnSpeed);
            _gameObject.Speed = Math.Clamp(newSpeed, 0, GetSpeed());

            // This locomotor speed is distance/second
            var distance = MathF.Min(_gameObject.Speed * deltaTime, distanceRemaining);

            // The distance we're moving
            var direction = Vector2.Normalize(delta.Vector2XY());
            var moveDirection = direction;

            // Calculate rotation
            var currentYaw = _gameObject.Yaw;

            var targetYaw = MathUtility.GetYawFromDirection(direction);
            var angleDelta = MathUtility.CalculateAngleDelta(targetYaw, currentYaw);

            var d = MathUtility.ToRadians(GetTurnRate()) * deltaTime;
            var turningDelta = MathF.Min(MathF.Abs(angleDelta), MathF.Abs(d));
            var newDelta = -MathF.Sign(angleDelta) * turningDelta;

            var yaw = reachedTurnSpeed ? currentYaw + newDelta : currentYaw;

            _gameObject.SteeringWheelsYaw = Math.Clamp(-angleDelta, MathUtility.ToRadians(-GetFrontWheelTurnAngle()), MathUtility.ToRadians(GetFrontWheelTurnAngle()));

            // model roll and pitch
            var modelPitch = 0.0f;
            var modelRoll = 0.0f;

            if (canExecuteSlide && LocomotorTemplate.Appearance == LocomotorAppearance.Hover)
            {
                translation += new Vector3(Vector2.Normalize(delta.Vector2XY()) * distance, 0);
                _gameObject.SetTranslation(translation);

                modelPitch = -distance * LocomotorTemplate.ForwardVelocityPitchFactor;
                modelRoll = angleDelta * distance * LocomotorTemplate.LateralVelocityRollFactor;

                _gameObject.ModelTransform.Rotation = Quaternion.CreateFromYawPitchRoll(modelPitch, modelRoll, 0);
                _gameObject.SetRotation(Quaternion.CreateFromYawPitchRoll(0.0f, 0.0f, yaw));

                return false;
            }

            var thrust = 0.0f;
            var deltaZ = 0.0f;

            // height
            var height = heightMap.GetHeight(x, y);
            switch (LocomotorTemplate.Appearance)
            {
                case LocomotorAppearance.Thrust:
                    var targetZ = targetPoint.Z;
                    if (nextPoint != null)
                    {
                        targetZ = height + LocomotorTemplate.PreferredHeight;
                    }

                    deltaZ = (distance / distanceRemaining) * (targetZ - translation.Z);
                    translation.Z += deltaZ;
                    break;
                case LocomotorAppearance.GiantBird:
                case LocomotorAppearance.Wings:
                case LocomotorAppearance.Hover:
                    thrust = GetCurrentThrust(height, deltaTime, translation.Z);
                    if (!reachedTurnSpeed)
                    {
                        break;
                    }

                    translation.Z += thrust;
                    break;
                case LocomotorAppearance.Treads:
                    translation.Z = height;
                    if (MathF.Abs(targetYaw - yaw) > MathUtility.ToRadians(2.0f)) //first fully rotate towards target point
                    {
                        distance = 0.0f;
                    }
                    break;
                default:
                    translation.Z = height;
                    break;
            }

            // moving direction
            var lookingDirection = _gameObject.LookDirection;
            switch (LocomotorTemplate.Appearance)
            {
                case LocomotorAppearance.Thrust:
                case LocomotorAppearance.Treads:
                    break;
                default:
                    moveDirection = Vector2.Normalize(new Vector2(lookingDirection.X, lookingDirection.Y));
                    break;
            }

            switch (LocomotorTemplate.Appearance)
            {
                case LocomotorAppearance.Thrust:
                    modelPitch = deltaZ;
                    break;
                case LocomotorAppearance.Wings:
                    if (!reachedTurnSpeed)
                    {
                        break;
                    }

                    modelPitch = -thrust / distance + distance * LocomotorTemplate.ForwardVelocityPitchFactor;
                    var angle = Math.Clamp(angleDelta, -MathUtility.PiOver4, MathUtility.PiOver4);
                    modelRoll = angle * distance * LocomotorTemplate.LateralVelocityRollFactor;
                    break;
                case LocomotorAppearance.Hover:
                    modelPitch = -distance * LocomotorTemplate.ForwardVelocityPitchFactor;
                    modelRoll = angleDelta * distance * LocomotorTemplate.LateralVelocityRollFactor;
                    break;
            }

            // roll and pitch according to terrain
            var worldPitch = 0.0f;
            var worldRoll = 0.0f;
            switch (LocomotorTemplate.Appearance)
            {
                case LocomotorAppearance.Treads:
                case LocomotorAppearance.FourWheels:
                    var normal = heightMap.GetNormal(x, y);

                    var deltaX = normal.X;
                    var deltaY = normal.Y;

                    worldPitch = (float) Math.Asin(deltaX);
                    worldRoll = (float) Math.Asin(deltaY);
                    break;
            }

            translation += new Vector3(moveDirection * distance, 0);
            _gameObject.SetTranslation(translation);

            _gameObject.ModelTransform.Rotation = Quaternion.CreateFromYawPitchRoll(modelPitch, modelRoll, 0);
            _gameObject.SetRotation(Quaternion.CreateFromYawPitchRoll(worldPitch, worldRoll, yaw));

            return false;
        }

        public void MaintainPosition(in TimeInterval gameTime, HeightMap heightMap)
        {
            switch (LocomotorTemplate.Appearance)
            {
                case LocomotorAppearance.Wings:
                    var deltaTime = (float) gameTime.DeltaTime.TotalSeconds;

                    var translation = _gameObject.Translation;

                    _gameObject.Speed = GetSpeed();
                    var circumference = MathUtility.TwoPi * GetScaledLocomotorValue(x => x.CirclingRadius);
                    var timePerRoundtrip = circumference / _gameObject.Speed;

                    var moveDirection = Vector2.Normalize(new Vector2(_gameObject.LookDirection.X, _gameObject.LookDirection.Y));

                    var deltaTransform = new Vector3(moveDirection * _gameObject.Speed * deltaTime, 0);

                    translation += deltaTransform;

                    var height = heightMap.GetHeight(translation.X, translation.Y);
                    translation.Z += GetCurrentThrust(height, deltaTime, translation.Z);
                    _gameObject.SetTranslation(translation);

                    var normal = heightMap.GetNormal(translation.X, translation.Y);

                    // TODO: in order to align to the terrain, but this messes up our lookingDirection -> we move randomly instead of in a clear circle
                    var worldPitch = 0; //-(float) Math.Asin(normal.X);
                    var worldRoll = 0; //-(float) Math.Asin(normal.Y);

                    var deltaYaw = (deltaTime / timePerRoundtrip) * MathUtility.TwoPi;
                    var worldYaw = _gameObject.Yaw + deltaYaw;
                    var modelRoll = -deltaYaw * deltaTransform.Length();
                    _gameObject.ModelTransform.Rotation = Quaternion.CreateFromYawPitchRoll(0, modelRoll, 0);
                    _gameObject.SetRotation(Quaternion.CreateFromYawPitchRoll(worldPitch, worldRoll, worldYaw));
                    break;
            }
        }

        private float GetCurrentThrust(float terrainHeight, float deltaTime, float height)
        {
            var heightRemaining = (terrainHeight + LocomotorTemplate.PreferredHeight) - height;
            var lift = GetLift();
            _gameObject.Lift += lift;
            _gameObject.Lift = Math.Clamp(_gameObject.Lift, 0, lift);
            return MathF.Min(_gameObject.Lift * deltaTime, heightRemaining);
        }

        public float GetScaledLocomotorValue(Func<LocomotorTemplate, float> getValue)
        {
            return (_baseSpeed / 100.0f) * getValue(LocomotorTemplate);
        }

        public float GetLocomotorValue(Func<LocomotorTemplate, float> getValue) => getValue(LocomotorTemplate);

        internal void Load(SaveFileReader reader)
        {
            reader.ReadVersion(2);

            var frameSomething = reader.ReadUInt32();

            var positionSomething = reader.ReadVector3();

            var unknownFloat1 = reader.ReadSingle();
            if (unknownFloat1 != 1.0f)
            {
                //throw new InvalidDataException();
            }

            var unknownFloat2 = reader.ReadSingle();
            if (unknownFloat2 != 99999.0f)
            {
                throw new InvalidDataException();
            }

            var unknownFloat3 = reader.ReadSingle();
            if (unknownFloat3 != 99999.0f)
            {
                throw new InvalidDataException();
            }

            var unknownFloat4 = reader.ReadSingle();
            if (unknownFloat4 != 99999.0f)
            {
                throw new InvalidDataException();
            }

            var unknownFloat5 = reader.ReadSingle();
            if (unknownFloat5 != 99999.0f)
            {
                throw new InvalidDataException();
            }

            var unknownFloat6 = reader.ReadSingle();
            if (unknownFloat6 != 99999.0f)
            {
                throw new InvalidDataException();
            }

            var unknownFloat7 = reader.ReadSingle();
            if (unknownFloat7 != 1.0f)
            {
                throw new InvalidDataException();
            }

            var unknown2 = reader.ReadUInt32();
            if (unknown2 != 0) // 0, 4
            {
                //throw new InvalidDataException();
            }

            var unknown3 = reader.ReadSingle(); // 0, 100

            var unknownFloat8 = reader.ReadSingle();
            if (unknownFloat8 != 1.0f)
            {
                throw new InvalidDataException();
            }

            var unknownFloat9 = reader.ReadSingle(); // 0.4849...

            var unknownFloat10 = reader.ReadSingle(); // 0.0892...
        }
    }
}
