using OpenSage.Content.Translation;
using OpenSage.Data.Apt;
using OpenSage.Data.Apt.Characters;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Library;
using Veldrid;

namespace OpenSage.Gui.Apt
{
    public sealed class RenderItem : TexturedItem
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private TimeInterval _lastUpdate;

        public LocalizedString TextValue { get; private set; }
        private bool IsHovered { get; set; }

        public delegate void CustomRenderCallback(AptRenderingContext context, Geometry geometry, Texture originalTexture);
        public CustomRenderCallback RenderCallback;

        public override void Create(Character character, AptContext context, SpriteItem parent = null)
        {
            Character = character;
            Context = context;
            Parent = parent;
            ScriptObject = new StageObject(this);
            Name = "";
            Visible = true;
            TextValue = character is Text text ? LocalizedString.CreateApt(text.Content) : null;
            IsHovered = false;
        }

        public override void Update(TimeInterval gt)
        {
            // Currently only Text needs to be updated
            if (Character is not Text t)
            {
                return;
            }

            if ((gt.TotalTime - _lastUpdate.TotalTime).TotalMilliseconds < Context.MsPerFrame)
            {
                return;
            }

            _lastUpdate = gt;
            if (t.Value.Length > 0)
            {
                string textValue = null;
                try
                {
                    var val = ScriptObject.ResolveValue(t.Value, ScriptObject);
                    if (val.Type != ValueType.Undefined)
                    {
                        textValue = val.ToString();
                    }
                }
                catch (System.Exception e)
                {
                    Logger.Warn($"Failed to resolve text value: {e}");
                }

                if (TextValue.Original != textValue)
                {
                    TextValue = LocalizedString.CreateApt(textValue);
                }
            }
        }

        protected override void RenderImpl(AptRenderingContext renderingContext)
        {
            if (!Visible)
            {
                return;
            }

            renderingContext.PushTransform(Transform);

            switch (Character)
            {
                case Shape s:
                    var geometry = Context.GetGeometry(s.Geometry, Character);
                    if (RenderCallback != null)
                    {
                        RenderCallback(renderingContext, geometry, Texture);
                    }
                    else
                    {
                        renderingContext.RenderGeometry(geometry, Texture);
                    }

                    if (Highlight)
                    {
                        renderingContext.RenderOutline(geometry);
                    }
                    break;

                case Text t:
                    renderingContext.RenderText(t, TextValue.Localized);
                    break;
            }

            renderingContext.PopTransform();
        }
    }
}
