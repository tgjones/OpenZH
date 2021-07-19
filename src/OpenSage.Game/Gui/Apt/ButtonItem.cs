using System.Collections.Generic;
using System.Numerics;
using OpenSage.Data.Apt.Characters;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Mathematics;

namespace OpenSage.Gui.Apt
{
    public class ButtonItem : TexturedItem
    {
        private bool _isHovered = false;
        private bool _isDown = false;
        private ItemTransform _curTransform;
        private List<InstructionCollection> _actionList;

        public override void Create(Character chararacter, AptContext context, SpriteItem parent = null)
        {
            Character = chararacter;
            Context = context;
            Parent = parent;
            Name = "";
            Visible = true;

            var button = Character as Button;

            _actionList = new List<InstructionCollection>();
        }


        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public override bool HandleInput(Point2D mousePos, bool mouseDown)
        {
            var button = Character as Button;

            var transform = _curTransform.GeometryRotation;
            transform.Translation = _curTransform.GeometryTranslation;// * scaling;
            ApplyCurrentRecord(ref transform);

            var verts = button.Vertices;
            var mouse = new Point2D(mousePos.X, mousePos.Y);

            foreach (var tri in button.Triangles)
            {
                var v1 = Vector2.Transform(verts[tri.IDX0], transform);
                var v2 = Vector2.Transform(verts[tri.IDX1], transform);
                var v3 = Vector2.Transform(verts[tri.IDX2], transform);

                if (TriangleUtility.IsPointInside(v1, v2, v3, mouse))
                {
                    if (!_isHovered)
                    {
                        logger.Debug("Hit: " + mousePos.X + "-" + mousePos.Y);
                        var idx = button.Actions.FindIndex(ba => ba.Flags.HasFlag(ButtonActionFlags.IdleToOverUp));
                        if (idx != -1)
                        {
                            _actionList.Add(button.Actions[idx].Instructions);
                        }
                        _isHovered = true;
                    }

                    if (_isHovered && mouseDown && !_isDown)
                    {
                        logger.Debug("Down: " + mousePos.X + "-" + mousePos.Y);
                        var idx = button.Actions.FindIndex(ba => ba.Flags.HasFlag(ButtonActionFlags.OverUpToOverDown));
                        if (idx != -1)
                        {
                            _actionList.Add(button.Actions[idx].Instructions);
                        }
                        _isDown = true;
                    }

                    if (_isHovered && !mouseDown && _isDown)
                    {
                        logger.Debug("Up: " + mousePos.X + "-" + mousePos.Y);
                        var idx = button.Actions.FindIndex(ba => ba.Flags.HasFlag(ButtonActionFlags.OverDownToOverUp));
                        if (idx != -1)
                        {
                            _actionList.Add(button.Actions[idx].Instructions);
                        }
                        _isDown = false;
                    }

                    return true;
                }
            }

            if (_isHovered)
            {
                var idx = button.Actions.FindIndex(ba => ba.Flags.HasFlag(ButtonActionFlags.OverUpToIdle));
                if (idx != -1)
                {
                    _actionList.Add(button.Actions[idx].Instructions);
                }
                _isHovered = false;
                logger.Debug("Unhovered: " + mousePos.X + "-" + mousePos.Y);
            }
            return false;
        }

        private void ApplyCurrentRecord(ref Matrix3x2 t)
        {
            var button = Character as Button;
            var idx = button.Records.FindIndex(br => br.Flags.HasFlag(ButtonRecordFlags.StateHit));
            if (idx != -1)
            {
                var br = button.Records[idx];

                var a = new Matrix3x2(t.M11, t.M12, t.M21, t.M22, t.M31, t.M32);
                var b = new Matrix3x2(br.RotScale.M11, br.RotScale.M12, br.RotScale.M21, br.RotScale.M22, 0, 0);
                var c = Matrix3x2.Multiply(a, b);

                t.M11 = c.M11;
                t.M12 = c.M12;
                t.M21 = c.M21;
                t.M22 = c.M22;

                t.M31 += br.Translation.X;
                t.M32 += br.Translation.Y;
            }
        }

        protected override void RenderImpl(AptRenderingContext renderingContext)
        {
            _curTransform = renderingContext.CurrentTransform * Transform;

            var windowScaling = renderingContext.Window.GetScaling();
            _curTransform.GeometryTranslation *= windowScaling;
            _curTransform.GeometryRotation.M11 *= windowScaling.X;
            _curTransform.GeometryRotation.M22 *= windowScaling.Y;
        }

        public override void RunActions(TimeInterval gt)
        {
            foreach (var action in _actionList)
            {
                Context.Avm.Execute(action, Parent.ScriptObject, Parent.Context);
            }
            _actionList.Clear();
        }
    }
}
