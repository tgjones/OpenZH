using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using OpenSage.Audio;
using OpenSage.Client;
using OpenSage.Content;
using OpenSage.Data.Ini;
using OpenSage.Data.Map;
using OpenSage.Data.Sav;
using OpenSage.DataStructures;
using OpenSage.Diagnostics;
using OpenSage.Graphics.Cameras;
using OpenSage.Graphics.Rendering;
using OpenSage.Graphics.Shaders;
using OpenSage.Gui.ControlBar;
using OpenSage.Gui.InGame;
using OpenSage.Logic.Object.Helpers;
using OpenSage.Mathematics;
using FixedMath.NET;
using OpenSage.Terrain;

namespace OpenSage.Logic.Object
{
    [DebuggerDisplay("[Object:{Definition.Name} ({Owner})]")]
    public sealed class GameObject : Entity, IInspectable, ICollidable
    {
        internal static GameObject FromMapObject(
            MapObject mapObject,
            AssetStore assetStore,
            GameObjectCollection gameObjects,
            HeightMap heightMap,
            bool useRotationAnchorOffset = true,
            in float? overwriteAngle = 0.0f,
            TeamFactory teamFactory = null)
        {
            var gameObject = gameObjects.Add(mapObject.TypeName);

            // TODO: Is there any valid case where we'd want to return null instead of throwing an exception?
            if (gameObject == null)
            {
                return null;
            }

            if (gameObject._body != null)
            {
                var healthMultiplier = mapObject.Properties.TryGetValue("objectInitialHealth", out var health)
                    ? (uint) health.Value / 100.0f
                    : 1.0f;

                gameObject._body.SetInitialHealth(healthMultiplier);
            }

            if (mapObject.Properties.TryGetValue("objectName", out var objectName))
            {
                gameObject.Name = (string) objectName.Value;
            }

            if (teamFactory != null)
            {
                if (mapObject.Properties.TryGetValue("originalOwner", out var teamName))
                {
                    var name = (string) teamName.Value;
                    if (name.Contains('/'))
                    {
                        name = name.Split('/')[1];
                    }
                    var team = teamFactory.FindTeamTemplateByName(name);
                    gameObject.Team = team;
                    gameObject.Owner = team?.Owner;
                }
            }

            if (mapObject.Properties.TryGetValue("objectSelectable", out var selectable))
            {
                gameObject.IsSelectable = (bool) selectable.Value;
            }

            // TODO: handle "align to terrain" property
            var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, overwriteAngle ?? mapObject.Angle);
            var rotationOffset = useRotationAnchorOffset
                ? Vector4.Transform(new Vector4(gameObject.Definition.RotationAnchorOffset.X, gameObject.Definition.RotationAnchorOffset.Y, 0.0f, 1.0f), rotation)
                : Vector4.UnitW;
            var position = mapObject.Position + rotationOffset.ToVector3();
            var height = heightMap.GetHeight(position.X, position.Y) + mapObject.Position.Z;
            gameObject.UpdateTransform(new Vector3(position.X, position.Y, height), rotation,
                gameObject.Definition.Scale);

            if (gameObject.Definition.IsBridge)
            {
                BridgeTowers.CreateForLandmarkBridge(assetStore, gameObjects, gameObject, mapObject);
            }

            if (gameObject.Definition.KindOf.Get(ObjectKinds.Horde))
            {
                gameObject.FindBehavior<HordeContainBehavior>().Unpack();
            }

            return gameObject;
        }

        private readonly Dictionary<string, AttributeModifier> _attributeModifiers;

        public uint ID { get; private set; }

        public Percentage ProductionModifier { get; set; } = new Percentage(1);
        public Fix64 HealthModifier { get; set; }


        private readonly GameContext _gameContext;

        internal GameContext GameContext => _gameContext;

        private readonly BehaviorUpdateContext _behaviorUpdateContext;

        private readonly GameObject _rallyPointMarker;

        private BodyDamageType _bodyDamageType = BodyDamageType.Pristine;

        internal readonly BitArray<WeaponSetConditions> WeaponSetConditions;
        private readonly WeaponSet _weaponSet;

        public readonly ObjectDefinition Definition;

        private readonly Transform _transform;
        public readonly Transform ModelTransform;

        private bool _objectMoved;

        public Transform Transform => _transform;
        public float Yaw => _transform.Yaw;
        public Vector3 EulerAngles => _transform.EulerAngles;
        public Vector3 LookDirection => _transform.LookDirection;
        public Vector3 Translation => _transform.Translation;
        public Quaternion Rotation => _transform.Rotation;
        public Matrix4x4 TransformMatrix => _transform.Matrix;

