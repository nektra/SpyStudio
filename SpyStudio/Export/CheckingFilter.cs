using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpyStudio.Export
{
    public abstract class CheckingFilter
    {
        public CheckingFilter()
        {
            ForcingState = 0;
        }

        protected int ForcingState { get; private set; }

        public abstract bool DoFiltering(string path);
        //Forces DoFiltering() to always return the value of state.
        public void ForceToState(bool state)
        {
            ForcingState = state ? 1 : -1;
        }
        //Restores normal behavior of DoFiltering().
        public void ResetForceToState()
        {
            ForcingState = 0;
        }
    }

}
