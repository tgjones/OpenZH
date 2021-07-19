using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using OpenSage.Mathematics;
using OpenSage.Tools.AptEditor.Apt.Editor;
using OpenSage.Tools.AptEditor.Apt.Editor.FrameItems;
using OpenSage.Tools.AptEditor.Util;

namespace OpenSage.Tools.AptEditor.UI.Widgets
{
    internal class FrameItemList : IWidget
    {
        public const string Name = "Frame Properties";
        private FrameItemUtilities? _utilities;
        // private VMConsole? _currentFrameAction;
        private int? _newPlaceObjectDepth;
        private int? _newPlaceCharacter;
        private ErrorType? _whyCannotPlaceCharacter;

        public void Draw(AptSceneManager manager)
        {
            var maybeNew = FrameItemUtilities.Reset(manager, _utilities);
            if (maybeNew is null)
            {
                return;
            }
            if (!ReferenceEquals(_utilities, maybeNew))
            {
                // if this is a different frame (imply _utilities.Active == true)
                ImGui.SetNextWindowSize(new Vector2(0, 0));
                _utilities = maybeNew;
                ResetCreatePlaceObjectForm();
            }

            if (ImGui.Begin(Name))
            {
                var id = _utilities.GetHashCode();

                // frame label
                ImGui.Text("Frame labels");
                ImGui.Button("New Frame label");
                foreach (var label in _utilities.FrameLabels)
                {
                    using var _ = new ImGuiIDHelper("Frame labels", ref id);
                    ImGui.Text($"{label.FrameId}");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 1, 0, 1), label.Name);
                    ImGui.SameLine();
                    ImGui.Text($"{label.Flags}");
                    ImGui.SameLine(ImGui.GetWindowWidth() - 100);
                    ImGui.Button("Remove");
                    ImGui.NewLine();
                    // TODO: is frame label globally visible?
                }
                ImGui.Separator();

                // background color
                if (!_utilities.BackgroundColors.Any())
                {
                    ImGui.Button("Set background color");
                }
                foreach (var color in _utilities.BackgroundColors)
                {
                    using var _ = new ImGuiIDHelper("Background colors", ref id);
                    ImGui.Text("Background Color");
                    ImGui.SameLine();
                    ImGui.ColorButton("Background color", color.Color.ToColorRgbaF().ToVector4());
                    ImGui.SameLine(ImGui.GetWindowWidth() - 100);
                    ImGui.Button("Remove");
                }
                ImGui.Separator();

                // actions
                ImGui.Button("Add frame Action");
                foreach (var item in _utilities.FrameActions)
                {
                    using var _ = new ImGuiIDHelper("Frame actions", ref id);
                    if (ImGui.Button("Frame Action"))
                    {
                        manager.CurrentActions = item.Instructions;
                    }
                }
                ImGui.Separator();

                // initActions
                ImGui.Button("Add InitAction");
                var indexColor = new Vector4(0, 1, 1, 1);
                var typeColor = new Vector4(0, 1, 0, 1);
                foreach (var item in _utilities.InitActions)
                {
                    using var _ = new ImGuiIDHelper("Init actions", ref id);
                    ImGui.TextColored(indexColor, $"{item.Sprite}");
                    ImGui.SameLine(35, 5);
                    if (ImGui.Button("Sprite InitAction"))
                    {
                        manager.CurrentActions = item.Instructions;
                    }
                }
                ImGui.Separator();

                // placeobjects
                ImGui.Text("Place commands");
                DrawCreatePlaceObjectForm();
                ImGui.Separator();
                ImGui.Indent(10);
                int? remove = null;
                foreach (var (depth, placeObject) in _utilities.PlaceObjects)
                {
                    using var _ = new ImGuiIDHelper("PlaceObjects", ref id);
                    ImGui.Text($"Depth: {depth}");
                    ImGui.SameLine();
                    if (ImGui.Button("Remove"))
                    {
                        remove = depth;
                    }

                    if (placeObject is null)
                    {
                        ImGui.Text("Remove character in the current depth.");
                        continue;
                    }

                    ImGui.TextColored(typeColor, "Character");

                    ProcessPlaceCharacter(placeObject);
                    ImGui.Spacing();
                    ProcessTransform(placeObject);
                    ImGui.Spacing();
                    ProcessColorTransform(placeObject);
                    ImGui.Spacing();
                    ProcessRatio(placeObject);
                    ImGui.Spacing();
                    ProcessName(placeObject);
                    ImGui.Spacing();
                    ProcessClipEvents(placeObject);
                    ImGui.Separator();
                }
                ImGui.Unindent();

                if (remove is int removeValue)
                {
                    _utilities.RemovePlaceObject(removeValue);
                }
            }
            ImGui.End();

