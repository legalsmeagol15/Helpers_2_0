using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Channels;
using Helpers;


namespace Helpers_2_0.Dependency
{
    public class Expression
    {
        public readonly DependencyFunction Fn;
        public readonly IList<object> Inputs;
        public object Value { get; private set; }
        public override string ToString()
        {
            throw new NotImplementedException();
        }
        
        internal DependencyNode ToNode(string name, List<DependencyNode> result)
        {
            int this_idx = result.Count;
            DependencyNode this_node = new(name, Fn);
            result.Add(this_node);
            var childValues = new object[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
            {
                object input = Inputs[i];
                if (input is Expression e)
                {
                    DependencyNode child_n = e.ToNode(null, result);
                    child_n.Listeners.Add(new ListenerLink(this_idx, i));
                    childValues[i] = child_n.Value;
                }
                else
                    childValues[i] = input;
            }
            this_node.Value = Fn(childValues);
            return this_node;
        }
        internal IList<DependencyNode> ToNodes(string name)
        {
            List<DependencyNode> result = new();
            ToNode(name, result);
            return result;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// This data object is optimized to make updates work as fast as possible.
    /// </remarks>
    public sealed class DependencyNodeSet
    {

        private static readonly Channel<UpdateArgs> _Updates = Channel.CreateUnbounded<UpdateArgs>();
        public static readonly ChannelReader<UpdateArgs> Updates = _Updates.Reader;

        private readonly List<DependencyNode> _Nodes = new();
        private readonly DataStructures.SkipList<(int,int)> _Gaps = new();
        private readonly List<object> _LockObjects = new();
        private readonly Dictionary<string, int> Names = new();
        private class SizeComparer : IComparer<Tuple<int, int>>
        {
            int IComparer<Tuple<int, int>>.Compare(Tuple<int, int> x, Tuple<int, int> y)
            {
                if (y.Item2 == int.MaxValue) return 1;
                if (x.Item1 == int.MinValue) return -1;
                return (x.Item2 - x.Item1).CompareTo(y.Item2 - y.Item1);
            }
        }
        
        public DependencyNodeSet()
        {
            _Gaps.Add((0, int.MaxValue));
        }
        
        public int Add(string name, Expression e)
        {
            IList<DependencyNode> nodes = e.ToNodes(name);

            // TODO:  is this backwards?  The one just "before" a gap of this size would be the first one too small?
            if (!_Gaps.TryGetBeforeOrEqual((1, nodes.Count), out var available))
                throw new Exception("No available gaps.");
            int index = available.Item1;
            _Gaps.Remove(available);

            
            for (int i = 0; i < nodes.Count; i++)
            {
                var n = nodes[i];
                // The indices in this group of nodes should all be relative to the parent index.
                for (int j = 0; j < n.Listeners.Count; j++)
                    n.Listeners.Set(j, n.Listeners[j] + index);
                // Copy the new nodes into position.
                if (index + i < _Nodes.Count)
                    _Nodes[index + i] = n;
                else
                    _Nodes.Add(n);
            }
            return index;
        }
        public void Update(int index)
        {

        }
        public int IndexOf(string name)
        {
            if (Names.TryGetValue(name, out int idx)) return idx;
            return -1;
        }
    }
    public sealed class NodeReference
    {

    }

    internal struct DependencyNode
    {
        public string Name;
        public readonly DependencyFunction Fn;
        public object Value { get; internal set; }
        internal readonly CacheFriendly.HotList3<ListenerLink> Listeners, Values;
        
        public bool IsLiteral => Fn == null;
        public bool IsHead => Name == null;

        public DependencyNode(string name, DependencyFunction fn)
        {
            Name = name;
            Fn = fn;
            Value = null;
            Listeners = new();
            Values = new();
        }
        

    }
    internal readonly struct ListenerLink
    {
        private const int ARG_MASK = 0b1111;
        private static readonly int SHIFT = (int)Math.ILogB(ARG_MASK);
        private readonly int _Position;
        /// <summary>The argument number within the listener.</summary>
        public int Argument => (_Position & ARG_MASK);
        /// <summary>The index number of the listener.</summary>
        public int Index => _Position >> SHIFT;
        private ListenerLink(int position) { this._Position = position; }
        public ListenerLink(int index, int argument, bool head = true) : this((index << SHIFT) | argument) { }
        public override bool Equals(object obj) => (obj is ListenerLink other) && other._Position == this._Position;
        public override int GetHashCode() => _Position;
        public static bool operator ==(ListenerLink a, ListenerLink b) => a._Position == b._Position;
        public static bool operator !=(ListenerLink a, ListenerLink b) => a._Position != b._Position;
        public static ListenerLink operator +(ListenerLink ll, int offset) => new(((ll.Index + offset) << SHIFT) | ll.Argument);
        public static ListenerLink operator -(ListenerLink ll, int offset) => new(((ll.Index - offset) << SHIFT) | ll.Argument);
    }
    public readonly struct UpdateArgs
    {
        public readonly int Index;
        public readonly object NewValue;
        public readonly object OldValue;
        public UpdateArgs(int index, object oldValue = null, object newValue = null)
        { this.Index = index; this.OldValue = oldValue; this.NewValue = newValue; }
    }
    public delegate object DependencyFunction(params object[] inputs);
}
