using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Opcodes;
using ValueType = OpenSage.Gui.Apt.ActionScript.ValueType;

namespace OpenSage.Tools.AptEditor.Apt.Editor
{
    internal static class UtilityMethodsExtensions
    {
        public static string InstructionName(this InstructionBase instruction)
        {
            switch (instruction)
            {
                case LogicalDestination destination:
                    return "BranchDestination";
                case LogicalEndOfFunction endOfFunction:
                    return "EndOfFunction";
                default:
                    return instruction.Type.ToString();
            }
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TKey: notnull where TValue : new()
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, new TValue());
            }

            return dict[key];
        }
    }

    internal class LogicalDestination : InstructionBase
    {
        public override InstructionType Type => throw new InvalidOperationException();
        public override void Execute(ActionContext context) => throw new InvalidOperationException();
        public string Name { get; set; }
        public LogicalBranch LogicalBranch { get; private set; }

        public LogicalDestination(LogicalBranch logicalBranch, string labelName)
        {
            Parameters = new List<Value>();
            LogicalBranch = logicalBranch;
            Name = labelName;
        }
        public override string ToString(ActionContext context) { return ToString(); }
        public override string ToString()
        {
            return "LogicalDestination";
        }
    }

    internal class LogicalEndOfFunction : InstructionBase
    {
        public override InstructionType Type => throw new InvalidOperationException();
        public override void Execute(ActionContext context) => throw new InvalidOperationException();
        public override List<Value> Parameters => throw new NotImplementedException();
        public LogicalDefineFunction LogicalDefineFunction { get; private set; }

        public LogicalEndOfFunction(LogicalDefineFunction logicalDefineFunction)
        {
            // Parameters = new List<Value>();
            LogicalDefineFunction = logicalDefineFunction;
        }

        public override string ToString(ActionContext context) { return ToString(); }
        public override string ToString()
        {
            return "LogicalEndOfFunction";
        }
    }

    internal class LogicalBranch : InstructionBase
    {
        public override InstructionType Type => InnerInstruction.Type;
        public override void Execute(ActionContext context) => throw new InvalidOperationException();
        public InstructionBase InnerInstruction { get; private set; }
        public LogicalDestination Destination { get; private set; }

        public LogicalBranch(InstructionBase instruction, string labelName)
        {
            Parameters = new List<Value>();
            InnerInstruction = instruction;
            Destination = new LogicalDestination(this, labelName);
        }
    }

    internal class LogicalDefineFunction : InstructionBase
    {
        public override InstructionType Type => InnerInstruction.Type;
        public override void Execute(ActionContext context) => throw new InvalidOperationException();
        public InstructionBase InnerInstruction { get; private set; }
        public LogicalEndOfFunction EndOfFunction { get; private set; }

        public LogicalDefineFunction(InstructionBase instruction)
        {
            InnerInstruction = instruction;
            Parameters = InnerInstruction.Parameters.SkipLast(1).ToList();
            EndOfFunction = null;
        }

        public LogicalEndOfFunction CreateEndOfFunction()
        {
            if (EndOfFunction != null)
            {
                throw new InvalidOperationException();
            }
            EndOfFunction = new LogicalEndOfFunction(this);
            return EndOfFunction;
        }

        // Probably it would be better to update InnerFunction.Parameter each time this.Parameter changes
        public List<Value> CreateRealParameters(int functionSize)
        {
            throw new NotImplementedException();
        }
    }

    internal class LogicalInstructions
    {
        public List<InstructionBase> Items { get; set; }
        public Dictionary<string, LogicalDestination> BranchDestinations { get; set; }

        public InstructionCollection Insts { get; private set; }

        public LogicalInstructions(InstructionCollection existingCollection)
        {
            Insts = existingCollection;

            var branchDestinations = new Dictionary<int, List<LogicalDestination>>();
            var endOfFunctions = new Dictionary<int, LogicalEndOfFunction>();

            // First pass: create deep copy of instructions, collect LogicalDestinations and LogicalEndOfFunctions
            var firstPassProcessed = new List<(int, InstructionBase)>();
            /// Sometimes we need to know the end address of current instruction, 
            /// which is the start address of next instruction
            var tasksForNextStep = new Stack<Action<int>>();
            foreach (var (position, instruction) in existingCollection.GetPositionedInstructions())
            {
                while (tasksForNextStep.Any())
                {
                    tasksForNextStep.Pop()(position);
                }

                var instructionToBeAdded =
                    InstructionUtility.NewLogicalInstruction(instruction.GetType(), instruction.Parameters,
                                                             out var pairedInstruction);

                switch (instructionToBeAdded)
                {
                    case LogicalBranch logicalBranch:
                        var offset = logicalBranch.InnerInstruction.Parameters.First().ToInteger();
                        tasksForNextStep.Push(nextPosition =>
                        {
                            var destination = nextPosition + offset;
                            branchDestinations.GetOrCreate(destination).Add(logicalBranch.Destination);
                        });
                        break;
                    case LogicalDefineFunction logicalDefineFunction:
                        var functionSize = logicalDefineFunction.InnerInstruction.Parameters.Last().ToInteger();
                        tasksForNextStep.Push(nextPosition =>
                        {
                            var endOfFunction = nextPosition + functionSize;
                            endOfFunctions[endOfFunction] = logicalDefineFunction.EndOfFunction;
                        });
                        break;
                }

                firstPassProcessed.Add((position, instructionToBeAdded));
            }

            // Second pass: add LogicalDestinations and LogicalEndOfFunctions to instruciton list
            Items = new List<InstructionBase>();
            foreach (var (position, instruction) in firstPassProcessed)
            {
                if (endOfFunctions.TryGetValue(position, out var endOfFunction))
                {
                    Items.Add(endOfFunction);
                }

                if (branchDestinations.TryGetValue(position, out var logicalDestinations))
                {
                    Items.AddRange(logicalDestinations);
                }

                Items.Add(instruction);
            }
        }

        private int IndexOfNextRealInstruction(int currentIndex)
        {
            for (var i = currentIndex + 1; i < Items.Count; ++i)
            {
                switch (Items[i])
                {
                    case LogicalDestination _:
                    case LogicalEndOfFunction _:
                        continue;
                    default:
                        return i;
                }
            }
            throw new IndexOutOfRangeException();
        }

        public InstructionCollection ConvertToRealInstructions()
        {
            // A pretty stupid methodnull
            var positionOfBranches = new Dictionary<LogicalBranch, int>();
            var positionOfDefineFunctions = new Dictionary<LogicalDefineFunction, int>();
            var sortedList = new SortedList<int, InstructionBase>();
            for (var i = 0; i < Items.Count; ++i)
            {
                var current = Items[i];
                switch (current)
                {
                    case LogicalDestination logicalDestination:
                        {
                            var sourceIndex = positionOfBranches[logicalDestination.LogicalBranch];
                            var sourceNext = IndexOfNextRealInstruction(sourceIndex);
                            var destination = IndexOfNextRealInstruction(i);
                            var offset = destination - sourceNext;
                            var parameters = logicalDestination.LogicalBranch.InnerInstruction.Parameters;
                            parameters[0] = Value.FromInteger(offset);
                        }
                        break;
                    case LogicalEndOfFunction logicalEndOfFunction:
                        {
                            var sourceIndex = positionOfDefineFunctions[logicalEndOfFunction.LogicalDefineFunction];
                            var sourceNext = IndexOfNextRealInstruction(sourceIndex);
                            var destination = IndexOfNextRealInstruction(i);
                            var size = destination - sourceNext;
                            var parameters = logicalEndOfFunction.LogicalDefineFunction.CreateRealParameters(size);
                            logicalEndOfFunction.LogicalDefineFunction.InnerInstruction.Parameters = parameters;
                        }
                        break;
                    case LogicalBranch logicalBranch:
                        positionOfBranches.Add(logicalBranch, i);
                        sortedList.Add(i, logicalBranch.InnerInstruction);
                        break;
                    case LogicalDefineFunction defineFunction:
                        positionOfDefineFunctions.Add(defineFunction, i);
                        sortedList.Add(i, defineFunction.InnerInstruction);
                        break;
                    default:
                        sortedList.Add(i, current);
                        break;
                }
            }
            return new InstructionCollection(sortedList);
        }
    }

    internal sealed class ValueTypePattern
    {
        public readonly ValueType[] FirstTypes;
        public readonly ValueType[] RepeatingTypes;
        public readonly ValueType[] LastTypes;

        public ValueTypePattern(ValueType[] firstTypes, ValueType[] repeatingTypes, ValueType[] lastTypes)
        {
            FirstTypes = firstTypes;
            RepeatingTypes = repeatingTypes;
            LastTypes = lastTypes;
        }
    }

    internal static class InstructionUtility
    {
        public static IReadOnlyList<string> InstructionNames => _instructionNames;

        private static Dictionary<Type, ValueType[]> _fixedInstructionsParameterTypes;
        private static Dictionary<Type, ValueTypePattern> _nonFixedInstructionsParameterPattern;
        private static Dictionary<string, Type> _instructionTypes;
        private static List<string> _instructionNames;
        private static int _defaultLabelName = 0;
        private static string _getName => "location_" + (++_defaultLabelName);

        static InstructionUtility()
        {
            _fixedInstructionsParameterTypes = new Dictionary<Type, ValueType[]>();
            _nonFixedInstructionsParameterPattern = new Dictionary<Type, ValueTypePattern>();
            _instructionTypes = new Dictionary<string, Type>();
            _instructionNames = new List<string>();

            var fixeds = RetrieveFixedSizeInstructionMetaData();
            foreach (var (name, type, valueTypes) in fixeds)
            {
                _fixedInstructionsParameterTypes.Add(type, valueTypes);
                _instructionTypes.Add(name, type);
                _instructionNames.Add(name);
            }

            var variables = RetrieveNonFixedSizeInstructionMetaData();
            foreach (var (name, type, pattern) in variables)
            {
                _nonFixedInstructionsParameterPattern.Add(type, pattern);
                _instructionTypes.Add(name, type);
                _instructionNames.Add(name);
            }

            _instructionNames.Sort();
        }

        public static InstructionBase NewLogicalInstruction(Type type, List<Value> parameters,
                                                            out InstructionBase? pairedInstruction)
        {
            var instruction = NewInstruction(type, parameters);
            pairedInstruction = null;
            switch (true)
            {
                case var _ when type == typeof(BranchAlways):
                case var _ when type == typeof(BranchIfTrue):
                    var logicalBranch = new LogicalBranch(instruction, _getName);
                    pairedInstruction = logicalBranch.Destination;
                    return logicalBranch;
                case var _ when type == typeof(DefineFunction):
                case var _ when type == typeof(DefineFunction2):
                    var logicalDefineFunction = new LogicalDefineFunction(instruction);
                    pairedInstruction = logicalDefineFunction.CreateEndOfFunction();
                    return logicalDefineFunction;
            }

            return instruction;
        }

        private static InstructionBase NewInstruction(Type type, List<Value> parameters)
        {
            var instance = Activator.CreateInstance(type);
            if(instance is InstructionBase instruction)
            {
                if (!CheckParameterType(instruction, parameters))
                {
                    throw new InvalidOperationException("Parameter type mismatch");
                }

                instruction.Parameters = parameters.Select(value => DeepCopyInstructionParameters(value)).ToList();
                return instruction;
            }
            throw new InvalidCastException(type.Name);
        }

        private static Value DeepCopyInstructionParameters(Value existing)
        {
            switch (existing.Type)
            {
                case ValueType.Constant:
                    return Value.FromConstant(existing.GetIDValue());
                case ValueType.Float:
                    return Value.FromFloat((float)existing.ToReal());
                case ValueType.Integer:
                    return Value.FromInteger(existing.ToInteger());
                case ValueType.Register:
                    return Value.FromRegister(existing.GetIDValue());
                case ValueType.String:
                    return Value.FromString(existing.ToString());
                default:
                    throw new InvalidOperationException();
            }
        }

        private static bool CheckParameterType(InstructionBase instruction, List<Value> parameters)
        {
            switch (instruction)
            {
                case LogicalBranch _:
                case LogicalDestination _:
                case LogicalEndOfFunction _:
                    return parameters.Count == 0;
                case LogicalDefineFunction logicalDefineFunction:
                    return CheckParameterType(logicalDefineFunction.InnerInstruction,
                                              logicalDefineFunction.Parameters.Append(Value.FromInteger(0)).ToList());
            }

            if (_nonFixedInstructionsParameterPattern.TryGetValue(instruction.GetType(), out var pattern))
            {
                var repeatingParametersCount = parameters.Count - (pattern.FirstTypes.Length + pattern.LastTypes.Length);
                if (repeatingParametersCount < 0)
                {
                    return false;
                }

                if (repeatingParametersCount % pattern.RepeatingTypes.Length != 0)
                {
                    return false;
                }

                for (var i = 0; i < repeatingParametersCount; ++i)
                {
                    if (parameters[i + pattern.FirstTypes.Length].Type != pattern.RepeatingTypes[i % pattern.RepeatingTypes.Length])
                    {
                        return false;
                    }
                }

                return true;
            }

            if (_fixedInstructionsParameterTypes.TryGetValue(instruction.GetType(), out var parameterTypes))
            {
                if (parameters.Count != parameterTypes.Length)
                {
                    return false;
                }

                for (var i = 0; i < parameters.Count; ++i)
                {
                    if (parameters[i].Type != parameterTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            throw new NotImplementedException(instruction.GetType().ToString());
        }

        private static (string, Type, ValueType[])[] RetrieveFixedSizeInstructionMetaData()
        {
            var none = Array.Empty<ValueType>();
            return new (string, Type, ValueType[])[]
            {
                ("ToNumber",            typeof(ToNumber),           none),
                ("NextFrame",           typeof(NextFrame),          none),
                ("Play",                typeof(Play),               none),
                ("Stop",                typeof(Stop),               none),
                ("Add",                 typeof(Add),                none),
                ("Subtract",            typeof(Subtract),           none),
                ("Multiply",            typeof(Multiply),           none),
                ("Divide",              typeof(Divide),             none),
                ("Modulo",              typeof(Modulo),             none),
                ("LogicalAnd",          typeof(LogicalAnd),         none),
                ("LogicalOr",           typeof(LogicalOr),          none),
                ("LogicalNot",          typeof(LogicalNot),         none),
                ("BitwiseAnd",          typeof(BitwiseAnd),         none),
                ("BitwiseOr",           typeof(BitwiseOr),          none),
                ("BitwiseXOr",          typeof(BitwiseXOr),         none),
                ("ShiftLeft",           typeof(ShiftLeft),          none),
                ("ShiftRight",          typeof(ShiftRight),         none),
                ("ShiftRight2",         typeof(ShiftRight2),        none),
                ("StringEquals",        typeof(StringEquals),       none),
                ("Pop",                 typeof(Pop),                none),
                ("ToInteger",           typeof(ToInteger),          none),
                ("GetVariable",         typeof(GetVariable),        none),
                ("SetVariable",         typeof(SetVariable),        none),
                ("StringConcat",        typeof(StringConcat),       none),
                ("GetProperty",         typeof(GetProperty),        none),
                ("SetProperty",         typeof(SetProperty),        none),
                ("RemoveSprite",        typeof(RemoveSprite),       none),
                ("CloneSprite",         typeof(CloneSprite),        none),
                ("Trace",               typeof(Trace),              none),
                ("Delete",              typeof(Delete),             none),
                ("Delete2",             typeof(Delete2),            none),

                ("DefineLocal",         typeof(DefineLocal),        none),
                ("DefineLocal2",        typeof(DefineLocal2),       none),
                ("CallFunction",        typeof(CallFunction),       none),
                ("Return",              typeof(Return),             none),
                ("NewObject",           typeof(NewObject),          none),
                ("NewMethod",           typeof(NewMethod),          none),
                ("InitArray",           typeof(InitArray),          none),
                ("InitObject",          typeof(InitObject),         none),

                ("TypeOf",              typeof(TypeOf),             none),
                ("Add2",                typeof(Add2),               none),
                ("LessThan2",           typeof(LessThan2),          none),
                ("Equals2",             typeof(Equals2),            none),
                ("ToString",            typeof(ToString),           none),
                ("PushDuplicate",       typeof(PushDuplicate),      none),
                ("GetMember",           typeof(GetMember),          none),
                ("SetMember",           typeof(SetMember),          none),
                ("Increment",           typeof(Increment),          none),
                ("Decrement",           typeof(Decrement),          none),
                ("CallMethod",          typeof(CallMethod),         none),
                ("EACallMethod",        typeof(EACallMethod),       none),
                ("Enumerate2",          typeof(Enumerate2),         none),
                ("PushThis",            typeof(PushThis),           none),
                ("PushZero",            typeof(PushZero),           none),
                ("PushOne",             typeof(PushOne),            none),
                ("CallFunc",            typeof(CallFunc),           none),
                ("CallMethodPop",       typeof(CallMethodPop),      none),
                ("Greater",             typeof(Greater),            none),
                ("PushThisVar",         typeof(PushThisVar),        none),
                ("PushGlobalVar",       typeof(PushGlobalVar),      none),
                ("ZeroVar",             typeof(ZeroVar),            none),
                ("PushTrue",            typeof(PushTrue),           none),
                ("PushFalse",           typeof(PushFalse),          none),
                ("PushNull",            typeof(PushNull),           none),
                ("PushUndefined",       typeof(PushUndefined),      none),
                ("CastOp",              typeof(CastOp),             none),
                ("ImplememtsOp",        typeof(ImplementsOp),       none),
                ("GetTime",             typeof(GetTime),            none),
                // with parameters
                ("GotoFrame",           typeof(GotoFrame),          new[] { ValueType.Integer }),
                ("GetUrl",              typeof(GetUrl),             new[] { ValueType.String, ValueType.String }),
                ("SetRegister",         typeof(SetRegister),        new[] { ValueType.Register }),
                // ("ConstantPool",        typeof(ConstantPool),       new[] { (ValueType.) }),
                ("GotoLabel",           typeof(GotoLabel),          new[] { ValueType.String }),
                // ("DefineFunction2",     typeof(DefineFunction2),    new[] { (ValueType) }),
                // ("PushData",            typeof(PushData),           new[] { () }),
                ("BranchAlways",        typeof(BranchAlways),       new[] { ValueType.Integer }),
                ("GetUrl2",             typeof(GetUrl2),            none),
                // ("DefineFunction",      typeof(DefineFunction),     new[] { () }),
                ("BranchIfTrue",        typeof(BranchIfTrue),       new[] { ValueType.Integer }),
                ("GotoFrame2",          typeof(GotoFrame2),         new[] { ValueType.Integer }),
                ("PushString",          typeof(PushString),         new[] { ValueType.String }),
                ("PushConstantByte",    typeof(PushConstantByte),   new[] { ValueType.Constant }),
                ("GetStringVar",        typeof(GetStringVar),       new[] { ValueType.String }),
                ("SetStringVar",        typeof(SetStringVar),       new[] { ValueType.String }),
                ("GetStringMember",     typeof(GetStringMember),    new[] { ValueType.String }),
                ("SetStringMember",     typeof(SetStringMember),    new[] { ValueType.String }),
                ("PushValueOfVar",      typeof(PushValueOfVar),     new[] { ValueType.Constant }),
                ("GetNamedMember",      typeof(GetNamedMember),     new[] { ValueType.Constant }),
                ("CallNamedFuncPop",    typeof(CallNamedFuncPop),   new[] { ValueType.Constant }),
                ("CallNamedFunc",       typeof(CallNamedFunc),      new[] { ValueType.Constant }),
                ("CallNamedMethodPop",  typeof(CallNamedMethodPop), new[] { ValueType.Constant }),
                ("PushFloat",           typeof(PushFloat),          new[] { ValueType.Float }),
                ("PushByte",            typeof(PushByte),           new[] { ValueType.Integer }),
                ("PushShort",           typeof(PushShort),          new[] { ValueType.Integer }),
                ("PushLong",            typeof(PushLong),           new[] { ValueType.Integer }), // TODO need reconstruction
                ("End",                 typeof(End),                none),
                ("CallNamedMethod",     typeof(CallNamedMethod),    new[] { ValueType.Constant }),
                ("Var",                 typeof(Var),                none),
                ("PushRegister",        typeof(PushRegister),       new[] { ValueType.Register }),
                ("PushConstantWord",    typeof(PushConstantWord),   new[] { ValueType.Constant }),
                ("CallFunctionPop",     typeof(CallFunctionPop),    none),
                ("StrictEquals",        typeof(StrictEquals),       none),
                ("Extends",             typeof(Extends),            none),
                ("InstanceOf",          typeof(InstanceOf),         none),
            };
        }

        private static (string, Type, ValueTypePattern)[] RetrieveNonFixedSizeInstructionMetaData()
        {
            var none = Array.Empty<ValueType>();

            var defineFunctionPattern = new ValueTypePattern(
                new[] { ValueType.String, ValueType.Integer },
                new[] { ValueType.String },
                new[] { ValueType.Integer }
            );

            var defineFunction2Pattern = new ValueTypePattern(
                new[] { ValueType.String, ValueType.Integer, ValueType.Integer, ValueType.Integer },
                new[] { ValueType.Integer, ValueType.String },
                new[] { ValueType.Integer }
            );

            return new[]
            {
                ("DefineFunction", typeof(DefineFunction), defineFunctionPattern),
                ("DefineFunction2", typeof(DefineFunction2), defineFunction2Pattern),
                ("ConstantPool", typeof(ConstantPool), new ValueTypePattern(none, new[] { ValueType.Constant }, none)),
                ("PushData", typeof(PushData), new ValueTypePattern(none, new[] { ValueType.Constant }, none)),
            };
        }
    }
}
