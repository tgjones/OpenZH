using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using OpenSage.FileFormats;
using OpenSage.Gui.Apt.ActionScript.Opcodes;
using static System.Text.Encoding;

namespace OpenSage.Gui.Apt.ActionScript
{
    // Provides some helper functions to parse instructions
    // Implements IDisposable, will automatically seek back the stream once disposed.
    sealed class InstructionParseHelper : IDisposable
    {
        public readonly int StartPosition;
        public int FurthestBranchDestination;

        // Current Position (at the time of call) relative to StartPosition
        public int CurrentPosition => (int) (InputStream.Position);

        private Stream InputStream;
        private long previousPosition;
        private bool disposed;

        public InstructionParseHelper(Stream input, long instructionStartPosition)
        {
            disposed = false;

            InputStream = input;
            previousPosition = InputStream.Position;

            StartPosition = (int) instructionStartPosition;
            InputStream.Seek(StartPosition, SeekOrigin.Begin);
            FurthestBranchDestination = StartPosition;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                InputStream.Seek(previousPosition, SeekOrigin.Begin);
            }
        }

        public BinaryReader GetReader()
        {
            return new BinaryReader(InputStream, UTF8, true);
        }

        // Check if we can continue to parse instructionsf
        public bool CanParse(SortedList<int, InstructionBase> instructions)
        {
            return instructions.Count == 0 ||
                instructions.Last().Value.Type != InstructionType.End ||
                CurrentPosition <= FurthestBranchDestination;
        }

