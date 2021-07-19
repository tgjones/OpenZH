using ImGuiNET;
using OpenSage.Tools.AptEditor.Apt.Editor;
using OpenSage.Tools.AptEditor.Util;
using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Tools.AptEditor.UI.Widgets
{
    internal class VMConsole : IWidget
    {
        LogicalInstructions _instructions = null;
        AptContext _context = null;
        ActionContext _acontext = null;
        InputComboBox _editBox = new AutoSuggestionBox
        {
            Suggestions = InstructionUtility.InstructionNames
        };
        int _editingIndex = -1;

        //public VMConsole(LogicalInstructions instructions)
        //{

        //}

        string input_type = "Number";
        string input_val = "114514";
        int int_input = 0;
        string exec_code_ref = "";
        string last_executed_func = "Initial output";

        public void CheckEnv(AptSceneManager manager)
        {
            if (manager.CurrentActions == null || manager.CurrentDisplay is null)
            {
                _context = null;
                _acontext = null;
            }
            else if (_instructions != manager.CurrentActions)
            {
                _instructions = manager.CurrentActions;
                ObjectContext ocon = manager.CurrentDisplay.ScriptObject;

                _context = manager.CurrentDisplay.Context;
                _acontext = new ActionContext(_context.Avm.GlobalObject, ocon, _context.Avm.CurrentContext(), 4)
                {
                    Apt = _context,
                    Stream = new InstructionStream(_instructions.Insts),
                };
                _context.Avm.PushContext(_acontext);
            }
            
        }

        public void Draw(AptSceneManager manager)
        {
            CheckEnv(manager);

            if (ImGui.Begin("VM Console"))
            {
                
                ImGui.InputInt("Line", ref int_input);
                //ImGui.SameLine();
                if (ImGui.Button("Goto"))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button("Exec"))
                {
                    if (_context != null)
                    {
                        var lef = _context.Avm.ExecuteOnce(); // _acontext);
                        last_executed_func = lef.ToString(_acontext);
                    }
                    else
                    {
                        last_executed_func = "Action not loaded";
                    }
                    
                }
                ImGui.SameLine();
                if (ImGui.Button("Exec(Jump Code Blocks)"))
                {

                }
              

                
                ImGui.InputText("Type", ref input_type, 1000);
                //ImGui.SameLine();
                ImGui.InputText("Value", ref input_val, 1000);
                //ImGui.SameLine();
                if (ImGui.Button("Push"))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button("Pop"))
                {

                }
                ImGui.SameLine();
                if (ImGui.Button("Pop & Show"))
                {

                }

                
                ImGui.InputTextMultiline("Code", ref exec_code_ref, 114514, new System.Numerics.Vector2(200, 200));
                if (ImGui.Button("Exec Code")) {
                    throw new System.NotImplementedException(exec_code_ref);
                }

                ImGui.Separator();

                ImGui.Text("Registers:");
                ImGui.Text(_acontext == null ? "Not Applicable" : _acontext.DumpRegister());

                ImGui.Text("Stack:");
                ImGui.Text(_acontext == null ? "Not Applicable" : _acontext.DumpStack());

                ImGui.Text("Last Execution:");
                ImGui.Text(last_executed_func);

                ImGui.Text("KW Global:");
                ImGui.Text(_acontext == null ? "null" : _acontext.Global.ToStringDisp(_acontext));

                ImGui.Text("KW This:");
                ImGui.Text(_acontext == null ? "null" : _acontext.This.ToStringDisp(_acontext));

                if (_instructions != null && ImGui.Begin("Codes"))
                { 
                    for (var i = 0; i < _instructions.Items.Count; ++i)
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
                            ImGui.Button(_instructions.Items[i].ToString(_acontext));
                        }
                    }
                }
                ImGui.End();
            }
            ImGui.End();
        }
    }
}
