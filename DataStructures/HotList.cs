using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers_2_0.CacheFriendly
{
    /// <summary>
    /// The point here is to store the top of the list in a cache-friendly way, followed by the 
    /// usual List allocation on the heap.
    /// </summary>
    public struct HotList3<T> : IList<T>
    {
        private T Item0, Item1, Item2;
        public int Count { get; private set; }
        private List<T> Extra;

        public T this[int index]
        {
            get
            {
                if (index >= Count) throw new IndexOutOfRangeException();
                return index switch
                {
                    0 => Item0,
                    1 => Item1,
                    2 => Item2,
                    _ => Extra[index - 3],
                };
            }
            set => Set(index, value);
        }
        public void Set(int index, T value)
        {
            if (index >= Count) throw new IndexOutOfRangeException();
            switch (index)
            {
                case 0: Item0 = value; return;
                case 1: Item1 = value; return;
                case 2: Item2 = value; return;
                default: Extra[index - 3] = value; return;
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Count >= 1) array[arrayIndex++] = Item0;
            if (Count >= 2) array[arrayIndex++] = Item1;
            if (Count >= 3) array[arrayIndex++] = Item2;
            if (Extra != null) Extra.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Count < 1) yield break; else yield return Item0;
            if (Count < 2) yield break; else yield return Item1;
            if (Count < 3) yield break; else yield return Item2;
            if (Extra == null) yield break;
            foreach (T item in Extra) yield return item;
        }
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int IndexOf(T item)
        {
            if (Count < 1) return -1; else if (Item0.Equals(item)) return 0;
            if (Count < 2) return -1; else if (Item1.Equals(item)) return 1;
            if (Count < 3) return -1; else if (Item2.Equals(item)) return 2;
            if (Extra == null) return -1;
            int i = Extra.IndexOf(item);
            return (i < 0) ? -1 : i + 3;
        }

        public void Add(T item)
        {
            switch (Count++)
            {
                case 0: Item0 = item; return;
                case 1: Item1 = item; return;
                case 2: Item2 = item; return;
                case 3: Extra = new List<T> { item }; return;
                default: Extra.Add(item); return;
            }
        }

        public void Clear()
        {
            Extra = null;
            Count = 0;
        }

        public void Insert(int index, T item)
        {
            if (index == 0) { (item, Item0) = (Item0, item); index++; }
            if (index == 1) { (item, Item1) = (Item1, item); index++; }
            if (index == 2) { (item, Item2) = (Item2, item); index++; }
            if (index >= 3) Extra.Insert(index - 3, item);
            Count++;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index == 0) { Item0 = Item1; index++; }
            if (index == 1) { Item1 = Item2; index++; }
            if (index == 2 && Extra != null) { Item1 = Extra[0]; index++; }
            Extra.RemoveAt(index - 3);
            Count--;
        }



    }
}
