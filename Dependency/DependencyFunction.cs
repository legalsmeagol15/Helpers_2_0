using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers_2_0.Dependency
{
    public delegate object DependencyFunction(params object[] inputs);
    public sealed class DependencyVocabulary
    {
        
        private Dictionary<DependencyFunction, List<Signature>> Signatures = new();
        public bool Matches(DependencyFunction fn, params Type[] types)
        {
            if (!Signatures.TryGetValue(fn, out var sigs)) return false;
            return sigs.Any(s => s.MatchesType(types));
        }

        private class Signature
        {
            public readonly Type[] Types;
            public IList<Func<bool, object>> Rules;
            public bool MatchesType(params Type[] types)
            {
                if (Types.Length != types.Length) return false;
                for (int i = 0; i < Types.Length; i++)
                {
                    if (!types[i].IsAssignableTo(Types[i])) return false;
                }
                return true;
            }
            public bool MatchesRules(IList<object> objs)
            {
                if (Rules == null) return true;
                
            }
        }
    }

    
}
