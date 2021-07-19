using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.Data.Apt;
using OpenSage.Gui.Apt.ActionScript;
using OpenSage.Gui.Apt.ActionScript.Opcodes;
using OpenSage.Gui.Apt.ActionScript.Library;

namespace OpenSage.Gui.Apt.ActionScript
{
    /// <summary>
    /// The actual ActionScript VM
    /// </summary>
    public sealed class VM
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private TimeInterval _lastTick;
        private Dictionary<string, ValueTuple<TimeInterval, int, Function, ObjectContext, Value[]>> _intervals;
        private Stack<ActionContext> _callStack;

        public ObjectContext GlobalObject { get; }
        public ActionContext GlobalContext { get; }
        public ObjectContext ExternObject { get; }

        public static readonly Dictionary<string, Type> BuiltinClassesMapping = new Dictionary<string, Type>() {
            ["Object"] = typeof(ObjectContext),
            ["Function"] = typeof(Function),
            ["Array"] = typeof(ASArray),
            ["Color"] = typeof(ASColor),
            ["String"] = typeof(ASString),
            ["MovieClip"] = typeof(MovieClip),
            ["TextField"] = typeof(TextField), 
        };
        public Dictionary<string, ObjectContext> Prototypes { get; private set; }
        public Dictionary<ObjectContext, Type> PrototypesInverse { get; private set; }

        public ObjectContext GetPrototype(string name) { return Prototypes.TryGetValue(name, out var value) ? value : null; }

        // Delegate to call a function inside the engine
        public delegate void HandleCommand(ActionContext context, string command, string param);
        public HandleCommand CommandHandler;

        // Delegate to retrieve an internal variable from the engine
        public delegate Value HandleExternVariable(string variable);
        public HandleExternVariable VariableHandler;

        // Delegate to load another movie
        public delegate AptFile HandleExternalMovie(string movie);
        public HandleExternalMovie MovieHandler;

        public VM(HandleCommand hc, HandleExternVariable hev, HandleExternalMovie hem) : this()
        {
            CommandHandler = hc;
            VariableHandler = hev;
            MovieHandler = hem;
        }

        public void RegisterClass(string className, Type classType)
        {
            var cst_param = new VM[] { this };
            var newProto = (ObjectContext) Activator.CreateInstance(classType, cst_param);
            var props = (Dictionary<string, Func<VM, Property>>) classType.GetField("PropertiesDefined").GetValue(null);
            var stats = (Dictionary<string, Func<VM, Property>>) classType.GetField("StaticPropertiesDefined").GetValue(null);
            foreach (var p in props)
                newProto.SetOwnProperty(p.Key, p.Value(this));
            var cst = newProto.constructor;
            cst.prototype = newProto;
            foreach (var p in stats)
                cst.SetOwnProperty(p.Key, p.Value(this));
            Prototypes[className] = newProto;
            PrototypesInverse[newProto] = classType;
           
        }

        public ObjectContext ConstructClass(Function cst_func)
        {
            var proto = cst_func.prototype;
            while (proto != null)
            {
                if (PrototypesInverse.ContainsKey(proto)) break;
                proto = proto.__proto__;
            }
            var cst_param = new VM[] { this };
            var ret = (ObjectContext) Activator.CreateInstance(PrototypesInverse[proto], cst_param);
            ret.__proto__ = cst_func.prototype;
            return ret;
        }

        public VM()
        {
            _intervals = new Dictionary<string, ValueTuple<TimeInterval, int, Function, ObjectContext, Value[]>>();
            _callStack = new Stack<ActionContext>();

            // initialize prototypes and constructors of Object and Function class
            Prototypes = new Dictionary<string, ObjectContext>() { ["Object"] = new ObjectContext(null) };
            var obj_proto = Prototypes["Object"];
            var func_proto = new NativeFunction(obj_proto); // Set the PrototypeInternal property
            Prototypes["Function"] = func_proto;
            PrototypesInverse = new Dictionary<ObjectContext, Type>() { [obj_proto] = typeof(ObjectContext), [func_proto] = typeof(DefinedFunction), };

            foreach (var p in ObjectContext.PropertiesDefined)
                obj_proto.SetOwnProperty(p.Key, p.Value(this));
            foreach (var p in Function.PropertiesDefined)
                func_proto.SetOwnProperty(p.Key, p.Value(this));

            var obj_cst = obj_proto.constructor;
            var func_cst = func_proto.constructor;
            obj_cst.prototype = obj_proto;
            func_cst.prototype = func_proto;

            foreach (var p in ObjectContext.StaticPropertiesDefined)
                obj_cst.SetOwnProperty(p.Key, p.Value(this));
            foreach (var p in Function.StaticPropertiesDefined)
                func_cst.SetOwnProperty(p.Key, p.Value(this));

            // initialize built-in classes
            foreach (var c in BuiltinClassesMapping)
            {
                if (c.Key == "Object" || c.Key == "Function")
                    continue;
                RegisterClass(c.Key, c.Value);
            }

            // initialize global vars and methods
            GlobalObject = new ObjectContext(this); // TODO replace it to Stage
            GlobalContext = new ActionContext(GlobalObject, GlobalObject, null, 4);
            ExternObject = new ExternObject(this);
            PushContext(GlobalContext);

            // expose all classes to global object
            foreach (var c in Prototypes)
            {
                GlobalObject.SetMember(c.Key, Value.FromFunction(c.Value.constructor));
            }
        }

