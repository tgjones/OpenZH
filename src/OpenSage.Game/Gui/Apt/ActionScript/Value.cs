using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.Data.Apt;
using OpenSage.FileFormats;
using OpenSage.Gui.Apt.ActionScript.Library;

namespace OpenSage.Gui.Apt.ActionScript
{
    public enum ValueType
    {
        String,
        Constant,
        Boolean,
        Integer,
        Register,
        Short,
        Float,
        Object,
        // Function,
        // Array,
        Undefined,

    }

    public class Value
    {
        // Any random Value with a fixed pointer
        // Used in debug and stack trace
        public static readonly Value Indexed = FromInteger(114514);

        public ValueType Type { get; private set; }

        private string _string;
        private bool _boolean;
        private int _number;
        private double _decimal;
        private ObjectContext _object;
        // private Function _function;
        // private Value[] _array;

        public bool IsNumericType()
        {
            return Type == ValueType.Float || Type == ValueType.Integer;
        }

        public bool Enumerable()
        {
            // TODO try to implement although not necessary
            return false;
        }

        public Value ResolveRegister(ActionContext context)
        {
            if (Type != ValueType.Register)
                return this;

            var result = this;
            if (_number < context.RegisterCount && context.RegisterStored(_number))
            {
                var entry = context.GetRegister(_number);
                result = entry;
            }
            return result;
        }

        public Value ResolveConstant(ActionContext context)
        {
            if (Type != ValueType.Constant)
                return this;

            var result = this;
            if (_number < context.Constants.Count) {
                var entry = context.Constants[_number];
                result = entry;
            }
            return result;
        }

        public static Value FromFunction(Function func)
        {
            var v = new Value();
            v.Type = ValueType.Object;
            v._object = func;
            return v;
        }

        public static Value FromObject(ObjectContext obj)
        {
            if (obj != null && obj.IsFunction()) return FromFunction((Function) obj);
            var v = new Value();
            v.Type = ValueType.Object;
            v._object = obj;
            return v;
        }

        public static Value FromArray(Value[] array, VM vm)
        {
            var v = new Value();
            v.Type = ValueType.Object;
            v._object = new ASArray(array, vm);
            return v;
        }

        public static Value FromRegister(uint num)
        {
            var v = new Value();
            v.Type = ValueType.Register;
            v._number = (int) num;
            return v;
        }

        public static Value FromConstant(uint id)
        {
            var v = new Value();
            v.Type = ValueType.Constant;
            v._number = (int) id;
            return v;
        }

        public static Value FromBoolean(bool cond)
        {
            var v = new Value();
            v.Type = ValueType.Boolean;
            v._boolean = cond;
            return v;
        }

        public static Value FromString(string str)
        {
            var v = new Value();
            v.Type = ValueType.String;
            v._string = str;
            return v;
        }

        public static Value FromInteger(int num)
        {
            var v = new Value();
            v.Type = ValueType.Integer;
            v._number = num;
            return v;
        }

        // TODO is it okay?
        public static Value FromUInteger(uint num)
        {
            var v = new Value();
            if (num > 0x0FFFFFFF)
            {
                v.Type = ValueType.Float;
                v._decimal = (double) num;
            }
            else
            {
                v.Type = ValueType.Integer;
                v._number = (int) num;
            }
            return v;
        }

        public static Value FromFloat(double num)
        {
            var v = new Value();
            v.Type = ValueType.Float;
            v._decimal = num;
            return v;
        }

        public static Value Undefined()
        {
            var v = new Value();
            v.Type = ValueType.Undefined;
            return v;
        }


        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public T ToObject<T>() where T: ObjectContext
        {
            if (Type == ValueType.Undefined)
            {
                logger.Error("Cannot create object from undefined!");
                return null;
            }
            if (Type != ValueType.Object)
                throw new InvalidOperationException();

            return (T) _object;
        }

        public ObjectContext ToObject()
        {
            if (Type == ValueType.Undefined)
            {
                logger.Error("Cannot create object from undefined!");
                return null;
            }

            if (Type == ValueType.String)
            {
                return new ASString(this, null);
            }

            if (Type != ValueType.Object)
                throw new InvalidOperationException();

            return _object;
        }

        // Follow ECMA specification 9.4: https://www.ecma-international.org/ecma-262/5.1/#sec-9.4
        public int ToInteger()
        {
            double floatNumber = ToFloat();

            if (double.IsNaN(floatNumber))
            {
                return 0;
            }

            return Math.Sign(floatNumber) * (int) Math.Abs(floatNumber);
        }

        public uint ToUInteger()
        {
            double number = ToFloat();
            if (double.IsNaN(number) || double.IsInfinity(number)) return 0;
            double posInt = Math.Sign(number) * (int) Math.Abs(number);
            uint ans = (uint) (posInt % 0x10000000);
            return ans;
        }

