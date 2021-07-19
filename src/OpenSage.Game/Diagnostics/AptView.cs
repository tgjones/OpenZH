using System.Numerics;
using ImGuiNET;
using OpenSage.Data.Apt.Characters;
using OpenSage.Diagnostics.Util;
using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Library;

namespace OpenSage.Diagnostics
{
    internal sealed class AptView : DiagnosticView
    {
        public override string DisplayName { get; } = "APT Windows";
        private DisplayItem _selectedItem;
        private int? _currentClipDepth;

        public AptView(DiagnosticViewContext context)
            : base(context)
        {

        }

        private object CreateObject(Value value)
        {
            switch (value.Type)
            {
                case ValueType.String:
                    return value.ToString();
                case ValueType.Boolean:
                    return value.ToBoolean();
                case ValueType.Short:
                case ValueType.Integer:
                    return value.ToInteger();
                case ValueType.Float:
                    return value.ToFloat();
                case ValueType.Object:
                    if (value.ToObject() is Function)
                        return "[FUNCTION]";
                    else if (value.ToObject() is ASArray)
                        return "[ARRAY]";
                    else
                        return "[OBJECT]";
                case ValueType.Undefined:
                    return "[UNDEFINED]";
            }

            return null;
        }

        protected override void DrawOverride(ref bool isGameViewFocused)
        {
            foreach (var aptWindow in Game.Scene2D.AptWindowManager.WindowStack)
            {
                ImGui.BeginChild("DisplayLists", new Vector2(400, 0), true, ImGuiWindowFlags.HorizontalScrollbar);

                if (ImGui.TreeNodeEx(aptWindow.Name, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnArrow))
                {
                    DrawDisplayListRecursive(0, aptWindow.Root);
                    ImGui.TreePop();
                }
                ImGui.EndChild();

                ImGui.SameLine();
                ImGui.BeginChild("ScriptObject");
                if (_selectedItem != null)
                {
                    ImGuiUtility.BeginPropertyList();
                    ImGuiUtility.PropertyRow("Type", _selectedItem.Character.GetType());
                    ImGuiUtility.PropertyRow("ClipDepth", _selectedItem.ClipDepth);
                    ImGuiUtility.PropertyRow("Visible", _selectedItem.Visible);

                    ImGuiUtility.EndPropertyList();

                    switch (_selectedItem)
                    {
                        case SpriteItem si:
                            ImGuiUtility.BeginPropertyList();
                            // CurrentFrame shows the next frame that should be played
                            ImGuiUtility.PropertyRow("CurrentFrame", si.CurrentFrame - 1);
                            ImGuiUtility.PropertyRow("State", si.State);
                            ImGuiUtility.EndPropertyList();

                            if (ImGui.CollapsingHeader("FrameLabels", ImGuiTreeNodeFlags.DefaultOpen))
                            {
                                ImGuiUtility.BeginPropertyList();
                                foreach (var frameLabels in si.FrameLabels)
                                {
                                    ImGuiUtility.PropertyRow(frameLabels.Key, frameLabels.Value);
                                }
                                ImGuiUtility.EndPropertyList();
                            }
                            break;
                        case RenderItem ri:
                            if (_selectedItem.Character is Text)
                            {
                                var text = _selectedItem.Character as Text;
                                ImGuiUtility.BeginPropertyList();
                                ImGuiUtility.PropertyRow("Content", ri.TextValue?.Original ?? "null");
                                ImGuiUtility.PropertyRow("LocalizedContent", ri.TextValue?.Localized ?? "null");
                                ImGuiUtility.PropertyRow("InitialContent", text.Content);
                                ImGuiUtility.PropertyRow("InitialValue", text.Value);
                                ImGuiUtility.PropertyRow("Color", text.Color.ToString());
                                ImGuiUtility.PropertyRow("Multiline", text.Multiline);
                                ImGuiUtility.PropertyRow("Wordwrap", text.WordWrap);
                                ImGuiUtility.EndPropertyList();
                            }
                            else if (_selectedItem.Character is Shape)
                            {
                                var shape = _selectedItem.Character as Shape;
                                ImGuiUtility.BeginPropertyList();
                                ImGuiUtility.PropertyRow("GeometryID", shape.Geometry);
                                ImGuiUtility.EndPropertyList();
                            }
                            break;
                    }

                    if (ImGui.CollapsingHeader("Variables", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGuiUtility.BeginPropertyList();
                        if (_selectedItem.ScriptObject != null)
                        {
                            var so = _selectedItem.ScriptObject;
                            foreach (var key in so.GetAllProperties())
                            {
                                ImGuiUtility.PropertyRow(key, CreateObject(so.GetMember(key)));
                            }
                        }
                        ImGuiUtility.EndPropertyList();
                    }
                    // Constants are now handled by ActionContext
                    // TODO: How to view these constants?
                    /*
                    if (ImGui.CollapsingHeader("Constants", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGuiUtility.BeginPropertyList();
                        int index = 0;
                        if (_selectedItem.ScriptObject != null)
                        {
                            foreach (var variable in _selectedItem.ScriptObject?.Constants)
                            {
                                ImGuiUtility.PropertyRow($"{index++}", CreateObject(variable));
                            }
                        }
                        ImGuiUtility.EndPropertyList();
                    }
                    */
                }
                ImGui.EndChild();
            }
        }

        private void DrawDisplayListRecursive(int depth, DisplayItem item)
        {
            var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.OpenOnDoubleClick;

            // workaround for Apt Editor
            var type = item.GetType();
            if (type.FullName == "OpenSage.Tools.AptEditor.Apt.WrappedDisplayItem")
            {
                item = (DisplayItem) type.GetProperty("Item").GetValue(item);
            }


            if (_currentClipDepth.HasValue && (depth > _currentClipDepth.Value))
            {
                _currentClipDepth = null;
            }

            if (item.ClipDepth.HasValue)
            {
                _currentClipDepth = item.ClipDepth;
            }

            if (!(item is SpriteItem))
            {
                treeNodeFlags = ImGuiTreeNodeFlags.Leaf;
            }

            if (item == _selectedItem)
            {
                treeNodeFlags |= ImGuiTreeNodeFlags.Selected;
            }

            if (item.ClipDepth.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 1.0f, 0.5f, 1.0f));
            }
            else if (_currentClipDepth.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            }

            bool hasRenderCallback = false;
            if (item is RenderItem renderItem && renderItem.RenderCallback != null)
            {
                hasRenderCallback = true;
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.5f, 1.0f));
            }

            var opened = ImGui.TreeNodeEx($"[{depth}] {item.Name}", treeNodeFlags);

            if (hasRenderCallback)
            {
                ImGui.PopStyleColor();
            }

            if (_currentClipDepth.HasValue)
            {
                ImGui.PopStyleColor();
            }

            ImGuiUtility.DisplayTooltipOnHover("MovieClip: " + item.Character.Container.MovieName);

            if (ImGuiNative.igIsItemClicked(0) > 0)
            {
                SelectDisplayItem(item);
            }

            if (opened)
            {
                if (item is SpriteItem)
                {
                    var spriteItem = item as SpriteItem;
                    foreach (var pair in spriteItem.Content.Items)
                    {
                        DrawDisplayListRecursive(pair.Key, pair.Value);
                    }
                }

                ImGui.TreePop();
            }
        }

        private void SelectDisplayItem(DisplayItem item)
        {
            if (_selectedItem != null)
            {
                _selectedItem.Highlight = false;
                _selectedItem = null;
            }
            _selectedItem = item;
            _selectedItem.Highlight = true;
            Context.SelectedAptWindow = _selectedItem.Context.Window;
        }
    }
}
