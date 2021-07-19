using ImGuiNET;
using OpenSage.Tools.AptEditor.Apt.Editor;
using OpenSage.Tools.AptEditor.Util;
using OpenSage.Data.Apt;

namespace OpenSage.Tools.AptEditor.UI.Widgets
{
    internal class ConstantPool : IWidget
    {
        ConstantData _data = null;
        InputComboBox _editBox = new AutoSuggestionBox
        {
            Suggestions = InstructionUtility.InstructionNames
        };
        int _editingIndex = -1;

        public void Draw(AptSceneManager manager)
        {
            _data = manager.AptManager?.AptFile.Constants;

            if (_data == null) return;

            if (ImGui.Begin("Constant Pool"))
            {

                for (var i = 0; i < _data.Entries.Count; ++i)
                {
                    if (i == _editingIndex)
                    {
                        _editBox.Draw();
                        ImGui.SameLine();
                        if (ImGui.Button("Done"))
                        {
                            _editingIndex = -1;
                        }
                    }
                    else
                    {
                        var s = string.Format("#{0}: {1} {2}", i, _data.Entries[i].Type.ToString(), _data.Entries[i].Value.ToString());
                        ImGui.Button(s);
                    }
                }
            }
            ImGui.End();
        }
    }
}
