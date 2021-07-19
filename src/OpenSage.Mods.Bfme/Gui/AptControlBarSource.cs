using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.Gui;
using OpenSage.Gui.Apt;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Library;
using OpenSage.Gui.Apt.ActionScript.Opcodes;
using OpenSage.Gui.ControlBar;
using OpenSage.Logic;
using OpenSage.Logic.Object;
using OpenSage.Mathematics;
using OpenSage.Mods.Bfme.Gui;
using SixLabors.Fonts;
using Veldrid;
using Geometry = OpenSage.Data.Apt.Geometry;
using Rectangle = OpenSage.Mathematics.Rectangle;
using ValueType = OpenSage.Gui.Apt.ActionScript.ValueType;

namespace OpenSage.Mods.Bfme
{
    class AptControlBar : IControlBar
    {
        Game _game;
        AptWindow _window;
        SpriteItem _root;
        private bool _commandbarVisible = false;
        private bool _palantirInitialized = false;
        private bool _minimapInitialized = false;
        private GameObject _selectedUnit;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly Font _font;
        private readonly int _fontSize = 11;
        private readonly ColorRgbaF _fontColor;

        public AptControlBar(Game game)
        {
            _game = game;

            _fontColor = new ColorRgbaF(0, 0, 0, 1); // _game.AssetStore.InGameUI.Current.DrawableCaptionColor.ToColorRgbaF(); -> this is white -> conflicts with the progress clock
            _fontSize = _game.AssetStore.InGameUI.Current.DrawableCaptionPointSize;
            var fontWeight = _game.AssetStore.InGameUI.Current.DrawableCaptionBold ? FontWeight.Bold : FontWeight.Normal;
            _font = _game.ContentManager.FontManager.GetOrCreateFont(_game.AssetStore.InGameUI.Current.DrawableCaptionFont, _fontSize, fontWeight);
        }

        public void AddToScene(Scene2D scene2D)
        {
            AptPalantir.Reset();

            _window = _game.LoadAptWindow("Palantir.apt");
            _root = _window.Root;

            _game.Scene2D.AptWindowManager.PushWindow(_window);
        }

        private void SetMinimap()
        {
            if (!_minimapInitialized)
            {
                var radar = _root.ScriptObject.GetMember("Radar").ToObject<StageObject>();
                var radarClipValue = radar.GetMember("RadarClip");

                if (radarClipValue.Type == ValueType.Undefined)
                {
                    return;
                }

                // This shape is used to render the minimap
                var radarClip = radarClipValue.ToObject<StageObject>().Item as SpriteItem;
                var shape = radarClip.Content.Items[1] as RenderItem;
                shape.RenderCallback = (AptRenderingContext renderContext, Geometry geom, Texture orig) =>
                {
                    var rect = new Rectangle(renderContext.GetBoundingBox(geom));
                    _game.Scene3D.Radar.DrawRadarMinimap(renderContext.GetActiveDrawingContext(), rect, true);
                };

                // This shape is used to render the overlay
                var radarChild = ((SpriteItem) radar.Item).Content.Items[1] as SpriteItem;
                shape = radarChild.Content.Items[1] as RenderItem;
                shape.RenderCallback = (AptRenderingContext renderContext, Geometry geom, Texture orig) =>
                {
                    var rect = new Rectangle(renderContext.GetBoundingBox(geom));
                    _game.Scene3D.Radar.DrawRadarOverlay(renderContext.GetActiveDrawingContext(), rect);
                };

                _minimapInitialized = true;
            }
        }

        private void InitializePalantir(Player player)
        {
            if (!_palantirInitialized)
            {
                Logger.Info("Initialize Palantir!");

                var showCommandInterface = _root.ScriptObject.GetMember("SetPalantirFrameState");
                if (showCommandInterface.Type != ValueType.Undefined)
                {
                    var good = Array.Exists(player.Template.IntrinsicSciences, s => s.Value.Name == "SCIENCE_GOOD");
                    var emptyArgs = new List<Value>();
                    emptyArgs.Add(Value.FromString(good ? "_good" : "_evil"));
                    FunctionCommon.ExecuteFunction(showCommandInterface, emptyArgs.ToArray(), _root.ScriptObject, _window.Context.Avm);
                    _palantirInitialized = true;
                }
            }
        }

