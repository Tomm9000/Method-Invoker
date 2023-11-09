using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public struct InvokerService
{
    private readonly Component _component;
    private readonly Type _componentType;
    private BindingFlags _bindingFlags ;

    private Dictionary<ParameterInfo, string> _savedStrings;
    private Dictionary<ParameterInfo, int> _savedInts;
    private Dictionary<ParameterInfo, float> _savedFloats;
    private Dictionary<ParameterInfo, bool> _savedBools;
    private Dictionary<ParameterInfo, SerializedProperty> _savedObjects;

    public InvokerService(Component component)
    {
        _component = component;
        _componentType = _component.GetType();
        _bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        
        _savedStrings = new Dictionary<ParameterInfo, string>();
        _savedInts = new Dictionary<ParameterInfo, int>();
        _savedFloats = new Dictionary<ParameterInfo, float>();
        _savedBools = new Dictionary<ParameterInfo, bool>();
        _savedObjects = new Dictionary<ParameterInfo, SerializedProperty>();

    }

    public MethodInfo[] GetMethods() => _componentType.GetMethods(_bindingFlags);
    public MethodInfo[] GetMethods(string methodName) => GetMethods().Where(m => m.Name.Contains(methodName)).ToArray();

    public MethodInfo[] GetMethods(params Type[] types) =>
        GetMethods().Where(m =>
        {
            // Cast an array of the param-types to an IStructuralEquatable so we can value-check if the arrays are equal
            IStructuralEquatable paramTypes = m.GetParameters().Select(p => p.ParameterType).ToArray();
            return paramTypes.Equals((IStructuralEquatable)types);
        }).ToArray();

    public object Invoke(string methodName, params object[] parameters)
    {
        var types = parameters.Select(p => p.GetType()).ToArray();
        var method = _componentType.GetMethod(methodName, _bindingFlags, null, types, null);
        return method?.Invoke(_component, parameters);
    }

    public static bool MethodHasParameters(MethodInfo methodInfo) =>
        methodInfo?.GetParameters().Length > 0;
    
    
    public IEnumerable<Type> GetUnderlyingParameterTypes(MethodInfo methodInfo) =>
        methodInfo.GetParameters().Select(pi => pi.ParameterType);
    
    public T GetValue<T>(ParameterInfo parInfo, Dictionary<ParameterInfo, T> storage, Func<T, T> editorFunc, T defaultValue)
    {
        if (!storage.ContainsKey(parInfo))
        {
            storage[parInfo] = defaultValue;
        }

        T value = storage[parInfo];
        storage[parInfo] = editorFunc(value);
        return storage[parInfo];
    }
    
    public object GetParameterTypes(ParameterInfo parInfo)
    {
        var str = parInfo.ParameterType.ToString();
        // Todo! Kan dit beter? Misch als iemand een genie vind een wens hierop gebruiken?
        switch (str)
        {
            case "System.String":
                return GetValue(parInfo, _savedStrings, label => EditorGUILayout.TextField(label), "");
                break;
            case "System.Int32":
                return GetValue(parInfo, _savedInts, label => EditorGUILayout.IntField(label), 0);
                break;
            case "System.Single":
                return GetValue(parInfo, _savedFloats, label => EditorGUILayout.FloatField(label), 0);
                break;
            case "System.Boolean":
                return GetValue(parInfo, _savedBools, label => EditorGUILayout.Toggle(label), false);
                break;
            /*case "System.Object" :
                return GetValue(parInfo, _savedObjects, label =>
                {
                    EditorGUILayout.ObjectField(label);
                    return label;
                }, default);*/
                default:
                break;
        }
        return null;
    }
}






