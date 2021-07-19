using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.Data.Apt.Characters;
using OpenSage.Gui.Apt.ActionScript.Library;
using OpenSage.Gui.Apt.ActionScript.Opcodes;

namespace OpenSage.Gui.Apt.ActionScript
{
    // ECMA-262 #8.3.1
    public class Property
    {
        public bool Enumerable { get; set; }
        public bool Configurable { get; set; }

        public static NamedDataProperty D(Value val, bool w, bool e, bool c) {
            return new NamedDataProperty()
            {
                Value = val,
                Writable = w,
                Enumerable = e,
                Configurable = c,
            };
        }
        public static NamedAccessoryProperty A(Func<ObjectContext, Value> g, Action<ObjectContext, Value> s, bool e, bool c)
        {
            return new NamedAccessoryProperty()
            {
                Get = g,
                Set = s,
                Enumerable = e,
                Configurable = c,
            };
        }

        public virtual string ToString(ActionContext actx)
        {
            return base.ToString();
        }
    }
    public class NamedDataProperty: Property
    {
        public Value Value { get; set; }
        public bool Writable { get; set; }
        public override string ToString(ActionContext actx)
        {
            return Value.ToStringWithType(actx);
        }
    }
    public class NamedAccessoryProperty: Property
    {
        public Func<ObjectContext, Value> Get { get; set; }
        public Action<ObjectContext, Value> Set { get; set; }
    }

    public class ObjectContext
    {

        protected static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public ObjectContext __proto__
        {
            get { return GetMember("__proto__").ToObject(); }
            set { SetMember("__proto__", Value.FromObject(value)); }
        }
        public ObjectContext prototype
        {
            get { return GetMember("prototype").ToObject(); }
            set { SetMember("prototype", Value.FromObject(value)); }
        }
        public Function constructor
        {
            get { return GetMember("constructor").ToFunction(); }
            set { SetMember("constructor", Value.FromFunction(value)); }
        }


        /// <summary>
        /// Contains functions and member variables
        /// </summary>
        public Dictionary<string, Value> Variables { get; set; }
        private Dictionary<string, Property> Properties;

        // public List<Value> Constants { get; set; }

        // internal properties

        public ObjectContext PrototypeInternal { get; protected set; }
        public string Class;
        public bool Extensible;

        /*
        private static ObjectContext _createPrototypes()
        {
            var _op = new ObjectContext(false);
            FunctionPrototype = new ObjectContext(false);
            _foc = new NativeFunction((a, b, c) => new ObjectContext(), false);
            _ffc = new NativeFunction((a, b, c) => throw new NotImplementedException("Nonetheless this is not the real ActionScript."), false); ;

            FunctionPrototype.constructor = _ffc;
            FunctionPrototype.__proto__ = _op;
            _foc.__proto__ = FunctionPrototype;
            _ffc.__proto__ = FunctionPrototype;
            _foc.prototype = _op;
            _ffc.prototype = FunctionPrototype;

            _op.constructor = _foc;
            return _op;
        }
      

        // class inheritance
        protected internal static Function _foc { get; private set; }
        protected internal static Function _ffc { get; private set; }
        public static readonly ObjectContext ObjectPrototype = _createPrototypes(); // = new ObjectContext(false) { __proto__ = null, constructor = Function.ObjectConstructor };
        public static ObjectContext FunctionPrototype { get; private set; } // = new ObjectContext() { constructor = Function.FunctionConstructor };
         */

        // prototype declaration

