﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenSage.Gui;
using OpenSage.Gui.ControlBar;
using OpenSage.Gui.UnitOverlay;
using OpenSage.Input;
using OpenSage.Logic;
using OpenSage.Logic.Object;
using OpenSage.Mathematics;

namespace OpenSage.Mods.Bfme.Gui
{
    public class RadialUnitOverlay : InputMessageHandler, IUnitOverlay
    {
        Game _game;

        private bool _visible;
        private Point2D _center;
        private CommandSet _commandSet;
        List<RadialButton> _buttons;
        private GameObject _selectedUnit;

        public override HandlingPriority Priority => HandlingPriority.UIPriority;

        public RadialUnitOverlay(Game game)
        {
            _game = game;
            _game.InputMessageBuffer.Handlers.Add(this);
            _buttons = new List<RadialButton>();
        }

        public void Update(Player player)
        {
            _visible = false;
            if (player.SelectedUnits.Count != 1)
            {
                _selectedUnit = null;
                return;
            }

            var selectedUnit = player.SelectedUnits.First();
            if (selectedUnit.BuildProgress < 0.999f
                || !selectedUnit.Definition.KindOf.Get(ObjectKinds.Structure)
                || selectedUnit.Definition.CommandSet == null)
            {
                _selectedUnit = null;
                return;
            }

            _visible = true;

            var screenPosition = _game.Scene3D.Camera.WorldToScreenPoint(selectedUnit.Collider.WorldBounds.Center);
            _center = new Point2D((int)screenPosition.X, (int)screenPosition.Y);
            _commandSet = selectedUnit.Definition.CommandSet.Value;

            if (_selectedUnit != selectedUnit && _commandSet != null)
            {
                //Update button list
                var playerTeamplate = selectedUnit.Owner.Template;
                var heroIndex = 0;

                var commandButtons = new List<CommandButton>();
                foreach (var button in _commandSet.Buttons.Values)
                {
                    if (button.Value.Command == CommandType.Revive)
                    {
                        if (heroIndex < playerTeamplate.BuildableHeroesMP.Count())
                        {
                            var heroDefinition = playerTeamplate.BuildableHeroesMP[heroIndex++];
                            if (selectedUnit.CanRecruitHero(heroDefinition.Value))
                            {
                                commandButtons.Add(new CommandButton(CommandType.UnitBuild, heroDefinition));
                            }
                        }
                    }
                    else
                    {
                        commandButtons.Add(button.Value);
                    }
                }

                _buttons.Clear();
                foreach (var commandButton in commandButtons)
                {
                    var radialButton = new RadialButton(_game, selectedUnit, commandButton);
                    _buttons.Add(radialButton);
                }

                _selectedUnit = selectedUnit;
            }

            var isProducing = selectedUnit.ProductionUpdate?.IsProducing ?? false;
            foreach (var radialButton in _buttons)
            {
                var (count, progress) = isProducing ? selectedUnit.ProductionUpdate.GetCountAndProgress(radialButton.CommandButton) : (0, 0.0f);
                radialButton.Update(progress, count, selectedUnit.CanEnqueue(radialButton.CommandButton));
            }
        }

        public void Render(DrawingContext2D drawingContext)
        {
            if (!_visible)
            {
                return;
            }

            var radialBorder = _game.GetMappedImage("RadialBorder");

            var radius = (-1 + MathF.Sqrt(_buttons.Count() + 0.75f)) * (radialBorder.Coords.Width * 0.9f);
            var deltaAngle = MathUtility.TwoPi / _buttons.Count();

            var i = 0;
            foreach (var button in _buttons)
            {
                var posX = _center.X + MathF.Sin(i * deltaAngle) * radius;
                var posY = _center.Y + -MathF.Cos(i * deltaAngle) * radius;

                button.Render(drawingContext, new Vector2(posX, posY));
                i++;
            }
        }

        public override InputMessageResult HandleMessage(InputMessage message)
        {
            if (_visible)
            {
                foreach (var button in _buttons)
                {
                    if (button.HandleMouseCursor(message))
                    {
                        return InputMessageResult.Handled;
                    }
                }
            }
            return InputMessageResult.NotHandled;
        }
    }

    public class RadialUnitOverlaySource : IUnitOverlaySource
    {
        public IUnitOverlay Create(Game game) => new RadialUnitOverlay(game);
    }
}
