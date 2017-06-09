using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aga.Controls.Tree;

namespace SpyStudio.Tools
{
    public enum Allowability
    {
        Min = 0,
        NotAllowed = 0,
        NotCheckable = 1,
        Checkable = 2,
        Max = 2,
    }

    public class AllowabilityResult<T> where T : Node
    {
        public T Node;
        public Allowability Allowability;
        public AllowabilityResult(T node, Allowability allow)
        {
            Node = node;
            Allowability = allow;
        }
    }
}