        public static Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>> ()
        {
            // properties
            ["constructor"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => { tv.PrototypeInternal = actx.Apt.Avm.Prototypes["Object"]; actx.Push(Value.FromObject(tv)); }
                 , avm)), true, false, false),
            ["__proto__"] = (avm) => Property.A(
                 (tv) => Value.FromObject(tv.PrototypeInternal),
                 (tv, val) => tv.PrototypeInternal = val.ToObject(), 
                 false, false),
            // methods
            ["addProperty"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     string name = args.Length > 0 ? args[0].ToString() : null;
                     Function getter = args.Length > 1 ? args[1].ToFunction() : null;
                     Function setter = args.Length > 2 ? args[2].ToFunction() : null;
                     tv.AddProperty(actx, name, getter, setter);
                 }
                 , avm)), true, false, false),
            ["hasOwnProperty"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     var name = args.Length > 0 ? args[0].ToString() : null;
                     var ans = tv.HasOwnMember(name);
                     actx.Push(Value.FromBoolean(ans));
                 }
                 , avm)), true, false, false),
            ["isPropertyEnumerable"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     var name = args.Length > 0 ? args[0].ToString() : null;
                     var ans = tv.IsPropertyEnumerable(name);
                     actx.Push(Value.FromBoolean(ans));
                 }
                 , avm)), true, false, false),
            ["isPrototypeOf"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     var theClass = args.Length > 0 ? args[0].ToObject() : null;
                     var ans = tv.IsPrototypeOf(theClass);
                     actx.Push(Value.FromBoolean(ans));
                 }
                 , avm)), true, false, false),
            ["toString"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     var ans = tv.ToString();
                     actx.Push(Value.FromString(ans));
                 }
                 , avm)), true, false, false),
        };

        public static Dictionary<string, Func<VM, Property>> StaticPropertiesDefined = new Dictionary<string, Func<VM, Property>> ()
        { 
            // ["prototype"] = (avm) => Property.D(Value.FromObject(avm.GetPrototype("Object")), true, false, false),
        };
        

        // equivalent to [[Construct]]
        public ObjectContext(VM vm)
        {
            //Actionscript variables are not case sensitive!
            Variables = new Dictionary<string, Value>(StringComparer.OrdinalIgnoreCase);
            Properties = new Dictionary<string, Property>(StringComparer.OrdinalIgnoreCase);
            PrototypeInternal = vm == null ? null : vm.Prototypes["Object"];
            Class = "Object";
        }

        /// <summary>
        /// this ActionScript object is not bound bound to an item, e.g. for global object
        /// </summary>
        public ObjectContext(): this((VM) null)
        {
            // __proto__ = ObjectPrototype;
        }

        public Dictionary<string, Property>.KeyCollection GetAllProperties() { return Properties.Keys; }

        public virtual void AddProperty(ActionContext context, string name, Function getter, Function setter)
        {
            Func<ObjectContext, Value> fget = getter == null ? null : (tv) =>
            {
                context.PushRecallCode(new PushToIndex());
                getter.Invoke(context, tv, new Value[0]);
                return Value.Indexed;
            };
            Action<ObjectContext, Value> fset = setter == null ? null : (tv, val) =>
            {
                context.PushRecallCode(new Pop());
                setter.Invoke(context, tv, new Value[1] { val });
            };
            var prop = Property.A(fget, fset, false, true);
            Properties[name] = prop;
        }

        public virtual void SetOwnProperty(string name, Property prop)
        {
            Properties[name] = prop;
        }

        public virtual Value GetOwnMember(string name)
        {
            Value ans = Value.Undefined();
            if (Properties.TryGetValue(name, out var prop))
            {
                if (prop is NamedDataProperty)
                    ans = ((NamedDataProperty) prop).Value;
                else if (prop is NamedAccessoryProperty)
                {
                    var prop_ = (NamedAccessoryProperty) prop;
                    if (prop_.Get != null)
                        ans = prop_.Get(this);
                    else
                        logger.Warn($"[WARN] property's getter is null: {name}");
                }
            }
            return ans;
        }

        public virtual void SetOwnMember(string name, Value val, ObjectContext localOverride = null)
        {
            if (val == null)
                val = Value.FromObject(null);
            if (Properties.TryGetValue(name, out var prop))
            {
                if (prop is NamedDataProperty)
                {
                    var prop_ = (NamedDataProperty) prop;
                    if (prop_.Writable)
                    {
                        if (localOverride == null)
                        {
                            if (prop_.Configurable)
                                prop_.Enumerable = val.Enumerable();
                            prop_.Value = val;
                            Properties[name] = prop_;
                        }
                        else
                            localOverride.SetOwnMember(name, val, null);
                    }
                    else
                        logger.Warn($"[WARN] Unwritable property: {name}");
                }
                    
                else if (prop is NamedAccessoryProperty)
                {
                    var prop_ = (NamedAccessoryProperty) prop;
                    if (prop_.Set != null)
                        prop_.Set(localOverride == null ? this : localOverride, val);
                    else
                        logger.Warn($"[WARN] property's setter is null: {name}");
                }
            }
            else
            {
                var prop1 = Property.D(val, true, val.Enumerable(), true);
                Properties[name] = prop1;
            }
        }

        public virtual bool HasOwnMember(string name)
        {
            return Properties.ContainsKey(name) && GetOwnMember(name).Type != ValueType.Undefined;
        }

        public virtual bool DeleteOwnMember(string name)
        {
            if (Properties.TryGetValue(name, out var prop))
            {
                // not necessary?
                // if (prop is NamedDataProperty && ((NamedDataProperty) prop).Value.Type == ValueType.Undefined)
                //     return true;
                if (prop.Configurable)
                {
                    Properties.Remove(name);
                    return true;
                }
                else
                {
                    logger.Warn($"[WARN] Unconfigurable property: {name}");
                    return false;
                }
                    
            }
            return true;
        }

        /// <summary>
        /// return a variable, when not present return Undefined
        /// equivalent to the specop [[Get]]
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public virtual Value GetMember(string name)
        {
            var thisVar = this;
            while (thisVar != null)
            {
                if (thisVar.HasOwnMember(name))
                {
                    var val = thisVar.GetOwnMember(name);
                    if (val.Type != ValueType.Undefined)
                        return val;
                }
                thisVar = thisVar.PrototypeInternal;
            }
            logger.Warn($"[WARN] Undefined property: {name}");
            return Value.Undefined();
        }


        public virtual void SetMember(string name, Value val)
        {
            // case 1: property exists locally
            if (HasOwnMember(name))
                SetOwnMember(name, val);
            else
            {
                var thisVar = this;
                while (thisVar != null)
                {
                    if (thisVar.HasOwnMember(name))
                    {
                        thisVar.SetOwnMember(name, val, this);
                        return;
                    }
                    thisVar = thisVar.PrototypeInternal;
                }
                SetOwnMember(name, val);
            }
        }

        public virtual bool HasMember(string name)
        {
            var thisVar = this;
            while (thisVar != null)
            {
                if (thisVar.HasOwnMember(name))
                {
                    return true;
                }
                thisVar = thisVar.PrototypeInternal;
            }
            return false;
        }

        public virtual bool DeleteMember(string name)
        {
            return DeleteOwnMember(name);
        }

        public virtual bool IsPropertyEnumerable(string name)
        {
            var thisVar = this;
            while (thisVar != null)
            {
                if (thisVar.HasOwnMember(name))
                {
                    return thisVar.Properties[name].Enumerable;
                }
                thisVar = thisVar.PrototypeInternal;
            }
            return false;
        }


        /// <summary>
        ///
        /// this: should be a prototype (has "constructor": function)
        /// theClass: should be an instance (has "prototype": constructor function)
        /// </summary>
        /// <param name="theClass"></param>
        /// <returns></returns>
        public bool IsPrototypeOf(ObjectContext theClass)
        {
            var ans = false;
            var proto = theClass.PrototypeInternal;
            while (proto != null)
            {
                if (proto == this)
                {
                    ans = true;
                    break;
                }
                proto = proto.PrototypeInternal;
            }
            return ans;
        }

        public bool IsFunction() { return this is Function; }
        public bool IsConstructor() { return IsFunction() && prototype.IsPrototype(); }
        public bool IsPrototype() { return constructor != null && constructor.IsFunction(); }

        public bool InstanceOf(ObjectContext cst) // TODO Not complete
        {
            return cst.IsFunction() && (cst.prototype == this.__proto__);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public Value ToPrimitive()
        {
            return null;
        }

        public string ToStringDisp(ActionContext actx)
        {
            string ans = "{\n";
            foreach (string s in Properties.Keys)
            {
                Properties.TryGetValue(s, out var v);
                ans = ans + s + ": " + v.ToString(actx) + ", \n";
            }
            ans = ans + "}";
            return ans;
        }

        // ****** The following functions are to be moved to several different classes

        /// <summary>
        /// Check wether or not a string is a builtin flash variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public virtual bool IsBuiltInVariable(string name)
        {
            if (name == "__proto__" || name == "prototype" || name == "constructor")
                return false;
            return Builtin.IsBuiltInVariable(name);
        }

        /// <summary>
        /// Get builtin variable
        /// </summary>
        /// <param name="name">variable name</param>
        /// <returns></returns>
        public virtual Value GetBuiltInVariable(string name)
        {
            return Builtin.GetBuiltInVariable(name, this);
        }

        /// <summary>
        /// Set a builtin flash variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public virtual void SetBuiltInVariable(string name, Value val)
        {
            Builtin.SetBuiltInVariable(name, this, val);
        }

        /// <summary>
        /// Check whether or not a string is a builtin flash function
        /// </summary>
        /// <param name="name">function name</param>
        /// <returns></returns>
        public virtual bool IsBuiltInFunction(string name)
        {
            return Builtin.IsBuiltInFunction(name);
        }

        /// <summary>
        /// Execute a builtin function maybe move builtin functions elsewhere
        /// </summary>
        /// <param name="actx"></param>
        /// <param name="name">function name</param>
        /// <param name="args"></param>
        public virtual void CallBuiltInFunction(ActionContext actx, string name, Value[] args)
        {
            Builtin.CallBuiltInFunction(name, actx, this, args);
        }


       
    }
}
