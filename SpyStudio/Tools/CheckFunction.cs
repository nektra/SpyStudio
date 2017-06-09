using System;
using Aga.Controls.Tree;

namespace SpyStudio.Tools
{
    public class CheckFunction<TNodeType>
        where TNodeType : Node
    {
        public CheckFunction(CheckFunctionType type, Func<TNodeType, bool> function)
        {
            Type = type;
            Function = function;
        }

        public CheckFunctionType Type { get; set; }
        public Func<TNodeType, bool> Function { get; set; }
    }
}