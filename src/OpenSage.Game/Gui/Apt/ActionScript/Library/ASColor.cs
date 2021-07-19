using System.Linq;
using System;
using System.Collections.Generic;
using OpenSage.Mathematics;

namespace OpenSage.Gui.Apt.ActionScript.Library
{
    internal sealed class ASColor : ObjectContext
    {
        public static new Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            // properties
            // methods
            ["getRGB"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (vm, tv, args) => {
                     var ans = ((ASColor) tv).getRGB();
                     vm.Push(ans);
                 }
                 , avm)), true, false, false),
            ["setRGB"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                (vm, tv, args) => {
                    ((ASColor) tv).setRGB(args);
                    vm.Push(Value.Undefined());
                }
                , avm)), true, false, false),
        };

        public static new Dictionary<string, Func<VM, Property>> StaticPropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            
        };

        private ColorRgba _color = ColorRgba.White;

        public ASColor(VM vm) : base(vm) { }
        /*
        public ASColor() : base()
        {
            //list of builtin functions
            _builtinFunctions.Add("setRGB", setRGB);
        }
        */
        public Value getRGB()
        {
            return Value.FromInteger(_color.ToIntegerRGB());
        }
        public Value setRGB(Value[] args)
        {
            _color = ColorRgba.FromHex(_color, args.First().ToString());
            return Value.Undefined();
        }
    }
}
