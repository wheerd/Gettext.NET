using System;
using System.Collections;
using System.Collections.Generic;

namespace GettextDotNet
{
    public static class ExtensionMethods
    {
        public static void ReplaceFirstOccurence<T>(this Stack<T> stack, T find, T replacement)
        {
            var tmpStack = new Stack<T>();
            while (stack.Count > 0 && !stack.Peek().Equals(find))
            {
                tmpStack.Push(stack.Pop());
            }
            var error = stack.Count == 0;
            if (!error)
            {
                stack.Pop();
                stack.Push(replacement);
            }
            while (tmpStack.Count > 0)
            {
                stack.Push(tmpStack.Pop());
            }
            if (error)
            {
                throw new ArgumentException(String.Format("The object '{0}' could not be found on the stack", find), "find");
            }
        }
    }
}
