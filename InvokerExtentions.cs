using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class InvokerExtentions
{
    public static bool MethodHasParameters(this MethodInfo methodInfo) =>
        methodInfo?.GetParameters().Length > 0;
    
    public static IEnumerable<Type> GetUnderlyingParameterTypes(this MethodInfo methodInfo) =>
        methodInfo.GetParameters().Select(pi => pi.ParameterType);
}
