using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefCountStackViewer
{
    public class StackFrame
    {
        public string Method { get; set; }
        public string Offset { get; set; }
        public string Ebp { get; set; }

        public static bool Matches(StackFrame left, StackFrame right)
        {
            if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null))
                return object.ReferenceEquals(left, right);

            return left.Method == right.Method
                        && left.Ebp == right.Ebp;
        }


        public override string ToString()
        {
            return "{StackFrame, " + Ebp + ", " + Method;
        }
    }
}
