using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.Data.Apt;
using OpenSage.Gui.Apt.ActionScript.Library;
using OpenSage.Gui.Apt.ActionScript.Opcodes;

namespace OpenSage.Gui.Apt.ActionScript
{
    public sealed class ActionContext
    {
        public ObjectContext This { get; private set; }
        public ObjectContext Global { get; private set; }
        public ActionContext Outer { get; private set; } 
        public AptContext Apt { get; set; }
        public InstructionStream Stream { get; set; }
        public int RegisterCount { get; private set; }
        public Dictionary<string, Value> Params { get; private set; }
        private Dictionary<string, Value> _locals; 
        public bool Return { get; set; }
        public bool Halt { get; set; }
        public List<ConstantEntry> GlobalConstantPool => Apt.Constants.Entries;
        public List<Value> Constants { get; set; }

        private Stack<Value> _stack;
        private Stack<InstructionBase> _immiexec;
        private Value[] _registers { get; set; }

        public ActionContext(ObjectContext globalVar, ObjectContext thisVar, ActionContext outerVar, int numRegisters = 0)
        {
            // assignments
            Global = globalVar;
            This = thisVar;
            if (outerVar == this) outerVar = null;
            Outer = outerVar; // null if the most outside
            RegisterCount = numRegisters;

            // initializations
            _stack = new Stack<Value>();
            _immiexec = new Stack<InstructionBase>();
            _registers = new Value[RegisterCount];
            Params = new Dictionary<string, Value>();
            _locals = new Dictionary<string, Value>();
            Constants = new List<Value>();
            Return = false;
        }

        // basics

        public bool IsOutermost() { return Outer == null || Outer == this; }

        public void PushRecallCode(InstructionBase inst) { _immiexec.Push(inst); }
        public InstructionBase PopRecallCode() { return _immiexec.Pop(); }
        public InstructionBase FirstRecallCode() { return _immiexec.Peek(); }
        public bool HasRecallCode() { return _immiexec.Count != 0; }

        // constant operations

        public void ReformConstantPool(List<Value> Parameters)
        {
            var pool = new List<Value>();

            for (var i = 0; i < Parameters.Count; ++i)
            {
                Value result;
                var entry = GlobalConstantPool[Parameters[i].ToInteger()];
                switch (entry.Type)
                {
                    case ConstantEntryType.String:
                        result = Value.FromString((string) entry.Value);
                        break;
                    case ConstantEntryType.Register:
                        result = Value.FromRegister((uint) entry.Value);
                        break;
                    default:
                        throw new NotImplementedException();
                }
                pool.Add(result);
            }

            Constants = pool;
        }

        // register operations

        public bool RegisterStored(int id) { return _registers[id] != null; }

        public Value GetRegister(int id) {
            if (id < 0 || id >= RegisterCount)
            {
                throw new InvalidOperationException($"Register number {id} is not appropriate! it should be 0~{RegisterCount - 1}.");
            }
            else
            {
                return RegisterStored(id) ? _registers[id] : Value.Undefined() ;
            }
            
        }

        public void SetRegister(int id, Value val)
        {
            if (id < 0 || id >= RegisterCount)
            {
                throw new InvalidOperationException($"Register number {id} is not appropriate! it should be 0~{RegisterCount - 1}.");
            }
            else
            {
                _registers[id] = val;
            }
        }

        public string DumpRegister()
        {
            var ans = $"Total {RegisterCount} Registers";
            for (int i = 0; i < RegisterCount; ++i)
            {
                if (_registers[i] != null) ans = ans + $"\n[{i}]{_registers[i].ToStringWithType(this)}";
            }
            return ans;
        }

        // stack operations

        public string DumpStack()
        {
            var stack_val = _stack.ToArray();
            var ans = String.Join("|", stack_val.Select(x => x.ToStringWithType(this)).ToArray());

            ans = String.Format("TOP|{0}|BOTTOM", ans);
            return ans;
        }

        public void Push(Value v)
        {
            _stack.Push(v);
        }

        public Value Peek()
        {
            var ret = _stack.Peek();
            if (ReferenceEquals(ret, Value.Indexed))
                ret = PushToIndex.PopValue();
            return ret.ResolveConstant(this).ResolveRegister(this);
        }

        public Value Pop()
        {
            var ret = _stack.Pop();
            if (ReferenceEquals(ret, Value.Indexed))
                ret = PushToIndex.PopValue();
            return ret.ResolveConstant(this).ResolveRegister(this);
        }

        // parameters

        /// <summary>
        /// Check if a specific string is a parameter in the current params
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <returns></returns>
        public bool CheckParameter(string name)
        {
            return Params.ContainsKey(name);
        }

        /// <summary>
        /// Returns the value of a parameter. Must be used with CheckParameter
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <returns></returns>
        public Value GetParameter(string name)
        {
            return Params[name];
        }

        /// <summary>
        /// Check if a specific string is a local value to this context
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public bool HasValueOnLocal(string name)
        {
            if (!IsOutermost())
            {
                return _locals.ContainsKey(name);
            }
            else
            {
                return Global.HasMember(name);
            }
            
        }

        public bool HasValueOnChain(string name)
        {
            var ans = false;
            var env = this;
            while (env != null)
            {
                if (env.HasValueOnLocal(name))
                {
                    ans = true;
                    break;
                }
                env = env.Outer;
            }

            return ans;
        }

