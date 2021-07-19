using System.Numerics;
using ImGuiNET;
using OpenSage.Data.Apt;
using OpenSage.Data.Apt.Characters;
using OpenSage.Tools.AptEditor.Apt.Editor;

namespace OpenSage.Tools.AptEditor.UI.Widgets
{
    internal sealed class CharacterList : IWidget
    {
        private CharacterUtilities? _utilities = null;
        private int _lastSelectedCharacter = -1;
        private string _decidingExportName = string.Empty;
        private string? _newCharacterName = null;
        private bool _isCreatingNewShape = true;

        public void Draw(AptSceneManager manager)
        {
            if (_utilities?.Manager != manager.AptManager)
            {
                _lastSelectedCharacter = -1;
                _decidingExportName = string.Empty;
                _utilities = manager.AptManager is null
                    ? null
                    : new CharacterUtilities(manager.AptManager);
            }

            if (_utilities is null)
            {
                return;
            }

            if (ImGui.Begin("Characters"))
            {
                ImGui.Text("Characters");

                ImGui.PushID(1);
                DrawCharacterCreationForm();
                ImGui.PopID();

                ImGui.PushID(2);
                ListCharacterDescription(manager);
                ImGui.PopID();
            }
            ImGui.End();
        }

        private void DrawCharacterCreationForm()
        {
            if (_newCharacterName is null)
            {
                if (ImGui.Button("New Character"))
                {
                    _newCharacterName = string.Empty;
                }
                return;
            }

            if (ImGui.RadioButton("Shape", _isCreatingNewShape))
            {
                _isCreatingNewShape = true;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Sprite", !_isCreatingNewShape))
            {
                _isCreatingNewShape = false;
            }

            ImGui.InputText("Name", ref _newCharacterName, 64);

            if (_isCreatingNewShape)
            {

                if (ImGui.Button("Create"))
                {
                    _utilities!.CreateShape(_newCharacterName);
                    CleanCreationData();
                }
            }
            else if (ImGui.Button("Create"))
            {
                _utilities!.CreateSprite(_newCharacterName);
                CleanCreationData();
            }
        }

        private void ListCharacterDescription(AptSceneManager manager)
        {
            var indexColor = new Vector4(0, 1, 1, 1);
            var typeColor = new Vector4(0, 0.8f, 0.2f, 1);
            var exportColor = new Vector4(1, 1, 0, 1);
            foreach (var desc in _utilities!.GetActiveCharactersDescription()) // TODO: connect the pipe to AptContext
            {
                var wasSelected = (_lastSelectedCharacter == desc.Index);

                if (desc.Import is Import import)
                {
                    ImGui.TextColored(indexColor, "Imported");
                    ImGui.SameLine(0, 5);
                    ImGui.Text($"{import.Movie}.{import.Name}");
                }

                if (desc.ExportedName != null)
                {
                    ImGui.TextColored(exportColor, "Exported");
                    ImGui.SameLine(0, 5);
                    ImGui.Text(desc.ExportedName);
                }

                ImGui.TextColored(indexColor, desc.Index.ToString());
                ImGui.SameLine(0, 5);
                ImGui.TextColored(typeColor, desc.Type);
                ImGui.SameLine(0, 5);


                var selected = wasSelected;
                ImGui.Selectable(desc.Name, ref selected);
                if (selected)
                {
                    _lastSelectedCharacter = desc.Index;
                    var selectedCharacter = _utilities.GetCharacterByIndex(desc.Index);
                    if (manager.CurrentCharacter != selectedCharacter)
                    {
                        System.Console.WriteLine($"Setting new character {desc.Index} {selectedCharacter.GetType().Name}");
                        manager.SetCharacter(selectedCharacter);
                        _decidingExportName = desc.ExportedName ?? desc.Name;

                        // VM stuffs
                        if (selectedCharacter is Sprite sprite)
                        {
                            var si = sprite.InitActions;
                            if (si != null)
                                manager.CurrentActions = new LogicalInstructions(si);
                            else
                                manager.CurrentActions = null;
                        }
                        else
                        {
                            manager.CurrentActions = null;
                        }
                    }
                    DrawSelectedCharacterDescription(desc);
                }
                ImGui.Separator();
            }
        }

        private void DrawSelectedCharacterDescription(CharacterUtilities.Description desc)
        {
            _decidingExportName ??= string.Empty;
            if (desc.ExportedName == null)
            {
                if (ImGui.Button("Export"))
                {
                    _utilities!.ExportCharacter(desc.Index, _decidingExportName);
                }
            }
            else
            {
                if (ImGui.Button("Cancel export"))
                {
                    _utilities!.CancelExport(desc.Index);
                }
            }
            ImGui.SameLine();
            ImGui.InputText("Exported name", ref _decidingExportName, 64);

            if (desc.Import is not null)
            {
                // imported characters should not be modified
                return;
            }

            if (desc.ShapeGeometry is int geometry)
            {
                ImGui.Text($"Bounds: {desc.ShapeBounds}");
                ImGui.Text($"Geometry: {geometry}");
            }
        }

        private void CleanCreationData()
        {
            _newCharacterName = null;
            _isCreatingNewShape = true;
        }
    }
}
