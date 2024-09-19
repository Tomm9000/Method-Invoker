using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SearchService;
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
    private Dictionary<ParameterInfo, GameObject> _savedObjects;


    private readonly Dictionary<Type, Func<ParameterInfo, object>> _typeHandler;
    
    

    public InvokerService(Component component) : this()
    {
        _component = component;
        _componentType = _component.GetType();
        _bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        
        _savedStrings = new Dictionary<ParameterInfo, string>();
        _savedInts = new Dictionary<ParameterInfo, int>();
        _savedFloats = new Dictionary<ParameterInfo, float>();
        _savedBools = new Dictionary<ParameterInfo, bool>();
        _savedObjects = new Dictionary<ParameterInfo, GameObject>();

        var invoker = this;

        _typeHandler = new Dictionary<Type, Func<ParameterInfo, object>>()
        {
            {typeof(string), parInfo => invoker.GetValue(parInfo, invoker._savedStrings, label => EditorGUILayout.TextField(label), "") },
            {typeof(int), parInfo => invoker.GetValue(parInfo, invoker._savedInts, label => EditorGUILayout.IntField(label), 0) },
            { typeof(float), parInfo => invoker.GetValue(parInfo, invoker._savedFloats, label => EditorGUILayout.FloatField(label), 0) },
            { typeof(bool), parInfo => invoker.GetValue(parInfo, invoker._savedBools, label => EditorGUILayout.Toggle(label), false) },
            { typeof(GameObject), parInfo => invoker.GetValue(parInfo, invoker._savedObjects, label => (GameObject)EditorGUILayout.ObjectField(label, typeof(GameObject), true), null) }
        };
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
        var parameterType = parInfo.ParameterType;

        if (_typeHandler.TryGetValue(parameterType, out var handler))
        {
            return handler(parInfo);
        }
        
        return null;
    }
}