        /// <summary>
        /// Returns the value of a local variable. Must be used with CheckLocal
        /// </summary>
        /// <param name="name">local name</param>
        /// <returns></returns>
        public Value GetValueOnLocal(string name)
        {
            if (!IsOutermost())
            {
                return HasValueOnLocal(name) ? _locals[name] : Value.Undefined();
            }
            else
            {
                return Global.GetMember(name);
            }
        }

        public Value GetValueOnChain(string name)
        {
            var ans = Value.Undefined();
            var env = this;
            while (env != null)
            {
                if (env.HasValueOnLocal(name))
                {
                    ans = env.GetValueOnLocal(name);
                    break;
                }
                env = env.Outer;
            }

            return ans;
        }

        public void SetValueOnLocal(string name, Value val)
        {
            if (!IsOutermost())
            {
                _locals[name] = val;
            }
            else
            {
                Global.SetMember(name, val);
            }
        }

        public void SetValueOnChain(string name, Value val)
        {
            var env = this;
            while (env != null)
            {
                if (env.HasValueOnLocal(name))
                {
                    env.SetValueOnLocal(name, val);
                    return;
                }
                env = env.Outer;
            }
            SetValueOnLocal(name, val);
        }

        public void DeleteValueOnLocal(string name)
        {
            if (!IsOutermost())
            {
                if (HasValueOnLocal(name)) _locals.Remove(name);
            }
            else
            {
                Global.DeleteMember(name);
            }
        }

        public void DeleteValueOnChain(string name)
        {
            var env = this;
            while (env != null)
            {
                if (env.HasValueOnLocal(name))
                {
                    env.DeleteValueOnLocal(name);
                    return;
                }
                env = env.Outer;
            }
        }

        /// <summary>
        /// Checks for special handled/global objects. After that checks for child objects of the
        /// current object
        /// </summary>
        /// <param name="name">the object name</param>
        /// <returns></returns>
        public Value GetObject(string name)
        {
            Value obj = null;

            if (Builtin.IsBuiltInVariable(name))
            {
                obj = Builtin.GetBuiltInVariable(name, This);
            }
            else
            {
                obj = This.GetMember(name);
            }

            return obj;
        }

        /// <summary>
        /// Get the current target path
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Value GetTarget(string target)
        {
            //empty target means the current scope
            if (target.Length == 0)
                return Value.FromObject(This);

            //depending on wether or not this is a relative path or not
            ObjectContext obj = target.First() == '/' ? Apt.Root.ScriptObject : This;

            foreach (var part in target.Split('/'))
            {
                if (part == "..")
                {
                    obj = ((StageObject) obj).GetParent();
                }
                else
                {
                    obj = obj.GetMember(part).ToObject();
                }
            }

            return Value.FromObject(obj);
        }

        /// <summary>
        /// Call an object constructor. Either builtin or defined earlier
        /// </summary>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public void ConstructObjectAndPush(string name, Value[] args) // TODO need reconstruction
        {
            var funcVal = GetValueOnChain(name);
            Value ret = null;
            if (funcVal.Type != ValueType.Object || !funcVal.ToObject().IsFunction())
            {
                throw new InvalidOperationException("Not a function");
            }
            else
            {
                var cst_func = funcVal.ToFunction();
                var thisVar = Apt.Avm.ConstructClass(cst_func);
                PushRecallCode(new PushValue(Value.FromObject(thisVar)));
                PushRecallCode(new Pop());
                cst_func.Invoke(this, thisVar, args);
                // Apt.Avm.ExecuteUntilReturn();
                // Pop(); // discard the returned value
                // ret = Value.FromObject(thisVar);
            }
        }

        /// <summary>
        /// Preload specified variables
        /// </summary>
        /// <param name="flags">the flags</param>
        public void Preload(FunctionPreloadFlags flags)
        {
            //preloaded variables start at register 1
            int reg = 1;

            //order is important
            if (flags.HasFlag(FunctionPreloadFlags.PreloadThis))
            {
                _registers[reg] = Value.FromObject(This);
                ++reg;
            }
            if (flags.HasFlag(FunctionPreloadFlags.PreloadArguments))
            {
                throw new NotImplementedException();
            }
            if (flags.HasFlag(FunctionPreloadFlags.PreloadSuper))
            {
                throw new NotImplementedException();
            }
            if (flags.HasFlag(FunctionPreloadFlags.PreloadRoot))
            {
                _registers[reg] = Value.FromObject(Apt.Root.ScriptObject);
                ++reg;
            }
            if (flags.HasFlag(FunctionPreloadFlags.PreloadParent))
            {
                _registers[reg] = Value.FromObject(((StageObject) This).GetParent());
                ++reg;
            }
            if (flags.HasFlag(FunctionPreloadFlags.PreloadGlobal))
            {
                _registers[reg] = Value.FromObject(Apt.Avm.GlobalObject);
                ++reg;
            }
            if (flags.HasFlag(FunctionPreloadFlags.PreloadExtern))
            {
                _registers[reg] = Value.FromObject(Apt.Avm.ExternObject);
                ++reg;
            }
            if (!flags.HasFlag(FunctionPreloadFlags.SupressSuper))
            {
                throw new NotImplementedException();
            }

            if (!flags.HasFlag(FunctionPreloadFlags.SupressArguments))
            {
                throw new NotImplementedException();
            }

            if (!flags.HasFlag(FunctionPreloadFlags.SupressThis))
            {
                _locals["this"] = Value.FromObject(This);
            }
        }
    }
}