        /// Used by AptEditor to get actual id of constant / register
        public uint GetIDValue()
        {
            if(Type != ValueType.Constant && Type != ValueType.Register)
                throw new InvalidOperationException();
            
            return (uint)_number;
        }

        // TODO: implement integer conversion etc.
        public double ToReal()
        {
            if(Type == ValueType.Float) {
                return _decimal;
            }

            // TODO
            throw new NotImplementedException();
        }

        public bool ToBoolean()
        {
            if (Type == ValueType.String || Type == ValueType.Object)
                throw new InvalidOperationException();

            bool var;

            switch (Type)
            {
                case ValueType.Boolean:
                    var = _boolean;
                    break;
                case ValueType.Undefined:
                    var = false;
                    break;
                case ValueType.Float:
                    var = (_decimal != 0);
                    break;
                case ValueType.Integer:
                case ValueType.Short:
                    var = (_number != 0);
                    break;
                default:
                    throw new InvalidOperationException();
            }


            return var;
        }

        public Function ToFunction()
        {
            if (Type == ValueType.Undefined)
            {
                logger.Error("Undefined Function!");
                return null;
            }
            if (Type != ValueType.Object || _object is not Function)
                throw new InvalidOperationException();

            return (Function) _object;
        }

        // Follow ECMA specification 9.8: https://www.ecma-international.org/ecma-262/5.1/#sec-9.8
        public override string ToString()
        {
            switch (Type)
            {
                case ValueType.String:
                    return _string;
                case ValueType.Boolean:
                    return _boolean.ToString();
                case ValueType.Short:
                case ValueType.Integer:
                case ValueType.Constant:
                case ValueType.Register:
                    return _number.ToString();
                case ValueType.Float:
                    return _decimal.ToString();
                case ValueType.Undefined:
                    return "undefined"; // follows ECMA-262
                case ValueType.Object:
                    // if (_object is Function)
                    // {
                    //     var f = (Function) _object;
                    //     return $"Function({f.Parameters.Count}, {f.Constants.Count})";
                    // }
                    return _object == null ? "null": _object.ToString();
                default:
                    throw new NotImplementedException(Type.ToString());
            }
        }

        public string ToStringWithType(ActionContext ctx)
        {
            var ttype = "?";
            try { ttype = this.Type.ToString().Substring(0, 3); }
            catch (InvalidOperationException e) {}
            String tstr = null;
            if (this.Type == ValueType.Constant && ctx != null)
            {
                tstr = this.ResolveConstant(ctx).ToString();
            }
            else if (this.Type == ValueType.Register && ctx != null) tstr = this.ResolveRegister(ctx).ToString();
            else if (this.Type == ValueType.Object) tstr = this.ToString();
            else tstr = this.ToString();
            return String.Format("({0}){1}", ttype, tstr);
            }

        // Follow ECMA specification 9.3: https://www.ecma-international.org/ecma-262/5.1/#sec-9.3
        public double ToFloat()
        {
            switch (Type)
            {
                case ValueType.Constant:
                case ValueType.Register:
                case ValueType.Short:
                case ValueType.Integer:
                    return _number;
                case ValueType.Float:
                    return _decimal;
                case ValueType.Undefined:
                    return float.NaN;
                case ValueType.Boolean:
                    return _boolean ? 1.0 : 0.0;
                default:
                    throw new NotImplementedException();
            }
        }

        public TEnum ToEnum<TEnum>() where TEnum : struct
        {
            if (Type != ValueType.Integer)
                throw new InvalidOperationException();

            return EnumUtility.CastValueAsEnum<int, TEnum>(_number);
        }

        //TODO: According to page 74, ActionEquals2 of SWF7 format specification
        //http://www.prowiki.org/upload/HelmutLeitner/flash_file_format_specification.pdf
        //Here an Abstract Equality Comparison Algorithm should be implemented.
        //(Section 11.9.3, ECMAScript Specification 3)
        //https://www-archive.mozilla.org/js/language/E262-3.pdf
        public bool Equals(Value b)
        {
            bool result;

            if (Type != b.Type)
                return false;

            switch (Type)
            {
                case ValueType.Undefined:
                    result = true;
                    break;
                case ValueType.String:
                    result = (b._string == _string);
                    break;
                case ValueType.Boolean:
                    result = b._boolean == _boolean;
                    break;
                case ValueType.Object:
                    result = b._object == _object;
                    break;
                case ValueType.Integer:
                    result = b._number == _number;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return result;
        }

        public bool StrictEquals(Value b)
        {
            bool result;

            if (Type != b.Type)
                return false;

            //TODO: Implement Strict Equality Comparison Algorithm
            switch (Type)
            {
                case ValueType.Undefined:
                    result = true;
                    break;
                case ValueType.String:
                    result = (b._string == _string);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return result;
        }
    }
}
