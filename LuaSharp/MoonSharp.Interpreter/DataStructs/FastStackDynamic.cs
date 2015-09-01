using System.Collections.Generic;

namespace MoonSharp.Interpreter.DataStructs
{
#if USE_DYNAMIC_STACKS
	internal class FastStack<T> : FastStackDynamic<T>
#endif

    /// <summary>
    ///     A non preallocated, non_fixed size stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FastStackDynamic<T> : List<T>
    {
        public FastStackDynamic(int startingCapacity)
            : base(startingCapacity)
        {
        }

        public T Push(T item)
        {
            Add(item);
            return item;
        }

        public void Expand(int size)
        {
            for (var i = 0; i < size; i++)
                Add(default(T));
        }

        public void Zero(int index)
        {
            this[index] = default(T);
        }

        public T Peek(int idxofs = 0)
        {
            var item = this[Count - 1 - idxofs];
            return item;
        }

        public void CropAtCount(int p)
        {
            RemoveLast(Count - p);
        }

        public void RemoveLast(int cnt = 1)
        {
            if (cnt == 1)
            {
                RemoveAt(Count - 1);
            }
            else
            {
                RemoveRange(Count - cnt, cnt);
            }
        }

        public T Pop()
        {
            var retval = this[Count - 1];
            RemoveAt(Count - 1);
            return retval;
        }
    }
}