        // Will calculate FurthestBranchDestination with offset and the current RelativePosition
        public void ReportBranchOffset(int offset)
        {
            FurthestBranchDestination = Math.Max(FurthestBranchDestination, CurrentPosition + offset);
        }
    }

    public sealed class InstructionCollection
    {
        private readonly SortedList<int, InstructionBase> _instructions;

        public int Count => _instructions.Count;

        public InstructionCollection(SortedList<int, InstructionBase> instructions)
        {
            _instructions = instructions;
        }

        public IReadOnlyCollection<KeyValuePair<int, InstructionBase>> GetPositionedInstructions()
        {
            return _instructions;
        }

        public ReadOnlyCollection<InstructionBase> GetInstructions()
        {
            return new ReadOnlyCollection<InstructionBase>(_instructions.Values);
        }

        public int GetPositionByIndex(int index)
        {
            return _instructions.Keys[index];
        }

        public int GetIndexByPosition(int position)
        {
            var index = _instructions.IndexOfKey(position);

            if (index > _instructions.Last().Key)
            {
                // We branched behind the last valid instruction...
                return _instructions.Last().Key;
            }

            return index;
        }

        public InstructionBase GetInstructionByIndex(int index)
        {
            return _instructions.Values[index];
        }

        public InstructionBase GetInstructionByPosition(int position)
        {
            return _instructions[position];
        }

        public static InstructionCollection Parse(Stream input, long instructionsPosition)
        {
            var instructions = new SortedList<int, InstructionBase>();
            using (var helper = new InstructionParseHelper(input, instructionsPosition))
            {
                var reader = helper.GetReader();
                while (helper.CanParse(instructions))
                {
                    //now reader the instructions
                    var instructionPosition = helper.CurrentPosition;
                    var type = reader.ReadByteAsEnum<InstructionType>();
                    var requireAlignment = InstructionAlignment.IsAligned(type);

                    if (requireAlignment)
                    {
                        reader.Align(4);
                    }

                    InstructionBase instruction = null;
                    var parameters = new List<Value>();

                    switch (type)
                    {
                        case InstructionType.ToNumber:
                            instruction = new ToNumber();
                            break;
                        case InstructionType.NextFrame:
                            instruction = new NextFrame();
                            break;
                        case InstructionType.Play:
                            instruction = new Play();
                            break;
                        case InstructionType.Stop:
                            instruction = new Stop();
                            break;
                        case InstructionType.Add:
                            instruction = new Add();
                            break;
                        case InstructionType.Subtract:
                            instruction = new Subtract();
                            break;
                        case InstructionType.Multiply:
                            instruction = new Multiply();
                            break;
                        case InstructionType.Divide:
                            instruction = new Divide();
                            break;
                        case InstructionType.BitwiseAnd:
                            instruction = new BitwiseAnd();
                            break;
                        case InstructionType.BitwiseOr:
                            instruction = new BitwiseOr();
                            break;
                        case InstructionType.BitwiseXOr:
                            instruction = new BitwiseXOr();
                            break;
                        case InstructionType.Greater:
                            instruction = new Greater();
                            break;
                        case InstructionType.LessThan:
                            instruction = new LessThan();
                            break;
                        case InstructionType.LogicalAnd:
                            instruction = new LogicalAnd();
                            break;
                        case InstructionType.LogicalOr:
                            instruction = new LogicalOr();
                            break;
                        case InstructionType.LogicalNot:
                            instruction = new LogicalNot();
                            break;
                        case InstructionType.StringEquals:
                            instruction = new StringEquals();
                            break;
                        case InstructionType.Pop:
                            instruction = new Pop();
                            break;
                        case InstructionType.ToInteger:
                            instruction = new ToInteger();
                            break;
                        case InstructionType.GetVariable:
                            instruction = new GetVariable();
                            break;
                        case InstructionType.SetVariable:
                            instruction = new SetVariable();
                            break;
                        case InstructionType.StringConcat:
                            instruction = new StringConcat();
                            break;
                        case InstructionType.GetProperty:
                            instruction = new GetProperty();
                            break;
                        case InstructionType.SetProperty:
                            instruction = new SetProperty();
                            break;
                        case InstructionType.CloneSprite:
                            instruction = new CloneSprite(); // NIE DOM
                            break;
                        case InstructionType.RemoveSprite:
                            instruction = new RemoveSprite(); // NIE DOM
                            break;
                        case InstructionType.Trace:
                            instruction = new Trace();
                            break;
                        case InstructionType.Random:
                            instruction = new RandomNumber();
                            break;
                        case InstructionType.Return:
                            instruction = new Return();
                            break;
                        case InstructionType.Modulo:
                            instruction = new Modulo();
                            break;
                        case InstructionType.TypeOf:
                            instruction = new TypeOf(); // TODO UINT Problem
                            break;
                        case InstructionType.Add2:
                            instruction = new Add2(); // TODO Type Conversion Problem
                            break;
                        case InstructionType.LessThan2:
                            instruction = new LessThan2(); // TODO Type Conversion Problem
                            break;
                        case InstructionType.ToString:
                            instruction = new ToString();
                            break;
                        case InstructionType.PushDuplicate:
                            instruction = new PushDuplicate();
                            break;
                        case InstructionType.Increment:
                            instruction = new Increment();
                            break;
                        case InstructionType.Decrement:
                            instruction = new Decrement();
                            break;
                        case InstructionType.EA_PushThis:
                            instruction = new PushThis();
                            break;
                        case InstructionType.EA_PushZero:
                            instruction = new PushZero();
                            break;
                        case InstructionType.EA_PushOne:
                            instruction = new PushOne();
                            break;
                        case InstructionType.EA_PushThisVar:
                            instruction = new PushThisVar();
                            break;
                        case InstructionType.EA_PushGlobalVar:
                            instruction = new PushGlobalVar();
                            break;
                        case InstructionType.EA_ZeroVar:
                            instruction = new ZeroVar();
                            break;
                        case InstructionType.EA_PushTrue:
                            instruction = new PushTrue();
                            break;
                        case InstructionType.EA_PushFalse:
                            instruction = new PushFalse();
                            break;
                        case InstructionType.EA_PushNull:
                            instruction = new PushNull();
                            break;
                        case InstructionType.EA_PushUndefined:
                            instruction = new PushUndefined();
                            break;
                        case InstructionType.GotoFrame:
                            instruction = new GotoFrame(); // TODO need research
                            parameters.Add(Value.FromInteger(reader.ReadInt32()));
                            break;
                        case InstructionType.GetURL:
                            instruction = new GetUrl();
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.SetRegister:
                            instruction = new SetRegister();
                            parameters.Add(Value.FromRegister(reader.ReadUInt32()));
                            break;
                        case InstructionType.GotoLabel:
                            instruction = new GotoLabel();
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.PushData: // NIE doubtful and not used in any games
                            {
                                throw new NotImplementedException();
                                instruction = new PushData();

                                var count = reader.ReadUInt32();
                                var constants = reader.ReadFixedSizeArrayAtOffset<uint>(() => reader.ReadUInt32(), count);

                                foreach (var constant in constants)
                                {
                                    parameters.Add(Value.FromConstant(constant));
                                }
                            }
                            break;
                        case InstructionType.BranchAlways:
                            {
                                instruction = new BranchAlways();
                                var offset = reader.ReadInt32();
                                parameters.Add(Value.FromInteger(offset));
                                helper.ReportBranchOffset(offset);
                            }
                            break;
                        case InstructionType.GetURL2:
                            instruction = new GetUrl2();
                            break;
                        // OOP Related
                        case InstructionType.ConstantPool:
                            {
                                instruction = new ConstantPool();
                                var count = reader.ReadUInt32();
                                var constants = reader.ReadFixedSizeArrayAtOffset<uint>(() => reader.ReadUInt32(), count);

                                foreach (var constant in constants)
                                {
                                    parameters.Add(Value.FromConstant(constant));
                                }
                            }
                            break;
                        case InstructionType.DefineFunction2: // TODO Flags?
                            {
                                instruction = new DefineFunction2();
                                var name = reader.ReadStringAtOffset();
                                var nParams = reader.ReadUInt32();
                                var nRegisters = reader.ReadByte();
                                var flags = reader.ReadUInt24();

                                //list of parameter strings
                                var paramList = reader.ReadFixedSizeListAtOffset<FunctionArgument>(() => new FunctionArgument()
                                {
                                    Register = reader.ReadInt32(),
                                    Parameter = reader.ReadStringAtOffset(),
                                }, nParams);

                                parameters.Add(Value.FromString(name));
                                parameters.Add(Value.FromInteger((int) nParams));
                                parameters.Add(Value.FromInteger((int) nRegisters));
                                parameters.Add(Value.FromInteger((int) flags));
                                foreach (var param in paramList)
                                {
                                    parameters.Add(Value.FromInteger(param.Register));
                                    parameters.Add(Value.FromString(param.Parameter));
                                }
                                //body size of the function
                                parameters.Add(Value.FromInteger(reader.ReadInt32()));
                                //skip 8 bytes
                                reader.ReadUInt64();
                            }
                            break;
                        case InstructionType.DefineFunction:
                            {
                                instruction = new DefineFunction();
                                var name = reader.ReadStringAtOffset();
                                //list of parameter strings
                                var paramList = reader.ReadListAtOffset<string>(() => reader.ReadStringAtOffset());

                                parameters.Add(Value.FromString(name));
                                parameters.Add(Value.FromInteger(paramList.Count));
                                foreach (var param in paramList)
                                {
                                    parameters.Add(Value.FromString(param));
                                }
                                //body size of the function
                                parameters.Add(Value.FromInteger(reader.ReadInt32()));
                                //skip 8 bytes
                                reader.ReadUInt64();
                            }
                            break;
                        case InstructionType.CallFunction:
                            instruction = new CallFunction();
                            break;
                        case InstructionType.EA_CallFunc:
                            instruction = new CallFunc(); // NIE don't know the difference, plan to implement when encountered
                            break;
                        case InstructionType.EA_CallFuncPop:
                            instruction = new CallFunctionPop();
                            break;
                        case InstructionType.CallMethod:
                            instruction = new CallMethod();
                            break;
                        case InstructionType.EA_CallMethod:
                            instruction = new EACallMethod();
                            break;
                        case InstructionType.EA_CallMethodPop:
                            instruction = new CallMethodPop();
                            break;
                        case InstructionType.DefineLocal:
                            instruction = new DefineLocal();
                            break;
                        case InstructionType.Var:
                            instruction = new DefineLocal2();
                            break;
                        case InstructionType.Delete:
                            instruction = new Delete(); // NIE DOM
                            break;
                        case InstructionType.Delete2:
                            instruction = new Delete2(); // NIE DOM
                            break;
                        case InstructionType.Enumerate2:
                            instruction = new Enumerate2();
                            break;
                        case InstructionType.Equals2:
                            instruction = new Equals2(); // TODO Follow ECMA-262 #11.9.3
                            break;
                        case InstructionType.GetMember:
                            instruction = new GetMember();
                            break;
                        case InstructionType.SetMember:
                            instruction = new SetMember();
                            break;
                        case InstructionType.InitArray:
                            instruction = new InitArray();
                            break;
                        case InstructionType.InitObject:
                            instruction = new InitObject(); // TODO member/property issue
                            break;
                        case InstructionType.NewMethod:
                            instruction = new NewMethod(); // TODO not sure if correct, the document is vague
                            break;
                        case InstructionType.NewObject:
                            instruction = new NewObject(); // TODO replace stack representations
                            break;




                        case InstructionType.BranchIfTrue:
                            {
                                instruction = new BranchIfTrue();
                                var offset = reader.ReadInt32();
                                parameters.Add(Value.FromInteger(offset));
                                helper.ReportBranchOffset(offset);
                            }
                            break;
                        case InstructionType.GotoFrame2:
                            instruction = new GotoFrame2();
                            parameters.Add(Value.FromInteger(reader.ReadInt32()));
                            break;
                        case InstructionType.EA_PushString:
                            instruction = new PushString();
                            //the constant id that should be pushed
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.EA_PushConstantByte:
                            instruction = new PushConstantByte();
                            //the constant id that should be pushed
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_GetStringVar:
                            instruction = new GetStringVar();
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.EA_SetStringVar:
                            instruction = new SetStringVar();
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.EA_GetStringMember:
                            instruction = new GetStringMember();
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.EA_SetStringMember:
                            instruction = new SetStringMember();
                            parameters.Add(Value.FromString(reader.ReadStringAtOffset()));
                            break;
                        case InstructionType.EA_PushValueOfVar:
                            instruction = new PushValueOfVar();
                            //the constant id that should be pushed
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_GetNamedMember:
                            instruction = new GetNamedMember();
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_CallNamedFuncPop:
                            instruction = new CallNamedFuncPop();
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_CallNamedFunc:
                            instruction = new CallNamedFunc();
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_CallNamedMethodPop:
                            instruction = new CallNamedMethodPop();
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_PushFloat:
                            instruction = new PushFloat();
                            parameters.Add(Value.FromFloat(reader.ReadSingle()));
                            break;
                        case InstructionType.EA_PushByte:
                            instruction = new PushByte();
                            parameters.Add(Value.FromInteger(reader.ReadByte()));
                            break;
                        case InstructionType.EA_PushShort:
                            instruction = new PushShort();
                            parameters.Add(Value.FromInteger(reader.ReadUInt16()));
                            break;
                        case InstructionType.EA_PushLong: // TODO follow ECMA-262
                            instruction = new PushLong();
                            parameters.Add(Value.FromUInteger(reader.ReadUInt32()));
                            break;
                        case InstructionType.End: // NIE do not know what to do
                            instruction = new End();
                            break;
                        case InstructionType.EA_CallNamedMethod: // TODO name retrieve
                            instruction = new CallNamedMethod();
                            parameters.Add(Value.FromConstant(reader.ReadByte()));
                            break;
                        case InstructionType.EA_PushRegister:
                            instruction = new PushRegister();
                            parameters.Add(Value.FromRegister(reader.ReadByte()));
                            break;
                        case InstructionType.EA_PushConstantWord:
                            instruction = new PushConstantWord();
                            parameters.Add(Value.FromConstant(reader.ReadUInt16()));
                            break;
                        // OOP Related



                        case InstructionType.StrictEqual:
                            instruction = new StrictEquals();
                            break;
                        case InstructionType.Extends:
                            instruction = new Extends();
                            break;
                        case InstructionType.InstanceOf:
                            instruction = new InstanceOf();
                            break;
                        case InstructionType.ImplementsOp:
                            instruction = new ImplementsOp(); // NIE what is the interface list of an object?
                            break;
                        case InstructionType.CastOp:
                            instruction = new CastOp(); 
                            break;
                        case InstructionType.GetTime:
                            instruction = new GetTime();
                            break;

                        default:
                            throw new InvalidDataException("Unimplemented bytecode instruction:" + type.ToString());
                    }

                    if (instruction != null)
                    {
                        instruction.Parameters = parameters;
                        instructions.Add(instructionPosition, instruction);
                    }
                }
            }

            return new InstructionCollection(instructions);
        }
    }
}
