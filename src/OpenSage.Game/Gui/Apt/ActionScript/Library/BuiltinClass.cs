using System;
using System.Collections.Generic;

namespace OpenSage.Gui.Apt.ActionScript.Library
{
    internal class BuiltinClass : ObjectContext
    {
        protected Dictionary<string, Func<Value[], Value>> _builtinFunctions;
        protected Dictionary<string, Func<Value>> _builtinVariablesGet;

        public BuiltinClass(VM vm): base(vm)
        {
            _builtinFunctions = new Dictionary<string, Func<Value[], Value>>();
            _builtinVariablesGet = new Dictionary<string, Func<Value>>();
        }

        public BuiltinClass() : base()
        {
            _builtinFunctions = new Dictionary<string, Func<Value[], Value>>();
            _builtinVariablesGet = new Dictionary<string, Func<Value>>();
        }

        public override bool IsBuiltInVariable(string name)
        {
            return _builtinVariablesGet.ContainsKey(name);
        }

        public override Value GetBuiltInVariable(string name)
        {
            return _builtinVariablesGet[name]();
        }

        public override bool IsBuiltInFunction(string name)
        {
            if (_builtinFunctions.ContainsKey(name))
            {
                return true;
            }

            return false;
        }

        public override void CallBuiltInFunction(ActionContext context, string name, Value[] args)
        {
            var retVal = _builtinFunctions[name](args);
            if (retVal.Type != ValueType.Undefined)
            {
                context.Push(retVal);
            }
        }
    }
}
