using System;
using OpenSage.Gui.Apt.ActionScript.Library;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{
    /// <summary>
    /// Start playback of the current object (must be sprite)
    /// </summary>
    public sealed class Play : InstructionBase
    {
        public override InstructionType Type => InstructionType.Play;

        public override void Execute(ActionContext context)
        {
            var thisVar = (StageObject) context.This;
            if (thisVar.Item is SpriteItem si)
            {
                si.Play();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Stop playback of the current object (must be sprite)
    /// </summary>
    public sealed class Stop : InstructionBase
    {
        public override InstructionType Type => InstructionType.Stop;

        public override void Execute(ActionContext context)
        {
            var thisVar = (StageObject) context.This;
            if (thisVar.Item is SpriteItem si)
            {
                si.Stop();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Jump to a specific frame number (must be sprite)
    /// </summary>
    public sealed class GotoFrame : InstructionBase
    {
        public override InstructionType Type => InstructionType.GotoFrame;
        public override uint Size => 4;

        public override void Execute(ActionContext context)
        {
            var frame = Parameters[0].ToInteger();
            var thisVar = (StageObject) context.This;
            if (thisVar.Item is SpriteItem si)
            {
                si.GotoFrame(frame);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Jump to a specific frame (label or number) and start playing or stop
    /// </summary>
    public sealed class GotoFrame2 : InstructionBase
    {
        public override InstructionType Type => InstructionType.GotoFrame2;
        public override uint Size => 4;

        public override void Execute(ActionContext context)
        {
            bool play = Convert.ToBoolean(Parameters[0].ToInteger() & 0x01);
            var frame = context.Pop();

            var thisVar = (StageObject) context.This;
            if (thisVar.Item is SpriteItem si)
            {
                if (frame.Type == ValueType.String)
                {
                    si.Goto(frame.ToString());
                }
                else
                {
                    si.GotoFrame(frame.ToInteger());
                }

                if (play)
                {
                    si.Play();
                }
                else
                {
                    si.Stop();
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Jump to a labeled frame (must be sprite)
    /// </summary>
    public sealed class GotoLabel : InstructionBase
    {
        public override InstructionType Type => InstructionType.GotoLabel;
        public override uint Size => 4;

        public override void Execute(ActionContext context)
        {
            var label = Parameters[0].ToString();

            var thisVar = (StageObject) context.This;
            if (thisVar.Item is SpriteItem si)
                si.Goto(label);
            else
                throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// NextFrame 
    /// </summary>
    public sealed class NextFrame : InstructionBase
    {
        public override InstructionType Type => InstructionType.NextFrame;

        public override void Execute(ActionContext context)
        {
            var thisVar = (StageObject) context.This;
            if (thisVar.Item is SpriteItem si)
            {
                si.NextFrame();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