        private void ClearCommandbuttons()
        {
            var aptCommandButtons = _root.ScriptObject.GetMember("CommandButtons").ToObject();
            // TODO what does this mean?
            /*
            if (aptCommandButtons.Constants.Count == 0)
            {
                return;
            }
            */
            throw new NotImplementedException();

            for (var i = 1; i <= 6; i++)
            {
                // we do not know how bfme handles this yet
                if (_game.SageGame == SageGame.Bfme) continue;

                var commandButton = aptCommandButtons.GetMember((i - 1).ToString()).ToObject<StageObject>();
                var placeHolder = commandButton.GetMember("placeholder").ToObject<StageObject>();
                placeHolder.Item.Visible = false;

                var shape = (placeHolder.Item as SpriteItem).Content.Items[1] as RenderItem;
                shape.RenderCallback = null;
            }
        }

        private void UpdateCommandbuttons()
        {
            if (_game.Scene3D.LocalPlayer.SelectedUnits.Count == 0)
            {
                return;
            }

            var selectedUnit = _game.Scene3D.LocalPlayer.SelectedUnits.First();
            if (selectedUnit == null)
            {
                return;
            }
            if (selectedUnit.Definition.CommandSet == null || selectedUnit.Definition.CommandSet.Value == null)
            {
                return;
            }

            var isProducing = selectedUnit.ProductionUpdate?.IsProducing ?? false;
            var commandSet = selectedUnit.Definition.CommandSet.Value;

            var aptCommandButtons = _root.ScriptObject.GetMember("CommandButtons").ToObject();
            for (var i = 1; i <= 6; i++)
            {
                // we do not know how bfme handles this yet
                if (_game.SageGame == SageGame.Bfme) continue;

                var commandButton = aptCommandButtons.GetMember((i - 1).ToString()).ToObject<StageObject>();
                var placeHolder = commandButton.GetMember("placeholder").ToObject<StageObject>();
                placeHolder.Item.Visible = false;

                if (!commandSet.Buttons.ContainsKey(i))
                {
                    continue;
                }
                var button = commandSet.Buttons[i].Value;
                if (!button.InPalantir || button.Command == CommandType.Revive)
                {
                    continue;
                }

                var createContent = commandButton.GetMember("CreateContent");
                var args = new List<Value>
                {
                    Value.FromString("bttn"),
                    Value.FromString("CommandButton")
                };

                //TODO: fix so this works
                FunctionCommon.ExecuteFunction(createContent, args.ToArray(), commandButton.Item.ScriptObject, _window.Context.Avm);

                placeHolder.Item.Visible = true;
                var shape = (placeHolder.Item as SpriteItem).Content.Items[1] as RenderItem;

                var (count, progress) = isProducing ? selectedUnit.ProductionUpdate.GetCountAndProgress(button) : (0, 0.0f);

                var texture = button.ButtonImage.Value;
                shape.RenderCallback = (AptRenderingContext renderContext, Geometry geom, Texture orig) =>
                {
                    var enabled = selectedUnit.CanPurchase(button);
                    var rect = new Rectangle(renderContext.GetBoundingBox(geom)).ToRectangleF();
                    renderContext.GetActiveDrawingContext().DrawMappedImage(texture, rect, grayscaled: !enabled);

                    if (count > 0)
                    {
                        renderContext.GetActiveDrawingContext().FillRectangleRadial360(
                                        new Rectangle(rect),
                                        new ColorRgbaF(1.0f, 1.0f, 1.0f, 0.6f),
                                        progress);

                        if (count > 1)
                        {
                            var textRect = new Rectangle(RectangleF.Transform(rect, Matrix3x2.CreateTranslation(new Vector2(0, rect.Width / 4))));
                            renderContext.GetActiveDrawingContext().DrawText(count.ToString(), _font, TextAlignment.Center, _fontColor, textRect);
                        }
                    }
                };
            }
        }


