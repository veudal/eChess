using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepCopyExtensions;

namespace eChess
{
    public static class Extensions
    {
        public static T Clone<T>(this T source)
        {
            return DeepCopyByExpressionTrees.DeepCopyByExpressionTree(source);
        }
    }
}
