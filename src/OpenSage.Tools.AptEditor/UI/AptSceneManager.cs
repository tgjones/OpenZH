using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using OpenSage.Data.Apt;
using OpenSage.Data.Apt.Characters;
using OpenSage.Gui.Apt;
using OpenSage.Mathematics;
using OpenSage.Tools.AptEditor.Apt;
using OpenSage.Tools.AptEditor.Apt.Editor;

namespace OpenSage.Tools.AptEditor.UI
{
    internal class AptLoadFailure : Exception
    {
        public string? File { get; }
        public AptLoadFailure(string? file) : base(file is null ? "Apt Load failed" : $"Cannot load {file}")
        {
            File = file;
        }
    }

    internal class AptSceneManager
    {
        public int MillisecondsPerFrame => (int) (AptManager?.AptFile.Movie.MillisecondsPerFrame ?? 30);
        public Character? CurrentCharacter { get; private set; }
        public LogicalInstructions? CurrentActions { get; set; }
        public bool IsCurrentCharacterImported => CurrentCharacter?.Container != AptManager?.AptFile;
        public string? CurrentAptPath { get; private set; }
        public int NumberOfFrames { get; private set; }
        public int? CurrentFrameWrapped => NumberOfFrames == 0 ? new int?() : CurrentFrame % NumberOfFrames;
        public int CurrentFrame { get; private set; }
        public Vector2 CurrentOffset
        {
            get => CurrentWindow?.WindowTransform.GeometryTranslation ?? Vector2.Zero;
            set => SetTransform((ref ItemTransform t) => t.GeometryTranslation = value);
        }
        public float CurrentScale
        {
            get => CurrentWindow?.WindowTransform.GeometryRotation.M11 ?? 1;
            set => SetTransform((ref ItemTransform t) => t.GeometryRotation = Matrix3x2.CreateScale(value));
        }
        public ColorRgbaF DisplayBackgroundColor { get; private set; }
        public Game Game { get; }
        public AptEditManager? AptManager { get; private set; }
        public AptWindow? CurrentWindow { get; private set; }
        public WrappedDisplayItem CurrentDisplay { get; private set; }

        public AptSceneManager(Game game)
        {
            UnloadApt();
            Game = game;
        }

        public void UnloadApt()
        {
            CurrentAptPath = null;
            NumberOfFrames = 0;
            CurrentFrame = 0;
            CurrentOffset = Vector2.Zero;
            CurrentScale = 1;
            AptManager = null;
            CurrentWindow = null;
            CurrentDisplay = null;
            CurrentActions = null;
        }

        public void LoadApt(string path)
        {
            UnloadApt();
            var entry = Game.ContentManager.FileSystem.GetFile(path);
            if (entry == null)
            {
                throw new AptLoadFailure(path);
            }

            AptFile aptFile;
            try
            {
                aptFile = AptFile.FromFileSystemEntry(entry);
            }
            catch (FileNotFoundException fileNotFound)
            {
                throw new AptLoadFailure(fileNotFound.FileName);
            }

            CurrentAptPath = path;
            AptManager = new AptEditManager(aptFile);
        }

        public void LoadApt(AptFile aptFile, string? name = null)
        {
            UnloadApt();
            
            CurrentAptPath = name ?? aptFile.MovieName;
            AptManager = new AptEditManager(aptFile);
        }

        public void ChangeDisplayBackgroundColor(ColorRgbaF newColor)
        {
            if (DisplayBackgroundColor == newColor)
            {
                return;
            }

            DisplayBackgroundColor = newColor;
            ResetAptWindow();
        }

        public void ResetAptWindow()
        {
            CurrentWindow = null;
            CurrentDisplay = null;
            // CurrentActions = null;

            var windows = Game.Scene2D.AptWindowManager;
            while (windows.OpenWindowCount > 0)
            {
                windows.PopWindow();
            }

            var multiplied = DisplayBackgroundColor.ToVector4() * 255;
            var color = new ColorRgba((byte) multiplied.X, (byte) multiplied.Y, (byte) multiplied.Z, (byte) multiplied.W);
            windows.PushWindow(AptEditorBackgroundSource.CreateBackgroundAptWindow(Game, color));
            if (AptManager != null)
            {
                CurrentWindow = new AptWindow(Game, Game.ContentManager, AptManager.AptFile);
                CurrentWindow.Context.LoadContext();
                var root = CurrentWindow.Root;
                // hack: an AptWindow will always be updated by the game when it's loaded
                // because AptWindow.Root.IsNewFrame() will return true if the private member
                // _lastUpdate.TotalTime is zero.
                // By manually setting it to a non-zero value, and by stopping the sripte,
                // it won't be updated by the game anymore
                var field = root.GetType().GetField("_lastUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                field!.SetValue(root, new TimeInterval(1, 0));
                CurrentWindow.Root.Stop();

                // set display list
                var list = CurrentWindow.Root.Content;
                while (list.Items.Any())
                {
                    list.RemoveItem(list.Items.Keys.First());
                }
                if (CurrentCharacter != null)
                {
                    CurrentDisplay = new WrappedDisplayItem(CurrentCharacter, CurrentWindow.Context, CurrentWindow.Root);
                    list.AddItem(default, CurrentDisplay);
                }

                windows.PushWindow(CurrentWindow);
                if (CurrentDisplay is not null)
                    CurrentDisplay.PlayToFrameNoActions(0);
            }
        }

        public void SetCharacter(Character character)
        {
            if (AptManager is null)
            {
                throw new InvalidOperationException();
            }

            CurrentCharacter = character;
            NumberOfFrames = (CurrentCharacter as Playable)?.Frames.Count ?? 0;
            CurrentScale = 1f;
            CurrentOffset = new Vector2(200, 200);
            ResetAptWindow();
            if (NumberOfFrames > 0)
            {
                PlayToFrame(0);
            }
        }

        public void PlayToFrame(int frame)
        {
            if (CurrentDisplay is null)
            {
                throw new InvalidOperationException();
            }

            CurrentFrame = frame;
            CurrentDisplay.PlayToFrameNoActions(CurrentFrame);
        }

        public void NextFrame()
        {
            if (CurrentDisplay is null)
            {
                throw new InvalidOperationException();
            }

            ++CurrentFrame;
            CurrentDisplay.UpdateNextFrameNoActions();
        }

        private delegate void TransformAction(ref ItemTransform t);
        private void SetTransform(TransformAction action)
        {
            if (CurrentWindow == null)
            {
                return;
            }

            action(ref CurrentWindow.WindowTransform);
        }
    }
}
