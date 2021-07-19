using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Gui.Apt.ActionScript.Library
{
    /// <summary>
    /// This class is meant to access builtin variables and return the corresponding value
    /// </summary>
    public static class Builtin
    {
        private static readonly Dictionary<string, Func<Value[], Value>> BuiltinClasses;
        private static readonly Dictionary<string, Action<ActionContext, ObjectContext, Value[]>> BuiltinFunctions;
        private static readonly Dictionary<string, Func<ObjectContext, Value>> BuiltinVariablesGet;
        private static readonly Dictionary<string, Action<ObjectContext, Value>> BuiltinVariablesSet;
        public static DateTime InitTimeStamp { get; } = DateTime.Now;

        static Builtin()
        {
            // list of builtin objects and their corresponding constructors
            BuiltinClasses = new Dictionary<string, Func<Value[], Value>>
            {
                // ["Color"] = args => Value.FromObject(new ASColor()),
                // ["Array"] = args => Value.FromObject(new ASArray(args)),
                //["Object"] = Function.ObjectConstructor,
                //["Function"] = Function.FunctionConstructor,
            };

            // list of builtin functions
            BuiltinFunctions = new Dictionary<string, Action<ActionContext, ObjectContext, Value[]>>
            {
                // MovieClip methods
                // ["gotoAndPlay"] = (actx, ctx, args) => GotoAndPlay(actx, ctx, args),
                // ["gotoAndStop"] = (actx, ctx, args) => GotoAndStop(ctx, args),
                // ["stop"] = (actx, ctx, args) => Stop(ctx),
                ["loadMovie"] = LoadMovie,
                ["attachMovie"] = AttachMovie,

                // Global constructors / functions
                ["Boolean"] = BoolFunc,
                ["getTime"] = (actx, ctx, args) => GetTime(actx),
                ["clearInterval"] = ClearInterval,
                ["setInterval"] = SetInterval,

                

            };

            // list of builtin variables
            BuiltinVariablesGet = new Dictionary<string, Func<ObjectContext, Value>>
            {
                // Globals
                // ["_root"] = ctx => Value.FromObject(ctx.Item.Context.Root.ScriptObject),
                // ["_global"] = ctx => Value.FromObject(ctx.Item.Context.Avm.GlobalObject),
                // ["extern"] = ctx => Value.FromObject(ctx.Item.Context.Avm.ExternObject),

                // MovieClip methods
                // ["_parent"] = GetParent,
                // ["_name"] = ctx => Value.FromString(ctx.Item.Name),
                // ["_x"] = GetX,
                // ["_y"] = GetY,
                // ["_currentframe"] = ctx => Value.FromInteger(((SpriteItem) ctx.Item).CurrentFrame),
            };

            // list of builtin variables - set
            BuiltinVariablesSet = new Dictionary<string, Action<ObjectContext, Value>>
            {
                /*
                ["_alpha"] = (ctx, v) =>
                {
                    var transform = ctx.Item.Transform;
                    ctx.Item.Transform =
                        transform.WithColorTransform(transform.ColorTransform.WithA(v.ToInteger() / 100.0f));
                },
                ["textColor"] = (ctx, v) =>
                {
                    var hexStr = v.ToString();
                    var hexColor = Convert.ToInt32(hexStr, 16);

                    var b = (hexColor & 0xFF) / 255.0f;
                    var g = ((hexColor & 0xFF00) >> 8) / 255.0f;
                    var r = ((hexColor & 0xFF0000) >> 16) / 255.0f;

                    var transform = ctx.Item.Transform;
                    ctx.Item.Transform =
                        transform.WithColorTransform(transform.ColorTransform.WithRGB(r, g, b));
                },
                */
            };
        }

        public static bool IsBuiltInClass(string name)
        {
            return BuiltinClasses.ContainsKey(name);
        }

        public static bool IsBuiltInFunction(string name)
        {
            return BuiltinFunctions.ContainsKey(name);
        }

        public static bool IsBuiltInVariable(string name)
        {
            return BuiltinVariablesGet.ContainsKey(name) || BuiltinVariablesSet.ContainsKey(name);
        }

        public static void CallBuiltInFunction(string name, ActionContext actx, ObjectContext ctx, Value[] args)
        {
            BuiltinFunctions[name](actx, ctx, args);
        }

        public static Value GetBuiltInVariable(string name, ObjectContext ctx)
        {
            return BuiltinVariablesGet[name](ctx);
        }

        public static void SetBuiltInVariable(string name, ObjectContext ctx, Value val)
        {
            BuiltinVariablesSet[name](ctx, val);
        }

        public static Value GetBuiltInClass(string name, Value[] args)
        {
            return BuiltinClasses[name](args);
        }
        /*
        private static Value GetX(ObjectContext ctx)
        {
            return Value.FromFloat(ctx.Item.Transform.GeometryTranslation.X);
        }

        private static Value GetY(ObjectContext ctx)
        {
            return Value.FromFloat(ctx.Item.Transform.GeometryTranslation.Y);
        }
        */




        private static void GetTime(ActionContext context)
        {
            var result_ = DateTime.Now - Builtin.InitTimeStamp;
            var result = Value.FromFloat(result_.TotalMilliseconds);
            context.Push(result);
        }

        private static void LoadMovie(ActionContext context, ObjectContext ctx, Value[] args)
        {
            var url = Path.ChangeExtension(args[0].ToString(), ".apt");
            var window = context.Apt.Window.Manager.Game.LoadAptWindow(url);

            context.Apt.Window.Manager.QueryPush(window);
        }

        private static void AttachMovie(ActionContext context, ObjectContext ctx, Value[] args)
        {
            var url = Path.ChangeExtension(args[0].ToString(), ".apt");
            var name = args[1].ToString();
            var depth = args[2].ToInteger();
        }

        private static void SetInterval(ActionContext context, ObjectContext ctx, Value[] args)
        {
            var vm = context.Apt.Avm;
            var name = context.Pop().ToString();

            vm.CreateInterval(name, args[1].ToInteger(), args[0].ToFunction(), ctx, Array.Empty<Value>());

            ctx.Variables[name] = Value.FromString(name);
        }

        private static void ClearInterval(ActionContext context, ObjectContext ctx, Value[] args)
        {
            var vm = context.Apt.Avm;
            var name = args[0].ToString();

            vm.ClearInterval(name);
            ctx.Variables.Remove(name);
        }

        private static void BoolFunc(ActionContext context, ObjectContext ctx, Value[] args)
        {
            var result = Value.FromBoolean(args[0].ToBoolean());
            context.Push(result);
        }

    }
}