            // Draw Frame's Action / InitAction
            // _currentFrameAction?.Draw(manager);
        }

        private void DrawCreatePlaceObjectForm()
        {
            if (_utilities is null)
            {
                throw new InvalidOperationException();
            }

            if (_newPlaceObjectDepth is not int depth)
            {
                if (ImGui.Button("New"))
                {
                    _newPlaceObjectDepth = default(int);
                }
                return;
            }

            if (ImGui.RadioButton("Place", _newPlaceCharacter is int))
            {
                _newPlaceCharacter = default(int);
                _utilities.IsCharacterPlaceable(_newPlaceCharacter.Value,
                                                out _whyCannotPlaceCharacter);
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Remove", _newPlaceCharacter is null))
            {
                _newPlaceCharacter = null;
            }
            ImGui.InputInt("Depth", ref depth);
            _newPlaceObjectDepth = depth;
            if (_newPlaceCharacter is int character)
            {
                if (ImGui.InputInt("Character", ref character))
                {
                    _newPlaceCharacter = character;
                    if (_utilities.IsCharacterPlaceable(character,
                                                        out _whyCannotPlaceCharacter))
                    {
                        _whyCannotPlaceCharacter = null;
                    }
                }
            }
            var currentError = _utilities.PlaceObjects.ContainsKey(depth)
                ? ErrorType.PlaceObjectDepthAlreadyTaken
                : _whyCannotPlaceCharacter;
            if (currentError is ErrorType error)
            {
                var reason = string.Empty;
                foreach (var c in error.ToString())
                {
                    reason += reason.Any() && char.IsUpper(c)
                        ? $" {char.ToLower(c)}"
                        : c;
                }

                ImGui.TextColored(ColorRgbaF.Red.ToVector4(), reason);
            }
            else if (ImGui.Button("Create"))
            {
                _utilities.AddPlaceObject(depth, _newPlaceCharacter);
                ResetCreatePlaceObjectForm();
            }
        }

        private void ResetCreatePlaceObjectForm()
        {
            _newPlaceObjectDepth = null;
            _newPlaceCharacter = null;
            _whyCannotPlaceCharacter = null;
        }

        private void ProcessPlaceCharacter(LogicalPlaceObject placeObject)
        {
            if (!placeObject.ModifyingExisting)
            {
                ImGui.Button("Initialize");
                ImGui.SameLine();
                if(placeObject.Character is int character)
                {
                    if (ImGui.InputInt("##place character", ref character))
                    {
                        if(_utilities!.IsCharacterPlaceable(character, out var whyNot))
                        {
                            placeObject.SetCharacter(character);
                        }
                        else
                        {
                            ImGui.TextColored(ColorRgbaF.Red.ToVector4(), whyNot.ToString());
                        }
                    }
                }
                else
                {
                    ImGui.TextColored(ColorRgbaF.Red.ToVector4(), "Invalid character");
                }
            }
            else
            {
                if (!placeObject.Character.HasValue)
                {
                    ImGui.Button("Edit");
                }
                else
                {
                    ImGui.Button("Replace with");
                }
                ImGui.SameLine();
                ImGui.Button(ToStringOrDefault(placeObject.Character, "Existing"));
            }
        }

        private static void ProcessTransform(LogicalPlaceObject placeObject)
        {
            if (!placeObject.HasTransfrom)
            {
                if (ImGui.Button("Add Transformation"))
                {
                    placeObject.InitTransform();
                }
                return;
            }

            ImGui.Text("Transformation");
            ImGui.SameLine();
            if (ImGui.Button("Remove"))
            {
                placeObject.DisableTransform();
            }

            const float radToDeg = (180 / MathF.PI);

            var rotationInDegrees = placeObject.Rotation * radToDeg;
            if (ImGui.InputFloat("Rotation", ref rotationInDegrees, 1, 10, "%.3f deg"))
            {
                placeObject.SetRotation(rotationInDegrees / radToDeg);
            }

            var skewInDegrees = placeObject.Skew * (180 / MathF.PI);
            if (ImGui.InputFloat("Skew", ref skewInDegrees, 1, 10, "%.3f deg"))
            {
                placeObject.SetSkew(skewInDegrees / radToDeg);
            }

            var scale = placeObject.Scale;
            if (ImGui.InputFloat2("Scale", ref scale))
            {
                placeObject.SetScale(scale);
            }

            var translation = placeObject.Translation;
            if (ImGui.InputFloat2("Translation", ref translation))
            {
                placeObject.SetTranslation(translation);
            }
        }

        private static void ProcessColorTransform(LogicalPlaceObject placeObject)
        {
            if (placeObject.ColorTransform is not Vector4 value)
            {
                if (ImGui.Button("Add Color Transform"))
                {
                    placeObject.SetColor(Vector4.One);
                }
                return;
            }
            ImGui.Text("Color Transform");
            ImGui.SameLine();
            if (ImGui.ColorEdit4("Color Transform", ref value))
            {
                placeObject.SetColor(value);
            }
            ImGui.SameLine();
            if (ImGui.Button("Remove"))
            {
                placeObject.SetColor(null);
            }
        }

        private static void ProcessRatio(LogicalPlaceObject placeObject)
        {
            ImGui.Text($"Ratio: {ToStringOrDefault(placeObject.Ratio, "None")}");
        }

        private static void ProcessName(LogicalPlaceObject placeObject)
        {
            if (placeObject.Name == null)
            {
                if (ImGui.Button("Add Name"))
                {
                    placeObject.SetName($"Place {placeObject.Character},{placeObject.GetHashCode()}");
                }
                return;
            }

            var name = placeObject.Name;
            var edited = ImGui.InputText("Name", ref name, (uint) (name.Length + 10));
            ImGui.SameLine();
            if (ImGui.Button("Remove"))
            {
                placeObject.SetName(null);
            }
            else if (edited)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    ImGui.TextColored(ColorRgbaF.Red.ToVector4(), "Prefer a non-empty name.");
                }
                else
                {
                    placeObject.SetName(name);
                }
            }
        }

        private static void ProcessClipEvents(LogicalPlaceObject placeObject)
        {
            if (placeObject.ClipEvents == null)
            {
                ImGui.Button("Add Ciip Events");
                return;
            }

            ImGui.Text($"Clip Event Count: {placeObject.ClipEvents.Count}");
            return;
        }

        private static string ToStringOrDefault<T>(T? item, string defaultValue) where T : struct
        {
            if (item is T t)
            {
                return t.ToString() ?? defaultValue;
            }
            return defaultValue;
        }
    }
}
