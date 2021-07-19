using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenSage.Data.Apt.Characters;
using OpenSage.Data.Apt.FrameItems;
using OpenSage.Data.Apt;
using OpenSage.Gui.Apt.ActionScript;

namespace OpenSage.Gui.Apt
{
    enum CharType: uint
    {
        Shape = 1,
        Text = 2,
        Font = 3,
        Button = 4,
        Sprite = 5,
        Sound = 6,
        Image = 7,
        Morph = 8,
        Movie = 9,
        StaticText = 10,
        None = 11,
        Video = 12
    };

    abstract class InstancedCharacter<T> where T: Character
    {
        //public 
        public AptContext Context { get; }
        public T Prototype { get; }
        public CharType Type { get; }

        public InstancedCharacter(AptContext context, T c)
        {
            Context = context;
            Prototype = c;
        }

        public abstract void FromAptFile();
    }

    class InstancedSprite : InstancedCharacter<Sprite>
    {
        public InstructionCollection InitActions { get; }
        public List<InstFrame> Frames { get; }
        public Dictionary<InstFrameLabel, int> LabeledFrames { get; }
        public InstancedSprite(AptContext context, Sprite c): base(context, c) { }

        public override void FromAptFile()
        {

        }

        public class InstFrameLabel
        {

        }
        public class InstFrame
        {
            public List<FrameOpr> Oprs { get; private set; }
            // event?

            public InstFrame()
            {
                Oprs = new List<FrameOpr>();
            }
            public InstFrame(List<FrameItem> items)
            {

            }
        }

        public class FrameOpr
        {

        }
    }

    class InstancedMovie: InstancedSprite
    {
        public InstancedMovie(AptContext context, Sprite c) : base(context, c) { }
    }
}