        // interval operations

        public void CreateInterval(string name, int duration, Function func, ObjectContext ctx, Value[] args)
        {
            _intervals[name] = (_lastTick,
                                duration,
                                func,
                                ctx,
                                args
                                );
        }

        public void UpdateIntervals(TimeInterval current)
        {
            for (int i = 0; i < _intervals.Count; ++i)
            {
                var interval = _intervals.Values.ElementAt(i);

                if (current.TotalTime.TotalMilliseconds > (interval.Item1.TotalTime.TotalMilliseconds + interval.Item2))
                {
                    Execute(interval.Item3, interval.Item5, interval.Item4);
                    interval.Item1 = current;
                }
            }

            _lastTick = current;
        }

        public void ClearInterval(string name)
        {
            _intervals.Remove(name);
        }

        // stack operations

        public void PushContext(ActionContext context) { _callStack.Push(context); }
        public ActionContext PopContext()
        {
            if (IsCurrentContextGlobal())
                throw new InvalidOperationException("Gloabl execution context is not possible to pop.");
            else
                return _callStack.Pop();
        }
        public ActionContext CurrentContext() { return _callStack.Peek(); }
        public bool IsCurrentContextGlobal() { return _callStack.Count == 1 || CurrentContext().IsOutermost(); }
        public void ForceReturn() { }


        public ActionContext GetActionContext(ActionContext outerVar, ObjectContext thisVar, int numRegisters, List<Value> consts, InstructionCollection code)
        {
            if (thisVar is null) thisVar = GlobalObject;
            var context = new ActionContext(GlobalObject, thisVar, outerVar, numRegisters)
            {
                Apt = outerVar.Apt,
                Stream = new InstructionStream(code),
                Constants = consts,
            };
            return context;
        }

        public ActionContext GetActionContext(AptContext apt, ObjectContext thisVar, int numRegisters, List<Value> consts, InstructionCollection code)
        {
            if (thisVar is null) thisVar = GlobalObject;
            var context = new ActionContext(GlobalObject, thisVar, null, numRegisters)
            {
                Apt = apt,
                Stream = new InstructionStream(code),
                Constants = consts,
            };
            return context;
        }
        /*
        public ActionContext GetActionContext(int numRegisters, InstructionCollection code, ObjectContext scope, List<ConstantEntry> consts)
        {
            var stream = new InstructionStream(code);

            var context = new ActionContext(GlobalObject, scope, null, numRegisters)
            {
                Apt = scope.Item.Context,
                Stream = stream,
                Constants = consts,
            };
            return context;
        }
        */
        // execution


        /// <summary>
        /// Execute once in the current ActionContext.
        /// </summary>
        /// <returns></returns>
        public InstructionBase ExecuteOnce()
        {
            InstructionBase ans = null;

            var context = CurrentContext();
            if (context.Halt)
            {
                throw new NotImplementedException("No more instructions");
            }

            var stream = context.Stream;
            if (stream.IsFinished()) context.Return = true;
            else
            {
                ans = stream.GetInstruction();
                ans.Execute(context);
            }
            context = CurrentContext();

            if (context.Return)
            {
                if (!IsCurrentContextGlobal())
                {
                    var ret = context.Pop();
                    PopContext();
                    // not sure if it is correct, the document is vague again
                    var ccur = CurrentContext();
                    ccur.Push(ret);
                    // special operations requiring being executed after function return
                    context = ccur;
                }
                else
                {
                    context.Halt = true;
                }
            }
            // special operations requiring being executed after function return
            while (context.HasRecallCode())
                context.PopRecallCode().Execute(context);
            return ans;
        }

        public void ExecuteUntilReturn()
        {
            var context = CurrentContext();
            while (!context.Return) ExecuteOnce();
        }

        public void ExecuteUntilHalt()
        {
            while (!CurrentContext().Halt) ExecuteOnce();
        }

        public Value Execute(Function func, Value[] args, ObjectContext thisVar)
        {
            func.Invoke(this.CurrentContext(), thisVar, args);
            ExecuteUntilReturn();
            var ret = CurrentContext().Pop();
            return ret;
            /*
            var context = func.GetContext(this, args, thisVar);
            var stream = context.Stream;
 

            var instr = stream.GetInstruction();
            InstructionBase prevInstr = null;

            while (instr.Type != InstructionType.End)
            {
                instr.Execute(context);

                if (context.Return)
                    return context.Pop();

                if (stream.IsFinished())
                    break;

                prevInstr = instr;
                instr = stream.GetInstruction();
            }

            return Value.Undefined();*/
        }

        public InstructionBase ExecuteOnce(ActionContext context)
        {
            var instr = context.Stream.GetInstruction();
            instr.Execute(context);
            return instr;
        }

        // TODO all should be replaced. This is an extremely dangerous call.
        public void Execute(InstructionCollection code, ObjectContext scope, AptContext apt) { Execute(GetActionContext(apt, scope, 4, null, code)); }
        public void Execute(ActionContext context)
        { 
            var stream = context.Stream;
            var instr = stream.GetInstruction();

            InstructionBase prev = null;

            while (instr.Type != InstructionType.End)
            {
                instr.Execute(context);

                prev = instr;
                instr = stream.GetInstruction();
            }
        }

        public void Handle(ActionContext context, string url, string target)
        {
            UrlHandler.Handle(CommandHandler, MovieHandler, context, url, target);
        }
    }
}
