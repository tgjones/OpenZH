using System;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{
    /// <summary>
    /// An instruction that pops two values and adds them. Result on stack
    /// </summary>
    public sealed class Add : InstructionBase
    {
        public override InstructionType Type => InstructionType.Add;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();

            context.Push(Value.FromFloat(b.ToFloat() + a.ToFloat()));
        }
    }

    /// <summary>
    /// An instruction that pops two values and subtracts them. Result on stack
    /// </summary>
    public sealed class Subtract : InstructionBase
    {
        public override InstructionType Type => InstructionType.Subtract;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();

            context.Push(Value.FromFloat(b.ToFloat() - a.ToFloat()));
        }
    }

    /// <summary>
    /// Pop two values from stack and add them. Can concatenate strings. Result on stack
    /// The additive operator follows https://262.ecma-international.org/5.1/#sec-11.6.1
    /// </summary>
    public sealed class Add2 : InstructionBase
    {
        public override InstructionType Type => InstructionType.Add2;

        public override void Execute(ActionContext context)
        {
            //Pop two values
            var a = context.Pop();
            var b = context.Pop();

            if (a.IsNumericType() && b.IsNumericType())
            {
                context.Push(Value.FromFloat(b.ToFloat() + a.ToFloat()));
            }
            else
            {
                context.Push(Value.FromString(b.ToString() + a.ToString()));
            }
        }
    }

    /// <summary>
    /// Pop two values from stack, convert them to float and then multiply them. Result on stack
    /// </summary>
    public sealed class Multiply : InstructionBase
    {
        public override InstructionType Type => InstructionType.Multiply;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();

            context.Push(Value.FromFloat(b.ToFloat() * a.ToFloat()));
        }
    }

    /// <summary>
    /// Pop two values from stack, convert them to float and then divide them. Result on stack
    /// </summary>
    public sealed class Divide : InstructionBase
    {
        public override InstructionType Type => InstructionType.Divide;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();

            var af = a.ToFloat();
            var bf = b.ToFloat();

            var val_to_push = Value.FromFloat(float.NaN);

            if (!(af.Equals(Value.FromFloat(0)))) { val_to_push = Value.FromFloat(bf / af); }

            context.Push(val_to_push);
        }
    }

    /// <summary>
    /// Pop two values from stack, convert them to float and then divide them. Result on stack
    /// </summary>
    public sealed class Modulo : InstructionBase
    {
        public override InstructionType Type => InstructionType.Modulo;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();

            var af = a.ToFloat();
            var bf = b.ToFloat();

            var val_to_push = Value.FromFloat(float.NaN);

            if (!(bf.Equals(Value.FromFloat(0)))) { val_to_push = Value.FromFloat(af % bf); }

            context.Push(val_to_push);
        }
    }

    /// <summary>
    /// Pop a value from stack, increments it and pushes it back
    /// </summary>
    public sealed class Increment : InstructionBase
    {
        public override InstructionType Type => InstructionType.Increment;

        public override void Execute(ActionContext context)
        {
            var num = context.Pop().ToInteger();
            context.Push(Value.FromInteger(++num));
        }
    }

    /// <summary>
    /// Pop a value from stack, increments it and pushes it back
    /// </summary>
    public sealed class Decrement : InstructionBase
    {
        public override InstructionType Type => InstructionType.Decrement;

        public override void Execute(ActionContext context)
        {
            var num = context.Pop().ToInteger();
            context.Push(Value.FromInteger(--num));
        }
    }

    public sealed class ShiftLeft : InstructionBase
    {
        public override InstructionType Type => InstructionType.ShiftLeft;

        public override void Execute(ActionContext context)
        {
            var count = context.Pop().ToInteger() & 0b11111;
            var val = context.Pop().ToInteger();
            context.Push(Value.FromInteger(val << count));
        }
    }

    public sealed class ShiftRight : InstructionBase
    {
        public override InstructionType Type => InstructionType.ShiftRight;

        public override void Execute(ActionContext context)
        {
            var count = context.Pop().ToInteger() & 0b11111;
            var val = context.Pop().ToInteger();
            context.Push(Value.FromInteger(val >> count));
        }
    }

    // shift right as uint
    public sealed class ShiftRight2 : InstructionBase
    {
        public override InstructionType Type => InstructionType.ShiftRight2;

        public override void Execute(ActionContext context)
        {
            var count = context.Pop().ToInteger() & 0b11111;
            var val = (uint) context.Pop().ToInteger();
            context.Push(Value.FromInteger((int)(val >> count)));
        }
    }
}
