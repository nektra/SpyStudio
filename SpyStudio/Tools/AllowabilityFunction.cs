using System;
using Aga.Controls.Tree;

namespace SpyStudio.Tools
{
    public class AllowabilityFunction<T> : CheckFunction<T> where T : Node
    {
        private static Func<T, bool> CreateFunction(Func<T, Allowability> f)
        {
            return x => f(x) > Allowability.NotAllowed;
        }
        public AllowabilityFunction(Func<T, Allowability> function) : base(CheckFunctionType.AllowabilityFunction, CreateFunction(function))
        {
            AllowFunction = function;
        }
        public Func<T, Allowability> AllowFunction { get; set; }
    }
}