        public void SetTransformMatrix(Matrix4x4 matrix)
        {
            _transform.Matrix = matrix;
            _objectMoved = true;
        }

        public void SetTranslation(Vector3 translation)
        {
            _transform.Translation = translation;
            _objectMoved = true;
        }

        public void SetRotation(Quaternion rotation)
        {
            _transform.Rotation = rotation;
            _objectMoved = true;
        }

        public void SetScale(float scale)
        {
            _transform.Scale = scale;
            _objectMoved = true;
        }

        public void UpdateTransform(in Vector3? translation = null, in Quaternion? rotation = null, float scale = 1.0f)
        {
            _transform.Translation = translation ?? _transform.Translation;
            _transform.Rotation = rotation ?? _transform.Rotation;
            _transform.Scale = scale;
            _objectMoved = true;
        }

        // Doing this with a field and a property instead of an auto-property allows us to have a read-only public interface,
        // while simultaneously supporting fast (non-allocating) iteration when accessing the list within the class.
        public IReadOnlyList<BehaviorModule> BehaviorModules => _behaviorModules;
        private readonly List<BehaviorModule> _behaviorModules;

        private readonly BodyModule _body;
        public bool HasActiveBody() => _body is ActiveBody;

        public Fix64 Health
        {
            get => _body?.Health ?? Fix64.Zero;
            set => _body.Health = value;
        }

        public Fix64 MaxHealth
        {
            get => _body?.MaxHealth + HealthModifier ?? Fix64.Zero;
            set => _body.MaxHealth = value;
        }
        public Fix64 HealthPercentage => MaxHealth != Fix64.Zero ? Health / MaxHealth : Fix64.Zero;

        public void DoDamage(DamageType damageType, Fix64 amount, DeathType deathType, TimeInterval time)
        {
            _body.DoDamage(damageType, amount, deathType, time);
        }

        public Collider RoughCollider { get; set; }
        public List<Collider> Colliders { get; }

        public float VerticalOffset;

        public Player Owner { get; internal set; }

        public GameObject ParentHorde { get; set; }

        public int Supply { get; set; }

        public bool IsStructure { get; private set; }

        public BitArray<ModelConditionFlag> ModelConditionFlags => Drawable.ModelConditionFlags;

