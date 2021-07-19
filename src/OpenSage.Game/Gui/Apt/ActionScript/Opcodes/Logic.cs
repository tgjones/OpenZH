using System;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{


    /// <summary>
    /// Pop two values from stack and check them for equality. Does work with types. Result on stack
    /// </summary>
    public sealed class Equals2 : InstructionBase
    {
        public override InstructionType Type => InstructionType.Equals2;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();
            bool eq = a.Equals(b);
            context.Push(Value.FromBoolean(eq));

        }
    }

    /// <summary>
    /// Pop two values from stack and compare the two values using the Strict Equality Comparison Algorithm.
    /// Push the resulting Boolean value onto the stack.
    /// </summary>
    public sealed class StrictEquals : InstructionBase
    {
        public override InstructionType Type => InstructionType.Equals2;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();

            context.Push(Value.FromBoolean(a.StrictEquals(b)));
        }
    }

    /// <summary>
    /// Pop two values from stack and check them for equality. Does work with strings. Result on stack
    /// </summary>
    public sealed class LessThan : InstructionBase
    {
        public override InstructionType Type => InstructionType.LessThan;

        //Should work according to ECMA-262 Section 11.8.5
        public override void Execute(ActionContext context)
        {
            var arg1 = context.Pop().ToFloat();
            var arg2 = context.Pop().ToFloat();

            if (double.IsNaN(arg1)) arg1 = 0;
            if (double.IsNaN(arg2)) arg2 = 0;

            bool result = arg2 < arg1;
            context.Push(Value.FromBoolean(result));
            
        }
    }

    /// <summary>
    /// Pop two values from stack and check them for equality. Does work with strings. Result on stack
    /// </summary>
    public sealed class LessThan2 : InstructionBase
    {
        public override InstructionType Type => InstructionType.LessThan2;

        //Should work according to ECMA-262 Section 11.8.5 https://262.ecma-international.org/5.1/#sec-11.8.5
        public override void Execute(ActionContext context)
        {
            var arg1 = context.Pop().ToFloat();
            var arg2 = context.Pop().ToFloat();

            if (double.IsNaN(arg1) || double.IsNaN(arg2))
            {
                context.Push(Value.Undefined());
            }
            else
            {
                bool result = arg2 < arg1;
                context.Push(Value.FromBoolean(result));
            }
        }
    }

    //Pop two values A,B from stack and check if B is greater A (reverse stack order)
    public sealed class Greater : InstructionBase
    {
        public override InstructionType Type => InstructionType.Greater;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop().ToFloat();
            var b = context.Pop().ToFloat();

            context.Push(Value.FromBoolean(b > a));
        }
    }

    //Pop two values A,B from stack and calculate their bitwise and. Result on stack
    public sealed class BitwiseAnd : InstructionBase
    {
        public override InstructionType Type => InstructionType.BitwiseAnd;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop().ToInteger();
            var b = context.Pop().ToInteger();
            var result = Value.FromInteger(a & b);

            context.Push(result);
        }
    }

    //Pop two values A,B from stack and calculate their bitwise or. Result on stack
    public sealed class BitwiseOr : InstructionBase
    {
        public override InstructionType Type => InstructionType.BitwiseAnd;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop().ToInteger();
            var b = context.Pop().ToInteger();
            var result = Value.FromInteger(a | b);

            context.Push(result);
        }
    }

    //Pop two values A,B from stack and calculate their bitwise xor. Result on stack
    public sealed class BitwiseXOr : InstructionBase
    {
        public override InstructionType Type => InstructionType.BitwiseXOr;

        public override void Execute(ActionContext context)
        {
            var a = context.Pop().ToInteger();
            var b = context.Pop().ToInteger();
            var result = Value.FromInteger(a ^ b);

            context.Push(result);
        }
    }

    /// <summary>
    /// Pop a value from stack, convert it to boolean. Push the inverted value back to stack
    /// </summary>
    public sealed class LogicalNot : InstructionBase
    {
        public override InstructionType Type => InstructionType.LogicalNot;

        public override void Execute(ActionContext context)
        {
            var val = context.Pop();
            var boolVal = val.ToBoolean();
            context.Push(Value.FromBoolean(!boolVal));
        }
    }

    /// <summary>
    /// Pop two values from stack and check their logical and value in float.
    /// </summary>
    public sealed class LogicalAnd : InstructionBase
    {
        public override InstructionType Type => InstructionType.LogicalAnd;

        //Should work according to ECMA-262 Section 11.8.5
        public override void Execute(ActionContext context)
        {
            var arg1 = context.Pop().ToFloat();
            var arg2 = context.Pop().ToFloat();
            context.Push(Value.FromBoolean((!double.IsNaN(arg1) && arg1 != 0) && (!double.IsNaN(arg2) && arg2 != 0)));
        }
    }

    /// <summary>
    /// Pop two values from stack and check their logical and value in float.
    /// </summary>
    public sealed class LogicalOr : InstructionBase
    {
        public override InstructionType Type => InstructionType.LogicalOr;

        //Should work according to ECMA-262 Section 11.8.5
        public override void Execute(ActionContext context)
        {
            var arg1 = context.Pop().ToFloat();
            var arg2 = context.Pop().ToFloat();
            context.Push(Value.FromBoolean((!double.IsNaN(arg1) && arg1 != 0) || (!double.IsNaN(arg2) && arg2 != 0)));
        }
    }
}
