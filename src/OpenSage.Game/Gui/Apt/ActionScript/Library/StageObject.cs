using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSage.Gui.Apt.ActionScript.Library
{
    public class StageObject: ObjectContext
    {
        public static new Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            // properties
            ["_parent"] = (avm) => Property.A(
                (tv) => ((StageObject) tv).AnotherGetParent(),
                (tv, val) => { throw new NotImplementedException(); }, 
                false, false),
            ["_x"] = (avm) => Property.A(
                (tv) => Value.FromFloat(((StageObject) tv).Item.Transform.GeometryTranslation.X),
                (tv, val) => { throw new NotImplementedException(); },
                false, false),
            ["_y"] = (avm) => Property.A(
                (tv) => Value.FromFloat(((StageObject) tv).Item.Transform.GeometryTranslation.Y),
                (tv, val) => { throw new NotImplementedException(); },
                false, false),
            ["_name"] = (avm) => Property.A(
                (tv) => Value.FromString(((StageObject) tv).Item.Name),
                (tv, val) => { throw new NotImplementedException(); },
                false, false),
            ["_alpha"] = (avm) => Property.A(
                (tv) => throw new NotImplementedException(),
                (tv, val) =>
                {
                    var ctx = (StageObject) tv;
                    var transform = ctx.Item.Transform;
                    ctx.Item.Transform =
                        transform.WithColorTransform(transform.ColorTransform.WithA(val.ToInteger() / 100.0f));
                },
                false, false),
            // methods
            // nothing
        };

        /// <summary>
        /// The item that this context is connected to
        /// </summary>
        public DisplayItem Item { get; private set; }

        public StageObject(DisplayItem item) : this(item, item.Context.Avm) { }
        /// <summary>
        /// this ActionScript object is bound to an item
        /// </summary>
        /// <param name="item"></param>
        /// the item that this context is bound to
        public StageObject(DisplayItem item, VM vm) : base(vm)
        {
            Item = item;
            // InitializeProperties();
        }

        public override string ToString()
        {
            return Item == null ? "StageObject" : Item.Name;
        }


        // properties
        /*
        private void InitializeProperties()
        {
            //TODO: avoid new fancy switch
            switch (Item.Character)
            {
                case Text t:
                    Variables["textColor"] = Value.FromString(t.Color.ToHex());
                    break;
            }
        }
        */
        /// <summary>
        /// used by text
        /// </summary>
        /// <param name="value">value name</param>
        /// <returns></returns>
        public Value ResolveValue(string value, StageObject ctx)
        {
            var path = value.Split('.');
            var obj = ctx.GetParent();
            var member = path.Last();

            for (var i = 0; i < path.Length - 1; i++)
            {
                var fragment = path[i];

                if (obj.HasMember(fragment))
                {
                    obj = (StageObject) obj.GetMember(fragment).ToObject();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return obj.GetMember(member);
        }

        public StageObject GetParent()
        {
            StageObject result = null;

            if (Item.Parent != null)
            {
                result = Item.Parent.ScriptObject;
            }

            return result;
        }

        private Value AnotherGetParent()
        {
            // Parent of a render item is the parent of the containing sprite
            // TODO: By doing some search on the web,
            // it seems like in Flash / ActionScript 3, when trying to access
            // the `parent` of root object, null or undefined will be returned.
            var parent = Item is RenderItem ? Item.Parent?.Parent?.ScriptObject : Item.Parent?.ScriptObject;
            return Value.FromObject(parent);
        }

        public Value GetProperty(PropertyType property)
        {
            Value result = null;

            switch (property)
            {
                case PropertyType.Target:
                    result = Value.FromString(GetTargetPath());
                    break;
                case PropertyType.Name:
                    result = Value.FromString(Item.Name);
                    break;
                case PropertyType.X:
                    result = Value.FromFloat(Item.Transform.GeometryTranslation.X);
                    break;
                case PropertyType.Y:
                    result = Value.FromFloat(Item.Transform.GeometryTranslation.Y);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return result;
        }

        public void SetProperty(PropertyType property, Value val)
        {
            switch (property)
            {
                case PropertyType.Visible:
                    Item.Visible = val.ToBoolean();
                    break;
                case PropertyType.XScale:
                    Item.Transform.Scale((float) val.ToFloat(), 0.0f);
                    break;
                case PropertyType.YScale:
                    Item.Transform.Scale(0.0f, (float) val.ToFloat());
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Calculates the absolute target path
        /// </summary>
        /// <returns>the target</returns>
        private string GetTargetPath()
        {
            string path;

            if (GetParent() == null)
                path = "/";
            else
            {
                path = GetParent().GetTargetPath();
                path += Item.Name;
            }

            return path;
        }

    }

    public class TextField: StageObject
    {
        public static new Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>>(StageObject.PropertiesDefined)
        {
            ["textColor"] = (avm) => Property.A(
                (tv) => throw new NotImplementedException(),
                (tv, val) =>
                {
                    var ctx = (StageObject) tv;
                    var hexStr = val.ToString();
                    var hexColor = Convert.ToInt32(hexStr, 16);

                    var b = (hexColor & 0xFF) / 255.0f;
                    var g = ((hexColor & 0xFF00) >> 8) / 255.0f;
                    var r = ((hexColor & 0xFF0000) >> 16) / 255.0f;

                    var transform = ctx.Item.Transform;
                    ctx.Item.Transform =
                        transform.WithColorTransform(transform.ColorTransform.WithRGB(r, g, b));
                },
                false, false),
        };

        public static new Dictionary<string, Func<VM, Property>> StaticPropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            
        };

        public TextField(VM vm) : this(null, vm) { }

        public TextField(RenderItem item) : this(item, item.Context.Avm) { }

        public TextField(RenderItem item, VM vm) : base(item, vm)
        {
            
        }
    }

    public class MovieClip : StageObject
    {
        public static new Dictionary<string, Func<VM, Property>> PropertiesDefined = new Dictionary<string, Func<VM, Property>>(StageObject.PropertiesDefined)
        {
            // properties
            ["_currentframe"] = (avm) => Property.A(
                (tv) => Value.FromInteger(((SpriteItem) ((StageObject) tv).Item).CurrentFrame),
                (tv, val) =>
                {
                    throw new NotImplementedException();
                },
                false, false),

            // methods
            ["gotoAndPlay"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     ((MovieClip) tv).GotoAndPlay(actx, args);
                     actx.Push(Value.Undefined());
                 }
                 , avm)), true, false, false),
            ["gotoAndStop"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     ((MovieClip) tv).GotoAndStop(args);
                     actx.Push(Value.Undefined());
                 }
                 , avm)), true, false, false),
            ["stop"] = (avm) => Property.D(Value.FromFunction(new NativeFunction(
                 (actx, tv, args) => {
                     ((MovieClip) tv).Stop();
                     actx.Push(Value.Undefined());
                 }
                 , avm)), true, false, false),
        };

        public static new Dictionary<string, Func<VM, Property>> StaticPropertiesDefined = new Dictionary<string, Func<VM, Property>>()
        {
            
        };

        public MovieClip(VM vm) : this(null, vm) { }

        public MovieClip(RenderItem item) : this(item, item.Context.Avm) { }

        public MovieClip(RenderItem item, VM vm) : base(item, vm)
        {

        }


        public void GotoAndPlay(ActionContext actx, Value[] args)
        {
            if (Item is SpriteItem si)
            {
                var dest = args.First().ResolveRegister(actx);

                if (dest.Type == ValueType.String)
                {
                    si.Goto(dest.ToString());
                }
                else if (dest.Type == ValueType.Integer)
                {
                    si.GotoFrame(dest.ToInteger() - 1);
                }
                else
                {
                    throw new InvalidOperationException("Can only jump to labels or frame numbers");
                }

                si.Play();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void GotoAndStop(Value[] args)
        {
            if (Item is SpriteItem si)
            {
                var dest = args.First();

                if (dest.Type == ValueType.String)
                {
                    si.Goto(dest.ToString());
                }
                else if (dest.Type == ValueType.Integer)
                {
                    si.GotoFrame(dest.ToInteger() - 1);
                }
                else
                {
                    throw new InvalidOperationException("Can only jump to labels or frame numbers");
                }

                si.Stop(true);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Stop()
        {
            if (Item is SpriteItem si)
            {
                si.Stop();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
