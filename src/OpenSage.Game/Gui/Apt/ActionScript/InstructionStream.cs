using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.Gui.Apt.ActionScript.Opcodes;

namespace OpenSage.Gui.Apt.ActionScript
{
    public sealed class InstructionStream
    {
        /// <summary>
        /// the list of instructions overall
        /// </summary>
        private InstructionCollection _instructions;
        /// <summary>
        /// the current instruction
        /// </summary>
        public int Index { get; private set;  }

        public InstructionStream(InstructionCollection instructions)
        {
            _instructions = instructions;
            Index = 0;
        }

        /// <summary>
        /// Get an instruction and move to the next instruction right away. Skip paddings
        /// </summary>
        /// <returns></returns>
        public InstructionBase GetInstructionNoMove()
        {
            if (Index < 0 || _instructions.Count < Index - 1)
            {
                throw new IndexOutOfRangeException();
            }

            return _instructions.GetInstructionByIndex(Index);
        }

        /// <summary>
        /// Get an instruction and move to the next instruction right away. Skip paddings
        /// </summary>
        /// <returns></returns>
        public InstructionBase GetInstruction()
        {
            if (Index < 0 || _instructions.Count < Index - 1)
            {
                throw new IndexOutOfRangeException();
            }

            return _instructions.GetInstructionByIndex(Index++);
        }

        /// <summary>
        /// Extract a list of instructions from the current stream
        /// </summary>
        /// <param name="bytes">The size of the instructions to be extracted in bytes</param>
        /// <returns></returns>
        public InstructionCollection GetInstructions(int bytes)
        {
            // get the amount of instructions contained in that byterange
            var startPosition = _instructions.GetPositionByIndex(Index);
            var endPosition = startPosition + bytes;


            var subRange = _instructions.GetPositionedInstructions().Skip(Index).TakeWhile((kv) => kv.Key < endPosition);
            if (subRange.Any() && subRange.First().Key != startPosition) // sanity check
            {
                throw new InvalidOperationException("Didn't not get the right instructions!");
            }

            var instructions = new SortedList<int, InstructionBase>();
            foreach (var (position, instruction) in subRange)
            {
                instructions.Add(position, instruction);
            }
            Index += instructions.Count;

            return new InstructionCollection(instructions);
        }


        /// <summary>
        /// Tells that this should be the last interation 
        /// </summary>
        /// <returns></returns>
        public bool IsFinished()
        {
            return Index == _instructions.Count || Index == -1;
        }

        /// <summary>
        /// Move the instruction stream
        /// </summary>
        /// <param name="offset">The offset how much to move in bytes</param>
        public void Branch(int offset)
        {
            var startPosition = _instructions.GetPositionByIndex(Index);
            var destinationPosition = startPosition + offset;
            Index = _instructions.GetIndexByPosition(destinationPosition);
        }

        // TODO: OOB Check?
        public void GotoIndex(int index)
        {
            Index = index;
        }
    }
}
