using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using OpenSage.Data.Apt;
using OpenSage.Data.Apt.Characters;
using OpenSage.Data.Apt.FrameItems;
using OpenSage.Tools.AptEditor.Apt.Editor.FrameItems;
using OpenSage.Tools.AptEditor.UI;
using Action = OpenSage.Data.Apt.FrameItems.Action;

namespace OpenSage.Tools.AptEditor.Apt.Editor
{
    internal class FrameItemUtilities
    {
        public List<FrameItem> CurrentItems => _storedFrame.FrameItems;
        public IReadOnlyDictionary<int, LogicalPlaceObject?> PlaceObjects => _placeObjects;
        public IReadOnlyList<FrameLabel> FrameLabels => _frameLabels;
        public IReadOnlyList<LogicalAction> FrameActions => _frameActions;
        public IReadOnlyList<LogicalInitAction> InitActions => _initActions;
        public IReadOnlyList<BackgroundColor> BackgroundColors => _backgroundColors;

        private readonly Playable _character;
        private readonly AptSceneManager _manager;
        private readonly Frame _storedFrame;
        private readonly SortedDictionary<int, LogicalPlaceObject?> _placeObjects = new SortedDictionary<int, LogicalPlaceObject?>();
        private readonly List<FrameLabel> _frameLabels = new List<FrameLabel>();
        private readonly List<LogicalAction> _frameActions = new List<LogicalAction>();
        private readonly List<LogicalInitAction> _initActions = new List<LogicalInitAction>();
        private readonly List<BackgroundColor> _backgroundColors = new List<BackgroundColor>();

        private FrameItemUtilities(Playable character, AptSceneManager manager, Frame currentFrame)
        {
            _character = character;
            _manager = manager;
            _storedFrame = currentFrame;
            foreach (var frameItem in CurrentItems)
            {
                switch (frameItem)
                {
                    case PlaceObject placeObject:
                        // TODO placeObject Override?
                        _placeObjects.Add(placeObject.Depth, new LogicalPlaceObject(EditAndUpdate, placeObject));
                        break;
                    case RemoveObject removeObject:
                        _placeObjects.Add(removeObject.Depth, null);
                        break;
                    case FrameLabel frameLabel:
                        _frameLabels.Add(frameLabel);
                        break;
                    case Action action:
                        _frameActions.Add(new LogicalAction(action));
                        break;
                    case InitAction initAction:
                        _initActions.Add(new LogicalInitAction(initAction));
                        break;
                    case BackgroundColor background:
                        _backgroundColors.Add(background);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static FrameItemUtilities? Reset(AptSceneManager manager, FrameItemUtilities? current)
        {
            if (manager.IsCurrentCharacterImported)
            {
                return null;
            }

            if (manager.CurrentCharacter is not Playable p)
            {
                return null;
            }

            if (manager.CurrentFrameWrapped is not int frameNumber)
            {
                return null;
            }

            var currentFrame = p.Frames[frameNumber];
            if (ReferenceEquals(p, current?._character) &&
                ReferenceEquals(currentFrame, current?._storedFrame))
            {
                return current;
            }

            return new FrameItemUtilities(p, manager, currentFrame);
        }

        public void AddPlaceObject(int depth, int? character)
        {
            if (_placeObjects.ContainsKey(depth))
            {
                throw new AptEditorException(ErrorType.PlaceObjectDepthAlreadyTaken);
            }
            var currentIndex = CurrentItems.Count;
            FrameItem? created;
            LogicalPlaceObject? logicalPO;
            if (character is int index)
            {
                var po = PlaceObject.Create(depth, index);
                created = po;
                logicalPO = new LogicalPlaceObject(EditAndUpdate, po);
            }
            else
            {
                created = RemoveObject.Create(depth);
                logicalPO = null;
            }

            void Add()
            {
                CurrentItems.Add(created);
                _placeObjects.Add(depth, logicalPO);
            }
            void Remove()
            {
                CurrentItems.RemoveAt(currentIndex);
                _placeObjects.Remove(depth);
            }
            EditAndUpdate(new EditAction(Add, Remove, "Add PlaceObject"));
        }

        public void RemovePlaceObject(int depth)
        {
            if (!_placeObjects.ContainsKey(depth))
            {
                throw new AptEditorException(ErrorType.InvalidPlaceObjectDepth);
            }
            var toBeRemovedIndex = CurrentItems.FindIndex(item => item switch
            {
                PlaceObject p => p.Depth == depth,
                RemoveObject r => r.Depth == depth,
                _ => false
            });
            var toBeRemoved = CurrentItems[toBeRemovedIndex];
            var logicalPOToBeRemoved = _placeObjects[depth];
            void Remove()
            {
                CurrentItems.RemoveAt(toBeRemovedIndex);
                _placeObjects.Remove(depth);
            }
            void Restore()
            {
                CurrentItems.Insert(toBeRemovedIndex, toBeRemoved);
                _placeObjects.Add(depth, logicalPOToBeRemoved);
            }
            EditAndUpdate(new EditAction(Remove, Restore, "Remove PlaceObject"));
        }

        public bool IsCharacterPlaceable(int targetCharacter, out ErrorType? whyNotPlaceable)
        {
            var list = _character.Container.Movie.Characters;
            if (targetCharacter < 0 || targetCharacter >= list.Count)
            {
                whyNotPlaceable = ErrorType.InvalidCharacterId;
                return false;
            }
            var character = list[targetCharacter];
            if (character is not Playable playable)
            {
                whyNotPlaceable = null;
                return true;
            }
            return IsCharacterPlaceable(playable, _character, out whyNotPlaceable, true);
        }

        private static bool IsCharacterPlaceable(Playable destination, Character target, out ErrorType? whyNotPlaceable, bool first)
        {
            if(ReferenceEquals(destination, target))
            {
                whyNotPlaceable = first
                            ? ErrorType.PlaceObjectCircularDependency
                            : ErrorType.InvalidCharacterInDependency;
                return false;
            }
            var list = destination.Container.Movie.Characters;
            foreach (var frame in destination.Frames)
            {
                foreach (var item in frame.FrameItems)
                {
                    if (item is not PlaceObject po)
                    {
                        continue;
                    }
                    if (!po.Flags.HasFlag(PlaceObjectFlags.HasCharacter))
                    {
                        continue;
                    }
                    if (po.Character < 0 || po.Character >= list.Count)
                    {
                        whyNotPlaceable = ErrorType.InvalidCharacterInDependency;
                        return false;
                    }
                    if (list[po.Character] is not Playable placed)
                    {
                        continue;
                    }
                    if (ReferenceEquals(placed, target))
                    {
                        whyNotPlaceable = first
                            ? ErrorType.PlaceObjectCircularDependency
                            : ErrorType.InvalidCharacterInDependency;
                        return false;
                    }
                    if (!IsCharacterPlaceable(placed, destination, out whyNotPlaceable, false))
                    {
                        return false;
                    }
                    if (!IsCharacterPlaceable(placed, target, out whyNotPlaceable, false))
                    {
                        return false;
                    }
                }
            }

            whyNotPlaceable = null;
            return true;
        }

        private void EditAndUpdate(IEditAction edit)
        {
            edit.OnEdit += delegate
            {
                if (_manager.CurrentCharacter != _character)
                {
                    return; // to do: swtich to character
                }
                _manager.PlayToFrame(_manager.CurrentFrameWrapped!.Value);
            };
            _manager.AptManager!.Edit(edit);
        }
    }
}
