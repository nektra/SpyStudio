using System.Collections.Generic;
using System.Linq;

namespace Aga.Controls.Extensions
{
    public static class EnumerableExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> anEnumerable, T anItem)
        {
            return anEnumerable.TakeWhile(i => !i.Equals(anItem)).Count();
        }
    }
}
