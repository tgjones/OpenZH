using System.Collections.Generic;
using System;

namespace OpenSage.Gui.Apt.ActionScript.Library
{
    class ASString : ObjectContext
    {
        public static new Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            // properties
            ["constructor"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     ((ASString) tv).PrototypeInternal = actx.Apt.Avm.Prototypes["String"];
                     ((ASString) tv)._value = args[0].ToString();
                     actx.Push(Value.FromObject(tv));
                 }
                 , avm)), true, false, false),
            ["length"] = (avm) => Property.A(
                (tv) => Value.FromInteger(((ASString) tv).GetLength()),
                null
                , false, false),
            // methods
            ["substr"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (vm, tv, args) => {
                     var ans = ((ASString) tv).substr(args);
                     vm.Push(ans);
                 }
                 , avm)), true, false, false),
            // TODO
        };

        public static new Dictionary<string, Func<VM, Property>> StaticPropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            
        };

        private string _value;

        public ASString(VM vm) : this(null, vm) { }
        public ASString(Value value, VM vm) : base(vm)
        {
            if (value == null) _value = "null";
            else _value = value.ToString();
        }
        /*
        public ASString(string value) : base()
        {
            _value = value;

            //list of builtin functions
            _builtinFunctions.Add("substr", substr);

            // list of builtin variables
            _builtinVariablesGet.Add("length", () => Value.FromInteger(_value.Length));
        }
        */
        public int GetLength() { return _value.Length; }

        public Value substr(Value[] args)
        {
            if (args.Length == 1)
            {
                return Value.FromString(_value.Substring(args[0].ToInteger()));
            }
            else if (args.Length == 2)
            {
                return Value.FromString(_value.Substring(args[0].ToInteger(), args[1].ToInteger()));
            }
            else
            {
                throw new InvalidOperationException("Argument count invalid!");
            }
        }
    }
}
