using System.Collections.Generic;
using System;

namespace OpenSage.Gui.Apt.ActionScript.Library
{
    class ASArray : ObjectContext
    {
        public static new Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            // properties
            ["constructor"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     ((ASArray) tv).PrototypeInternal = actx.Apt.Avm.Prototypes["Array"];
                     ((ASArray) tv)._values = new List<Value>(args);
                     actx.Push(Value.FromObject(tv));
                 }
                 , avm)), true, false, false),
            ["length"] = (avm) => Property.A(
                (tv) => Value.FromInteger(((ASArray) tv).GetLength()),
                (tv, val) =>
                {
                    var len = val.ToInteger();
                    ((ASArray) tv).ChangeLength(len);
                }
                , false, false), 
            // methods
            // TODO
        };

        public static new Dictionary<string, Func<VM, Property>> StaticPropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            
        };

        private List<Value> _values;
        public int GetLength() { return _values.Count; }
        public void ChangeLength(int len)
        {
            if (len < 0) throw new InvalidOperationException("Array length can not be less than 0.");
            else if (len == 0) _values.Clear();
            else if (len == _values.Count) return;
            else if (len < _values.Count)
            {
                while (len < _values.Count)
                    _values.RemoveAt(len);
            }
            else
            {
                while (len > _values.Count)
                    _values.Add(Value.Undefined());
            }
        }

        public ASArray(VM vm) : this(null, vm) { }
        public ASArray(Value[] args, VM vm): base(vm)
        {
            if (args == null) _values = new List<Value>();
            else _values = new List<Value>(args);
        }
        /*
        public ASArray(Value[] args) : base()
        {
            _values = new List<Value>(args);
            // list of builtin variables
            _builtinVariablesGet.Add("length", () => Value.FromInteger(_values.Count));
        }
        */
        public Value[] GetValues()
        {
            Value[] ans = new Value[_values.Count];
            _values.CopyTo(0, ans, 0, _values.Count);
            return ans;
        }

        public override string ToString()
        {
            return $"[{String.Join(",", _values)}]";
        }
    }
}
