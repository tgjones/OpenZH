using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using OpenSage.Data;
using OpenSage.Tools.AptEditor.UI;
using OpenSage.Tools.AptEditor.Util;
using Veldrid;
using Veldrid.StartupUtilities;

namespace OpenSage.Tools.AptEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            Platform.Start();

            var rootPath = Launcher();
            if (rootPath != null)
            {
                GameWindowAdapter(rootPath);
            }

            Platform.Stop();
        }

        static string? Launcher()
        {

            var createInfo = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = 400,
                WindowHeight = 150,
                WindowInitialState = WindowState.Normal,
                WindowTitle = "OpenSAGE Apt Editor"
            };
            VeldridStartup.CreateWindowAndGraphicsDevice(createInfo, out var window, out var outGraphicsDevice);
            using var graphicsDevice = outGraphicsDevice;
            graphicsDevice.SyncToVerticalBlank = true;
            using var commandList = graphicsDevice.ResourceFactory.CreateCommandList();
            using var imGuiRenderer = new ImGuiRenderer(graphicsDevice,
                                                        graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
                                                        window.Width,
                                                        window.Height);
            using var gameTimer = new DeltaTimer();
            string? rootPath = null;
            var rootPathInput = new ImGuiTextBox(1024);

            void OnWindowResized()
            {
                graphicsDevice.ResizeMainWindow((uint) window.Width, (uint) window.Height);
                imGuiRenderer.WindowResized(window.Width, window.Height);
            }
            window.Resized += OnWindowResized;
            try
            {
                gameTimer.Start();
                while (rootPath == null)
                {
                    if (!window.Exists)
                    {
                        return null;
                    }

                    gameTimer.Update();
                    var inputSnapshot = window.PumpEvents();
                    imGuiRenderer.Update((float) gameTimer.CurrentGameTime.DeltaTime.TotalSeconds, inputSnapshot);

                    commandList.Begin();
                    commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
                    commandList.ClearColorTarget(0, RgbaFloat.Clear);

                    ImGui.SetNextWindowPos(Vector2.Zero);
                    ImGui.SetNextWindowSize(new Vector2(window.Width, window.Height));
                    var open = true;
                    if (ImGui.Begin("Launcher", ref open, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize))
                    {
                        ImGui.TextWrapped("Start OpenSage Apt Editor by providing a root path.");
                        ImGui.TextWrapped("It's recommended that you set it to " +
                            "the RA3SDK_UI_ScreensPack folder so you could load apt files more easily.");

                        // TODO More fancy implementations?
                        var defaultInputPath = "G:\\Games\\RA#s\\aptuis\\aptui";
                        
                        rootPathInput.InputText("##rootPath", out var inputRootPath);
                        if (ImGui.Button("Load"))
                        {
                            rootPath = inputRootPath;
                            if (rootPath.Length < 1) rootPath = defaultInputPath;
                            window.Close();
                        }
                    }
                    ImGui.End();

                    imGuiRenderer.Render(graphicsDevice, commandList);
                    commandList.End();
                    graphicsDevice.SubmitCommands(commandList);
                    graphicsDevice.SwapBuffers();
                }
                return rootPath;
            }
            finally
            {
                window.Resized -= OnWindowResized;
            }
        }

        static void GameWindowAdapter(string rootPath)
        {
            var installation = new GameInstallation(new AptEditorDefinition(), rootPath);
            using var game = new Game(installation, null, new Configuration { LoadShellMap = false });
            // should be the ImGui context created by DeveloperModeView
            var initialContext = ImGui.GetCurrentContext();
            var device = game.GraphicsDevice;
            var window = game.Window;
            using var imGuiRenderer = new ImGuiRenderer(device,
                                                        game.Panel.OutputDescription,
                                                        window.ClientBounds.Width,
                                                        window.ClientBounds.Height);
            var font = imGuiRenderer.LoadSystemFont("consola.ttf");

            using var commandList = device.ResourceFactory.CreateCommandList();
            var ourContext = ImGui.GetCurrentContext();
            // reset ImGui Context to initial one
            ImGui.SetCurrentContext(initialContext);

            var mainForm = new MainForm(game);
            void OnClientSizeChanged(object? sender, EventArgs args)
            {
                imGuiRenderer.WindowResized(window.ClientBounds.Width, window.ClientBounds.Height);
            }
            void OnRendering2D(object? sender, EventArgs e)
            {
                var previousContext = ImGui.GetCurrentContext();
                ImGui.SetCurrentContext(ourContext);
                try
                {
                    commandList.Begin();
                    commandList.SetFramebuffer(game.Panel.Framebuffer);
                    imGuiRenderer.Update((float) game.RenderTime.DeltaTime.TotalSeconds, window.CurrentInputSnapshot);
                    using (var fontSetter = new ImGuiFontSetter(font))
                    {
                        mainForm.Draw();
                    }
                    imGuiRenderer.Render(game.GraphicsDevice, commandList);
                    commandList.End();
                    device.SubmitCommands(commandList);
                }
                finally
                {
                    ImGui.SetCurrentContext(previousContext);
                }
            }
            window.ClientSizeChanged += OnClientSizeChanged;
            game.RenderCompleted += OnRendering2D;
            try
            {
                game.ShowMainMenu();
                game.Run();
            }
            finally
            {
                game.RenderCompleted -= OnRendering2D;
                window.ClientSizeChanged -= OnClientSizeChanged;
            }
        }
    }
}