        public RadiusDecalTemplate SelectionDecal;

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != null)
                {
                    throw new InvalidOperationException("An object's name cannot change once it's been set.");
                }

                _name = value ?? throw new ArgumentNullException(nameof(value));
                Parent.AddNameLookup(this);
            }
        }

        string IInspectable.Name => "GameObject";

        public TeamTemplate Team { get; set; }

        public bool IsSelectable { get; set; }
        public bool IsProjectile { get; private set; } = false;
        public bool CanAttack { get; private set; }

        public bool IsSelected { get; set; }

        public Vector3? RallyPoint { get; set; }

        internal Weapon CurrentWeapon => _weaponSet.CurrentWeapon;

        private TimeSpan ConstructionStart { get; set; }

        public TimeSpan? LifeTime { get; set; }

        public float BuildProgress { get; set; }

        public bool Destroyed { get; set; }

        public bool Hidden { get; set; }

        public bool IsDamaged
        {
            get
            {
                var healthPercentage = (float) _body.HealthPercentage;
                var damagedThreshold = _gameContext.AssetLoadContext.AssetStore.GameData.Current.UnitDamagedThreshold;
                return healthPercentage <= damagedThreshold;
            }
        }

        public float Speed { get; set; }
        public float SteeringWheelsYaw { get; set; }
        public float Lift { get; set; }

        public float TurretYaw { get; set; }
        public float TurretPitch { get; set; }

        public bool IsPlacementPreview { get; set; }

        public bool IsPlacementInvalid { get; set; }

        public GameObjectCollection Parent { get; private set; }

        public AIUpdate AIUpdate { get; }
        public ProductionUpdate ProductionUpdate { get; }

        private readonly HashSet<string> _upgrades;

        // We compute this every time it's requested, but we don't want
        // to allocate a new object every time.
        private readonly HashSet<string> _upgradesAll;

        public int Rank { get; set; }
        public int ExperienceValue { get; set; }
        public int ExperienceRequiredForNextLevel { get; set; }
        internal float ExperienceMultiplier { get; set; }

        public int EnergyProduction { get; internal set; }

        // TODO
        public ArmorTemplateSet CurrentArmorSet => Definition.ArmorSets.Values.First();

        public readonly Drawable Drawable;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        internal GameObject(
            ObjectDefinition objectDefinition,
            GameContext gameContext,
            Player owner,
            GameObjectCollection parent)
        {
            if (objectDefinition.BuildVariations != null && objectDefinition.BuildVariations.Count() > 0)
            {
                objectDefinition = objectDefinition.BuildVariations[gameContext.Random.Next(0, objectDefinition.BuildVariations.Count())].Value;
            }

            _upgrades = new HashSet<string>();
            _upgradesAll = new HashSet<string>();

            _objectMoved = true;
            Hidden = false;
            ExperienceMultiplier = 1.0f;
            ExperienceValue = 0;
            Rank = 0;

            Definition = objectDefinition ?? throw new ArgumentNullException(nameof(objectDefinition));

            _attributeModifiers = new Dictionary<string, AttributeModifier>();
            _gameContext = gameContext;
            Owner = owner;
            Parent = parent;

            _behaviorUpdateContext = new BehaviorUpdateContext(gameContext, this, TimeInterval.Zero);

            _weaponSet = new WeaponSet(this);
            WeaponSetConditions = new BitArray<WeaponSetConditions>();
            UpdateWeaponSet();

            _transform = Transform.CreateIdentity();
            ModelTransform = Transform.CreateIdentity();
            _transform.Scale = Definition.Scale;

            Drawable = new Drawable(objectDefinition, gameContext, this);

            var behaviors = new List<BehaviorModule>();

            void AddBehavior(string tag, BehaviorModule behavior)
            {
                behaviors.Add(behavior);
                AddModule(tag, behavior);
            }

            AddBehavior("ModuleTag_SMCHelper", new ObjectSpecialModelConditionHelper());

            if (objectDefinition.KindOf.Get(ObjectKinds.CanBeRepulsed))
            {
                AddBehavior("ModuleTag_RepulsorHelper", new ObjectRepulsorHelper());
            }

            // TODO: This shouldn't be added to all objects. I don't know what the rule is.
            // Maybe KindOf = CAN_ATTACK ?
            AddBehavior("ModuleTag_DefectionHelper", new ObjectDefectionHelper());

            // TODO: This shouldn't be added to all objects. I don't know what the rule is.
            // Probably only those with weapons.
            AddBehavior("ModuleTag_WeaponStatusHelper", new ObjectWeaponStatusHelper());

            // TODO: This shouldn't be added to all objects. I don't know what the rule is.
            // Probably only those with weapons.
            AddBehavior("ModuleTag_FiringTrackerHelper", new ObjectFiringTrackerHelper());

            // TODO: This shouldn't be added to all objects. I don't know what the rule is.
            AddBehavior("ModuleTag_ExperienceHelper", new ExperienceUpdate(this));

            foreach (var behaviorDataContainer in objectDefinition.Behaviors.Values)
            {
                var behaviorModuleData = (BehaviorModuleData) behaviorDataContainer.Data;
                var module = AddDisposable(behaviorModuleData.CreateModule(this, gameContext));

                // TODO: This will never be null once we've implemented all the behaviors.
                if (module != null)
                {
                    if (module is CreateModule createModule)
                    {
                        createModule.Execute(_behaviorUpdateContext);
                    }

                    AddBehavior(behaviorDataContainer.Tag, module);
                }
            }

            _behaviorModules = behaviors;

            ProductionUpdate = FindBehavior<ProductionUpdate>();

            _body = AddDisposable(((BodyModuleData) objectDefinition.Body.Data).CreateBodyModule(this));
            AddModule(objectDefinition.Body.Tag, _body);

            if (objectDefinition.AIUpdate != null)
            {
                AIUpdate = AddDisposable(((AIUpdateModuleData) objectDefinition.AIUpdate.Value.Data).CreateAIUpdate(this));
                AddModule(objectDefinition.AIUpdate.Value.Tag, AIUpdate);
            }

            var allGeometries = new List<Geometry>
            {
                Definition.Geometry
            };
            allGeometries.AddRange(Definition.AdditionalGeometries);
            allGeometries.AddRange(Definition.OtherGeometries);

            Colliders = new List<Collider>();
            foreach (var geometry in allGeometries)
            {
                Colliders.Add(Collider.Create(geometry, _transform));
            }

            RoughCollider = Collider.Create(Colliders);

            IsSelectable = Definition.KindOf.Get(ObjectKinds.Selectable);
            CanAttack = Definition.KindOf.Get(ObjectKinds.CanAttack);

            if (Definition.KindOf.Get(ObjectKinds.AutoRallyPoint))
            {
                var rpMarkerDef = gameContext.AssetLoadContext.AssetStore.ObjectDefinitions.GetByName("RallyPointMarker");
                _rallyPointMarker = AddDisposable(new GameObject(rpMarkerDef, gameContext, owner, parent));
            }

            if (Definition.KindOf.Get(ObjectKinds.Projectile))
            {
                IsProjectile = true;
            }

            if (Definition.KindOf.Get(ObjectKinds.Tree))
            {
                Supply = Definition.SupplyOverride > 0 ? Definition.SupplyOverride : gameContext.AssetLoadContext.AssetStore.GameData.Current.SupplyBoxesPerTree;
            }

            if (Definition.KindOf.Get(ObjectKinds.Structure))
            {
                IsStructure = true;
            }
        }

        public bool AffectsAreaPassability
        {
            get
            {
                if (!IsStructure)
                {
                    return false;
                }
                if (_gameContext.Scene3D.Game.SageGame == SageGame.CncGenerals ||
                    _gameContext.Scene3D.Game.SageGame == SageGame.CncGenerals)
                {
                    // SupplyWarehouse in CncGenerals has NoCollide
                    return true;
                }
                return !Definition.KindOf.Get(ObjectKinds.NoCollide);

            }
        }

        public void AddAttributeModifier(string name, AttributeModifier modifier)
        {
            if (_attributeModifiers.ContainsKey(name))
            {
                return;
            }
            _attributeModifiers.Add(name, modifier);
        }

        public void RemoveAttributeModifier(string name)
        {
            _attributeModifiers[name].Invalid = true;
        }

        


        public void ShowCollider(string name)
        {
            if (Colliders.Any(x => x.Name.Equals(name)))
            {
                return;
            }

            var allGeometries = new List<Geometry>
            {
                Definition.Geometry
            };
            allGeometries.AddRange(Definition.AdditionalGeometries);
            allGeometries.AddRange(Definition.OtherGeometries);

            var newColliders = new List<Collider>();
            foreach (var geometry in allGeometries)
            {
                if (geometry.Name.Equals(name))
                {
                    newColliders.Add(Collider.Create(geometry, Transform));
                }
            }

            if (AffectsAreaPassability)
            {
                foreach (var collider in newColliders)
                {
                    _gameContext.Navigation.UpdateAreaPassability(collider, false);
                }
            }
            Colliders.AddRange(newColliders);
            RoughCollider = Collider.Create(Colliders);
            _gameContext.Quadtree.Update(this);
        }

        public void HideCollider(string name)
        {
            if (!Colliders.Any(x => x.Name.Equals(name)))
            {
                return;
            }

            for (var i = Colliders.Count - 1; i >= 0; i--)
            {
                if (Colliders[i].Name.Equals(name))
                {
                    if (AffectsAreaPassability)
                    {
                        _gameContext.Navigation.UpdateAreaPassability(Colliders[i], true);
                    }
                    Colliders.RemoveAt(i);
                }
            }
            RoughCollider = Collider.Create(Colliders);
            _gameContext.Quadtree.Update(this);

            if (AffectsAreaPassability)
            {
                _gameContext.Navigation.UpdateAreaPassability(this, false);
            }
        }

        public void UpdateColliders()
        {
            RoughCollider.Update(_transform);
            foreach (var collider in Colliders)
            {
                collider.Update(_transform);
            }
            _gameContext.Quadtree.Update(this);
        }

        internal void LogicTick(ulong frame, in TimeInterval time)
        {
            if (Destroyed)
            {
                return;
            }

            if (_objectMoved)
            {
                UpdateColliders();

                var intersecting = _gameContext.Quadtree.FindIntersecting(this);

                foreach (var intersect in intersecting)
                {
                    DoCollide(intersect, time);
                    intersect.DoCollide(this, time);
                }

                if (AffectsAreaPassability)
                {
                    _gameContext.Navigation.UpdateAreaPassability(this, false);
                }

                _objectMoved = false;
            }

            // TODO: Should there be a BeforeLogicTick where we update this?
            _behaviorUpdateContext.UpdateTime(time);

            if (ModelConditionFlags.Get(ModelConditionFlag.Attacking))
            {
                CurrentWeapon?.LogicTick(time);
            }
            if (ModelConditionFlags.Get(ModelConditionFlag.Sold))
            {
                Die(DeathType.Normal, time);
                ModelConditionFlags.Set(ModelConditionFlag.Sold, false);
            }

            AIUpdate?.Update(_behaviorUpdateContext);

            foreach (var behavior in _behaviorModules)
            {
                behavior.Update(_behaviorUpdateContext);
            }

            foreach (var (key, modifier) in _attributeModifiers)
            {
                if (!modifier.Applied)
                {
                    modifier.Apply(this, time);
                }
                else if (modifier.Invalid || modifier.Expired(time))
                {
                    modifier.Remove(this);
                    _attributeModifiers.Remove(key);
                }
                else
                {
                    modifier.Update(this, time);
                }
            }
        }

        public bool CanRecruitHero(ObjectDefinition definition)
        {
            foreach (var obj in Parent.Items)
            {
                if (obj.Definition.Name == definition.Name
                    && obj.Owner == Owner)
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanProduceObject(ObjectDefinition definition)
        {
            return ProductionUpdate?.CanProduceObject(definition) ?? true;
        }

        public bool CollidesWith(ICollidable other, bool twoDimensional = false)
        {
            if (RoughCollider == null || other.RoughCollider == null)
            {
                return false;
            }

            if (!RoughCollider.Intersects(other.RoughCollider, twoDimensional))
            {
                return false;
            }

            foreach (var collider in Colliders)
            {
                foreach (var otherCollider in other.Colliders)
                {
                    if (collider.Intersects(otherCollider, twoDimensional))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void DoCollide(GameObject collidingObject, in TimeInterval time)
        {
            // TODO: Can we avoid updating this every time?
            _behaviorUpdateContext.UpdateTime(time);

            foreach (var behavior in _behaviorModules)
            {
                behavior.OnCollide(_behaviorUpdateContext, collidingObject);
            }
        }

        private void HandleConstruction(in TimeInterval gameTime)
        {
            // Check if the unit is being constructed
            if (IsBeingConstructed())
            {
                var passed = gameTime.TotalTime - ConstructionStart;
                BuildProgress = Math.Clamp((float) passed.TotalSeconds / Definition.BuildTime, 0.0f, 1.0f);

                if (BuildProgress >= 1.0f)
                {
                    FinishConstruction();
                }
            }
        }

        internal Vector3 ToWorldspace(in Vector3 localPos)
        {
            var worldPos = Vector4.Transform(new Vector4(localPos, 1.0f), _transform.Matrix);
            return new Vector3(worldPos.X, worldPos.Y, worldPos.Z);
        }

        internal Transform ToWorldspace(in Transform localPos)
        {
            var worldPos = localPos.Matrix * _transform.Matrix;
            return new Transform(worldPos);
        }

        public T FindBehavior<T>()
        {
            // TODO: Cache this?
            return _behaviorModules.OfType<T>().FirstOrDefault();
        }

        internal IEnumerable<T> FindBehaviors<T>()
        {
            // TODO: Cache this?
            return _behaviorModules.OfType<T>();
        }

        public bool HasUpgrade(UpgradeTemplate upgrade)
        {
            if (upgrade == null)
            {
                return true;
            }

            return upgrade.Type == UpgradeType.Player
                ? Owner.HasUpgrade(upgrade)
                : _upgrades.Contains(upgrade.Name);
        }

        internal void StartConstruction(in TimeInterval gameTime)
        {
            if (IsStructure)
            {
                ModelConditionFlags.SetAll(false);
                ModelConditionFlags.Set(ModelConditionFlag.ActivelyBeingConstructed, true);
                ModelConditionFlags.Set(ModelConditionFlag.AwaitingConstruction, true);
                ModelConditionFlags.Set(ModelConditionFlag.PartiallyConstructed, true);
                ConstructionStart = gameTime.TotalTime;
            }
        }

        internal void FinishConstruction()
        {
            ClearModelConditionFlags();
            EnergyProduction += Definition.EnergyProduction;
        }

        public bool IsBeingConstructed()
        {
            return ModelConditionFlags.Get(ModelConditionFlag.ActivelyBeingConstructed) ||
                   ModelConditionFlags.Get(ModelConditionFlag.AwaitingConstruction) ||
                   ModelConditionFlags.Get(ModelConditionFlag.PartiallyConstructed);
        }

        internal void UpdateDamageFlags(Fix64 healthPercentage)
        {
            // TODO: SoundOnDamaged
            // TODO: SoundOnReallyDamaged
            // TODO: SoundDie
            // TODO: TransitionDamageFX

            var oldDamageType = _bodyDamageType;

            if (healthPercentage < (Fix64) GameContext.AssetLoadContext.AssetStore.GameData.Current.UnitReallyDamagedThreshold)
            {
                ModelConditionFlags.Set(ModelConditionFlag.ReallyDamaged, true);
                ModelConditionFlags.Set(ModelConditionFlag.Damaged, false);
                _bodyDamageType = BodyDamageType.ReallyDamaged;
            }
            else if (healthPercentage < (Fix64) GameContext.AssetLoadContext.AssetStore.GameData.Current.UnitDamagedThreshold)
            {
                ModelConditionFlags.Set(ModelConditionFlag.ReallyDamaged, false);
                ModelConditionFlags.Set(ModelConditionFlag.Damaged, true);
                _bodyDamageType = BodyDamageType.Damaged;
            }
            else
            {
                ModelConditionFlags.Set(ModelConditionFlag.ReallyDamaged, false);
                ModelConditionFlags.Set(ModelConditionFlag.Damaged, false);
                _bodyDamageType = BodyDamageType.Pristine;
            }

            if (oldDamageType == _bodyDamageType)
            {
                return;
            }

            foreach (var behavior in _behaviorModules)
            {
                behavior.OnDamageStateChanged(
                    _behaviorUpdateContext,
                    oldDamageType,
                    _bodyDamageType);
            }
        }

        internal void LocalLogicTick(in TimeInterval gameTime, float tickT, HeightMap heightMap)
        {
            if (Destroyed)
            {
                return;
            }

            if (LifeTime != null && gameTime.TotalTime > LifeTime)
            {
                Die(DeathType.Normal, gameTime);
                Destroyed = true;
                return;
            }

            HandleConstruction(gameTime);

            _rallyPointMarker?.LocalLogicTick(gameTime, tickT, heightMap);
        }

        internal void BuildRenderList(RenderList renderList, Camera camera, in TimeInterval gameTime)
        {
            if (Destroyed || ModelConditionFlags.Get(ModelConditionFlag.Sold) || Hidden)
            {
                return;
            }

            var renderItemConstantsPS = new MeshShaderResources.RenderItemConstantsPS
            {
                HouseColor = Owner.Color.ToVector3(),
                Opacity = IsPlacementPreview ? 0.7f : 1.0f,
                TintColor = IsPlacementInvalid ? new Vector3(1, 0.3f, 0.3f) : Vector3.One,
            };

            // This must be done after processing anything that might update this object's transform.
            var worldMatrix = ModelTransform.Matrix * _transform.Matrix;
            Drawable.BuildRenderList(renderList, camera, gameTime, worldMatrix, renderItemConstantsPS);

            if ((IsSelected || IsPlacementPreview) && _rallyPointMarker != null && RallyPoint != null)
            {
                _rallyPointMarker._transform.Translation = RallyPoint.Value;

                // TODO: check if this should be drawn with transparency?
                _rallyPointMarker.BuildRenderList(renderList, camera, gameTime);
            }

            ModelConditionFlags.BitsChanged = false;
        }

        public void ClearModelConditionFlags()
        {
            ModelConditionFlags.SetAll(false);
        }

        public void OnLocalSelect(AudioSystem gameAudio)
        {
            var audioEvent = Definition.VoiceSelect?.Value;
            if (audioEvent != null && ParentHorde == null)
            {
                gameAudio.PlayAudioEvent(this, audioEvent);
            }
        }

        //TODO: make sure to play the correct voice event (e.g. VoiceMoveGroup etc.)
        public void OnLocalMove(AudioSystem gameAudio)
        {
            var audioEvent = Definition.VoiceMove?.Value;
            if (audioEvent != null && ParentHorde == null)
            {
                gameAudio.PlayAudioEvent(this, audioEvent);
            }
        }

        //TODO: use the target to figure out which sound triggers
        public void OnLocalAttack(AudioSystem gameAudio)
        {
            var audioEvent = Definition.VoiceAttack?.Value;
            if (audioEvent != null && ParentHorde == null)
            {
                gameAudio.PlayAudioEvent(this, audioEvent);
            }
        }

        internal void SetWeaponSetCondition(WeaponSetConditions condition, bool value)
        {
            WeaponSetConditions.Set(condition, value);
            UpdateWeaponSet();
        }

        private void UpdateWeaponSet()
        {
            _weaponSet.Update();
        }

        internal void SetWarhead(WeaponTemplate weaponTemplate)
        {
            // TODO
        }

        public bool CanPurchase(CommandButton button)
        {
            if (button.Object != null && button.Object.Value != null)
            {
                return HasUpgrade(button.NeededUpgrade?.Value)
                    && CanConstructUnit(button.Object.Value);
            }
            if (button.Upgrade != null && button.Upgrade.Value != null)
            {
                return HasUpgrade(button.NeededUpgrade?.Value)
                    && CanEnqueueUpgrade(button.Upgrade.Value);
            }
            if (button.Command == CommandType.CastleUnpack)
            {
                var castleBehavior = FindBehavior<CastleBehavior>();
                return HasEnoughMoney(castleBehavior.GetUnpackCost(Owner));
            }
            return true;
        }

        public bool HasEnoughMoney(float cost) => Owner.BankAccount.Money >= cost;

        public bool CanConstructUnit(ObjectDefinition objectDefinition)
        {
            return objectDefinition != null
                && HasEnoughMoney(objectDefinition.BuildCost)
                && Owner.CanProduceObject(Parent, objectDefinition)
                && CanProduceObject(objectDefinition);
        }

        public bool CanEnqueueUpgrade(UpgradeTemplate upgrade)
        {
            if (upgrade == null || ProductionUpdate == null)
            {
                return false;
            }

            var userHasEnoughMoney = HasEnoughMoney(upgrade.BuildCost);
            var hasQueuedUpgrade = ProductionUpdate.ProductionQueue.Any(x => x.UpgradeDefinition == upgrade);
            var canEnqueue = ProductionUpdate.CanEnqueue();
            var hasUpgrade = HasUpgrade(upgrade);

            var existingUpgrades = GetUpgradesCompleted();
            existingUpgrades.Add(upgrade.Name);

            var upgradeModuleCanUpgrade = false;
            foreach (var upgradeModule in FindBehaviors<UpgradeModule>())
            {
                if (upgradeModule.CanUpgrade(existingUpgrades))
                {
                    upgradeModuleCanUpgrade = true;
                    break;
                }
            }

            return userHasEnoughMoney && canEnqueue && !hasQueuedUpgrade && !hasUpgrade && upgradeModuleCanUpgrade;
        }

        public HashSet<string> GetUpgradesCompleted()
        {
            _upgradesAll.Clear();
            _upgradesAll.UnionWith(_upgrades);
            _upgradesAll.UnionWith(Owner.UpgradesCompleted);
            return _upgradesAll;
        }

        public void Upgrade(UpgradeTemplate upgrade)
        {
            switch (upgrade.Type)
            {
                case UpgradeType.Object:
                    _upgrades.Add(upgrade.Name);
                    break;
                case UpgradeType.Player:
                    Owner.AddUpgrade(upgrade, UpgradeStatus.Completed);
                    break;
                default:
                    throw new InvalidOperationException("This should not happen");
            }
        }

        public void RemoveUpgrade(UpgradeTemplate upgrade)
        {
            if (upgrade.Type == UpgradeType.Object)
            {
                _upgrades.Remove(upgrade.Name);
            }
            else if (upgrade.Type == UpgradeType.Player)
            {
                Owner.RemoveUpgrade(upgrade);
            }
            else
            {
                throw new InvalidOperationException("This should not happen");
            }
        }

        public bool CanAttackObject(GameObject obj)
        {
            if (!CanAttack) return false;

            var combinedWeaponCapabilities = Definition.WeaponSets.Values
                .SelectMany(x => x.Slots.Values.Select(s => s.Weapon.Value?.AntiMask))
                .Where(f => f.HasValue).Select(f => f.Value).Aggregate((s, t) => s | t);

            return combinedWeaponCapabilities.CanAttackObject(obj);
        }

        public bool IsAirborne(float groundDelta = 0.1f)
        {
            return Translation.Z - GameContext.Terrain.HeightMap.GetHeight(Translation.X, Translation.Y) > groundDelta;
        }

        internal void Kill(DeathType deathType, TimeInterval time)
        {
            _body.DoDamage(DamageType.Unresistable, _body.Health, deathType, time);
        }

        internal void Die(DeathType deathType, TimeInterval time)
        {
            Logger.Info("Object dying " + deathType);

            ModelConditionFlags.Set(ModelConditionFlag.ReallyDamaged, false);
            ModelConditionFlags.Set(ModelConditionFlag.Damaged, false);

            IsSelectable = false;

            // TODO: Can we avoid updating this every time?
            _behaviorUpdateContext.UpdateTime(time);

            // If there are multiple SlowDeathBehavior modules,
            // we need to use ProbabilityModifier to choose between them.
            var slowDeathBehaviors = FindBehaviors<SlowDeathBehavior>()
                .Where(x => x.IsApplicable(deathType))
                .ToList();
            if (slowDeathBehaviors.Count > 1)
            {
                var sumProbabilityModifiers = slowDeathBehaviors.Sum(x => x.ProbabilityModifier);
                var random = _gameContext.Random.Next(sumProbabilityModifiers);
                var cumulative = 0;
                foreach (var deathBehavior in slowDeathBehaviors)
                {
                    cumulative += deathBehavior.ProbabilityModifier;
                    if (random < cumulative)
                    {
                        deathBehavior.OnDie(_behaviorUpdateContext, deathType);
                        return;
                    }
                }
                throw new InvalidOperationException();
            }

            foreach (var dieModule in _behaviorModules)
            {
                dieModule.OnDie(_behaviorUpdateContext, deathType);
            }
        }

        internal void Destroy()
        {
            Drawable.Destroy();
            Destroyed = true;
        }

        internal void GainExperience(int experience)
        {
            ExperienceValue += (int) (ExperienceMultiplier * experience);
        }

        internal void SpecialPowerAtLocation(SpecialPower specialPower, Vector3 location)
        {
            var oclSpecialPowers = FindBehaviors<OCLSpecialPowerModule>();

            foreach (var oclSpecialPower in oclSpecialPowers)
            {
                if (oclSpecialPower.Matches(specialPower))
                {
                    oclSpecialPower.Activate(location);
                }
            }
            _gameContext.AudioSystem.PlayAudioEvent(specialPower.InitiateAtLocationSound.Value);
        }

        internal void Load(SaveFileReader reader)
        {
            reader.ReadVersion(7);

            ID = reader.ReadObjectID();

            var transform = reader.ReadMatrix4x3();
            SetTransformMatrix(transform.ToMatrix4x4());

            reader.__Skip(12);

            var drawableID = reader.ReadUInt32();

            _name = reader.ReadAsciiString();

            reader.__Skip(6);

            var geometry = new Geometry();

            geometry.Load(reader);

            reader.ReadVersion(1);
            var position = reader.ReadVector3();
            var unknown = reader.ReadSingle(); // 360
            reader.__Skip(29);
            var unknown16 = reader.ReadSingle(); // 360
            var unknown17 = reader.ReadSingle(); // 360
            reader.__Skip(4);

            var disabledTypes = reader.ReadBitArray<DisabledType>();

            reader.__Skip(75);

            var numUpgrades = reader.ReadUInt16();
            for (var i = 0; i < numUpgrades; i++)
            {
                var upgradeName = reader.ReadAsciiString();
                var upgrade = _gameContext.AssetLoadContext.AssetStore.Upgrades.GetByName(upgradeName);
                Upgrade(upgrade);
            }

            var team = reader.ReadAsciiString(); // teamPlyrAmerica

            reader.__Skip(16);

            var someCount = reader.ReadByte();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();
            reader.ReadUInt32();

            for (var i = 0; i < someCount; i++)
            {
                var someString1 = reader.ReadAsciiString(); // OuterPerimeter7, InnerPerimeter7
                reader.ReadBoolean();
                reader.ReadBoolean();
                reader.ReadBoolean();
            }

            reader.__Skip(17);

            // Modules
            var numModules = reader.ReadUInt16();
            for (var i = 0; i < numModules; i++)
            {
                var moduleTag = reader.ReadAsciiString();

                reader.BeginSegment();

                var module = GetModuleByTag(moduleTag);
                module.Load(reader.Inner);

                reader.EndSegment();
            }

            reader.__Skip(9);
            var someCount2 = reader.ReadUInt32();
            for (var i = 0; i < someCount2; i++)
            {
                var condition = reader.ReadAsciiString();
            }
            reader.__Skip(7);

            var weaponSet = new WeaponSet(this);
            weaponSet.Load(reader);

            var specialPowers = reader.ReadBitArray<SpecialPowerType>();

            reader.ReadBoolean(); // 0
            reader.ReadBoolean(); // 1
            reader.ReadBoolean(); // 1
        }

        void IInspectable.DrawInspector()
        {
            if (ImGui.Button("Bring into view"))
            {
                _gameContext.Scene3D.CameraController.GoToObject(this);
            }

            ImGui.SameLine();

            if (ImGui.Button("Select"))
            {
                _gameContext.Scene3D.LocalPlayer.SelectUnits(new[] { this });
            }

            ImGui.SameLine();

            if (ImGui.Button("Kill"))
            {
                // TODO: Time isn't right.
                Kill(DeathType.Exploded, _gameContext.Scene3D.Game.MapTime);
            }

            if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.LabelText("DisplayName", Definition.DisplayName);

                var translation = _transform.Translation;
                if (ImGui.DragFloat3("Position", ref translation))
                {
                    _transform.Translation = translation;
                }

                // TODO: Make this editable
                ImGui.LabelText("ModelConditionFlags", ModelConditionFlags.DisplayName);

                var speed = Speed;
                if (ImGui.InputFloat("Speed", ref speed))
                {
                    Speed = speed;
                }

                var lift = Lift;
                if (ImGui.InputFloat("Lift", ref lift))
                {
                    Lift = lift;
                }
            }

            foreach (var drawModule in Drawable.DrawModules)
            {
                if (ImGui.CollapsingHeader(drawModule.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    drawModule.DrawInspector();
                }
            }

            if (ImGui.CollapsingHeader(_body.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
            {
                _body.DrawInspector();
            }

            if (CurrentWeapon != null)
            {
                var weapon = CurrentWeapon;
                if (ImGui.CollapsingHeader("Weapon", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    weapon.DrawInspector();
                }
            }

            foreach (var behaviorModule in BehaviorModules)
            {
                if (ImGui.CollapsingHeader(behaviorModule.GetType().Name, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    behaviorModule.DrawInspector();
                }
            }
        }
    }
}