        private void ApplyPortrait(GameObject unit, StageObject frame)
        {
            if (frame == null || ((SpriteItem) frame.Item).Content.Items.Count == 0)
            {
                return;
            }

            var shape = ((SpriteItem) frame.Item).Content.Items[1] as RenderItem;

            if (shape.RenderCallback != null && _selectedUnit == unit)
            {
                return;
            }

            if (unit != null && unit.Definition.SelectPortrait != null)
            {
                //var shape = frameSprite.Content.Items[1] as RenderItem;
                shape.RenderCallback = (AptRenderingContext renderContext, Geometry geom, Texture orig) =>
                {
                    renderContext.RenderGeometry(geom, orig);

                    var destRect = RectangleF.Transform(geom.BoundingBox, renderContext.GetCurrentTransformMatrix());
                    if (unit.Definition.SelectPortrait != null && unit.Definition.SelectPortrait.Value != null)
                    {
                        var selectPortrait = unit.Definition.SelectPortrait.Value;
                        var sourceRect = selectPortrait.Coords;

                        renderContext.GetActiveDrawingContext().DrawImage(selectPortrait.Texture.Value, sourceRect, destRect);
                    }
                };
            }
            else
            {
                shape.RenderCallback = null;
            }
        }

        private void UpdatePalantir(Player player)
        {
            var palantirFrame = _root.ScriptObject.GetMember("CommandBackground").ToObject<StageObject>();

            if (player.SelectedUnits.Count > 0)
            {
                // TODO: Handle multiple selection.
                var unit = player.SelectedUnits.First();

                // TODO: Only do this when command set changes.
                ApplyPortrait(unit, palantirFrame);

                _selectedUnit = unit;
            }
            else
            {
                ApplyPortrait(null, palantirFrame);
            }
        }

        private void UpdateSideCommandbar(Player player)
        {
            var sideCommandBar = _root.ScriptObject.GetMember("SideCommandBar").ToObject<StageObject>();

            if (player.SelectedUnits.Count > 0)
            {
                if (!_commandbarVisible)
                {
                    var fadeIn = sideCommandBar.Item.ScriptObject.GetMember("FadeIn");

                    if (fadeIn.Type != ValueType.Undefined)
                    {
                        var emptyArgs = new List<Value>();
                        FunctionCommon.ExecuteFunction(fadeIn, emptyArgs.ToArray(), sideCommandBar.Item.ScriptObject, _window.Context.Avm);
                        _commandbarVisible = true;
                    }
                }
            }
            else if (player.SelectedUnits.Count == 0 && _commandbarVisible)
            {
                var fadeOut = sideCommandBar.Item.ScriptObject.GetMember("FadeOut");

                if (fadeOut.Type != ValueType.Undefined)
                {
                    var emptyArgs = new List<Value>();
                    FunctionCommon.ExecuteFunction(fadeOut, emptyArgs.ToArray(), sideCommandBar.Item.ScriptObject, _window.Context.Avm);
                    _commandbarVisible = true;
                }

                _commandbarVisible = false;
            }
        }

        public void Update(Player player)
        {
            if (AptPalantir.Initialized)
            {
                InitializePalantir(player);
                SetMinimap();
                if (AptPalantir.SideButtonsInitialized == 12)
                {
                    UpdateSideCommandbar(player);
                }

                if (player.SelectedUnits.Count > 0)
                {
                    UpdateCommandbuttons();
                }
                else
                {
                    ClearCommandbuttons();
                }

                UpdatePalantir(player);
            }
        }
    }

    public class AptControlBarSource : IControlBarSource
    {
        public IControlBar Create(string side, Game game)
        {
            return new AptControlBar(game);
        }
    }
}
