using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using OpenSage.Data;
using OpenSage.Tools.AptEditor.Apt;
using OpenSage.Tools.AptEditor.Apt.Writer;
using OpenSage.Tools.AptEditor.UI.Widgets;
using OpenSage.Tools.AptEditor.Util;

namespace OpenSage.Tools.AptEditor.UI
{
    internal sealed class MainForm
    {
        private readonly List<IWidget> _widgets = new List<IWidget>
        {
            new CharacterList(),
            new SceneTransform(),
            new FrameList(),
            new FrameItemList(),
            new GeometryEditor(),
            new ConstantPool(),
            new VMConsole(), 
        };
        private readonly ExportPathSelector _exportPathSelector = new ExportPathSelector();
        private readonly AptFileSelector _aptFileSelector;
        private readonly SearchPathAdder _searchPathAdder;
        private readonly FileListWindow _fileListWindow;
        private readonly List<ImGuiModalPopUp> _popups;
        private readonly GameWindow _window;
        private readonly AptSceneManager _manager;

        private readonly List<(Task, string)> _tasks = new List<(Task, string)>();
        private string? _loadAptError;
        private string? _lastSeriousError;

        public MainForm(Game game)
        {
            _window = game.Window;
            _manager = new AptSceneManager(game);
            _aptFileSelector = new AptFileSelector(game.ContentManager.FileSystem);
            _searchPathAdder = new SearchPathAdder(game);
            _fileListWindow = new FileListWindow(game, _aptFileSelector);
            _fileListWindow.Visible = true;
            _popups = new List<ImGuiModalPopUp>
            {
                new ImGuiModalPopUp("Critical Error",
                                    () => _lastSeriousError is string,
                                    DrawCriticalErrorPrompt,
                                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar),
                new ImGuiModalPopUp("Failed to load Apt file", () => _loadAptError is string, DrawErrorPrompt),
                new ImGuiModalPopUp("Please wait...", () => _tasks.Any(), DrawLoadingPrompt)
            };
        }

