using System;
using System.Diagnostics;
using System.Numerics;
using OpenSage.Data.Apt.Characters;
using OpenSage.Graphics;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Library;
using OpenSage.Mathematics;

namespace OpenSage.Gui.Apt
{
    public struct ItemTransform : ICloneable
    {
        public static readonly ItemTransform None = new ItemTransform(ColorRgbaF.White, Matrix3x2.Identity, Vector2.Zero);

        public ColorRgbaF ColorTransform;
        public Matrix3x2 GeometryRotation;
        public Vector2 GeometryTranslation;

        public ItemTransform(in ColorRgbaF color, in Matrix3x2 rotation, in Vector2 translation)
        {
            ColorTransform = color;
            GeometryRotation = rotation;
            GeometryTranslation = translation;
        }

        public static ItemTransform operator *(in ItemTransform a, in ItemTransform b)
        {
            return new ItemTransform(a.ColorTransform * b.ColorTransform,
                                     a.GeometryRotation * b.GeometryRotation,
                                     a.GeometryTranslation + b.GeometryTranslation);
        }

        public void Scale(float x, float y)
        {
            GeometryRotation = Matrix3x2.Multiply(Matrix3x2.CreateScale(x, y), GeometryRotation);
        }

        public ItemTransform WithColorTransform(in ColorRgbaF color)
        {
            return new ItemTransform(color,
                         GeometryRotation,
                         GeometryTranslation);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    [DebuggerDisplay("[DisplayItem:{Name}]")]
    public abstract class DisplayItem : DisposableBase
    {
        public AptContext Context { get; protected set; }
        public SpriteItem Parent { get; protected set; }
        public Character Character { get; protected set; }
        public ItemTransform Transform { get; set; }
        public StageObject ScriptObject { get; protected set; }
        public string Name { get; set; }
        public bool Visible { get; set; }
        public int? ClipDepth { get; set; }

        public bool Highlight { get; set; }

        internal RenderTarget ClipMask { get => _clipMask; set => DisposeAndAssign(ref _clipMask, value); }
        private RenderTarget _clipMask;

        /// <summary>
        /// Create a new DisplayItem
        /// </summary>
        /// <param name="character"></param>
        /// The template character that is used for this Item
        /// <param name="context"></param>
        /// Contains information about the AptFile where this is part of
        /// <param name="parent"></param>
        /// The parent displayItem (which must be a SpriteItem)
        public abstract void Create(Character character, AptContext context, SpriteItem parent = null);

        public virtual void Update(TimeInterval gt) { }

        public void Render(AptRenderingContext renderingContext)
        {
            if (ClipDepth.HasValue)
            {
                ClipMask.EnsureSize(renderingContext.WindowSize);
                renderingContext.SetRenderTarget(ClipMask);
            }

            RenderImpl(renderingContext);

            if (ClipDepth.HasValue)
            {
                renderingContext.SetRenderTarget(null);
            }
        }

        protected virtual void RenderImpl(AptRenderingContext renderingContext) { }

        public virtual void RunActions(TimeInterval gt) { }

        public virtual bool HandleInput(Point2D mousePos, bool mouseDown) { return false; }
    }
}