        public void Draw()
        {
            ImGui.SetNextWindowPos(Vector2.Zero, ImGuiCond.Always, Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(_window.ClientBounds.Width, 0), ImGuiCond.Always);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            var open = false;
            ImGui.Begin("OpenSAGE Apt Editor", ref open, ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar);

            foreach (var popup in _popups)
            {
                if (popup.Update())
                {
                    break;
                }
            }

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Open...", "Ctrl+O", false, true))
                    {
                        _aptFileSelector.Visible = true;
                    }

                    if (ImGui.MenuItem("Open example...", null, false, true))
                    {
                        var name = "feg_m_mainmenu3d";
                        _manager.LoadApt(SampleApt.Create(name, new Mathematics.ColorRgba(0, 255, 0, 255)), name);
                    }

                    if (ImGui.MenuItem("Export", null, false, _manager.AptManager != null))
                    {
                        _exportPathSelector.Visible = true;
                    }

                    if (ImGui.MenuItem("Close", null, false, _manager.AptManager != null))
                    {
                        _manager.UnloadApt();
                    }

                    if (ImGui.MenuItem("Add Search paths..."))
                    {
                        _searchPathAdder.Visible = true;
                    }

                    if (ImGui.MenuItem("File List Window"))
                    {
                        _fileListWindow.Visible = true;
                    }

                    ImGui.Separator();

                    if (ImGui.MenuItem("Exit", "Alt+F4", false, true))
                    {
                        _window.Close();
                    }

                    ImGui.EndMenu();
                }
                if (_manager.AptManager is AptEditManager manager && ImGui.BeginMenu("Edit"))
                {
                    var description = manager.GetUndoDescription();
                    if (ImGui.MenuItem(description ?? "Undo", description is not null))
                    {
                        manager.Undo();
                        RefreshDisplay(description);
                    }
                    description = manager.GetRedoDescription();
                    if (ImGui.MenuItem(description ?? "Redo", description is not null))
                    {
                        manager.Redo();
                        RefreshDisplay(description);
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMenuBar();
            }

            if (_exportPathSelector.GetValue() is string exportPath)
            {
                if (_manager.AptManager != null)
                {
                    var dump = _manager.AptManager.GetAptDataDump();
                    var task = _manager.AptManager.GetAptDataDump().WriteTo(new DirectoryInfo(exportPath));
                    _tasks.Add((task, $"Exporting apt to {exportPath}"));
                }
            }

            if (_aptFileSelector.GetValue() is string inputAptPath)
            {
                try
                {
                    string? lastFailed = null;
                    while (true)
                    {
                        try
                        {
                            _searchPathAdder.AutoLoad(inputAptPath, loadArtOnly: true); // here it's used to prepare art folder
                            _manager.LoadApt(inputAptPath);
                        }
                        catch (AptLoadFailure loadFailure)
                        {
                            if (loadFailure.File is string file)
                            {
                                if (file != lastFailed)
                                {
                                    lastFailed = file;
                                    if (_searchPathAdder.AutoLoad(file, loadArtOnly: false))
                                    {
                                        continue;
                                    }
                                }

                                if (Path.GetDirectoryName(file) is string directory)
                                {
                                    _searchPathAdder.MappedPath = directory;
                                }
                            }
                            _loadAptError =
                                $"Failed to open apt file {loadFailure.File ?? "?"} - {loadFailure.Message}.\n" +
                                "Consider adding more search paths (File Menu > Add Search Path).";
                            _searchPathAdder.Visible = true;
                            _searchPathAdder.Next = () => _aptFileSelector.Visible = true;
                        }
                        break;
                    }
                }
                catch (Exception unhandleled) when (!Debugger.IsAttached)
                {
                    _lastSeriousError = unhandleled.Message + '\n' + unhandleled.StackTrace;
                }
            }

            if (_manager.AptManager == null)
            {
                ImGui.Text("Open a .apt file to see its contents.");
            }
            else
            {
                ImGui.Text($"Currently loaded Apt file: {_manager.CurrentAptPath}");
            }

            ImGui.SameLine(ImGui.GetWindowWidth() - 100);
            ImGui.Text($"FPS: {ImGui.GetIO().Framerate,5:N2}");

            ImGui.End();

            try
            {
                _exportPathSelector.Draw();
                _aptFileSelector.Draw();
                _searchPathAdder.Draw();
                _fileListWindow.Draw();
                if (_lastSeriousError == null && _loadAptError == null && _manager.AptManager != null)
                {
                    foreach (var widget in _widgets)
                    {
                        widget.Draw(_manager);
                    }
                }
            }
            catch (Exception unhandleled) when (!Debugger.IsAttached)
            {
                _lastSeriousError = unhandleled.Message + '\n' + unhandleled.StackTrace;
            }

            ImGui.PopStyleVar();
        }

        private void RefreshDisplay(string? description)
        {
            if(description != null)
            {
                if(description.Contains("Edit Geometry"))
                {
                    //refresh GeometryEditor when undo/redo
                    _widgets.OfType<GeometryEditor>().First().RefreshInput();
                }
            }

        }

        private void DrawLoadingPrompt()
        {
            _tasks.RemoveAll(((Task Task, string _) t) =>
            {
                if (!t.Task.IsCompleted)
                {
                    return false;
                }
                t.Task.Dispose();
                return true;
            });
            if (!_tasks.Any())
            {
                ImGui.CloseCurrentPopup();
            }
            foreach (var (_, description) in _tasks)
            {
                ImGui.Text(description);
            }

        }

        private void DrawErrorPrompt()
        {
            ImGui.TextColored(new Vector4(1, 0.2f, 0.2f, 1), _loadAptError);

            if (ImGui.Button("Ok"))
            {
                _loadAptError = null;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SetItemDefaultFocus();
        }

        private void DrawCriticalErrorPrompt()
        {
            ImGui.TextColored(new Vector4(1, 0.2f, 0.2f, 1), "Unhandleled exception:");
            ImGui.Text(_lastSeriousError);
            ImGui.TextColored(new Vector4(1, 0.2f, 0.2f, 1), "Press Ok to exit");

            if (ImGui.Button("Ok"))
            {
                _lastSeriousError = null;
                ImGui.CloseCurrentPopup();
                _window.Close();
            }

            ImGui.SetItemDefaultFocus();
        }
    }
}
