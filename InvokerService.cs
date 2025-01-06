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
    // Holds a refrence to the Unity Component
    private readonly Component _component;
    
    // Stores the type of the Component
    private readonly Type _componentType;
    
    // Defines binding flags for specifying access levels
    private BindingFlags _bindingFlags ;

    //Parameter Dictionaries
    private Dictionary<ParameterInfo, string> _savedStrings;
    private Dictionary<ParameterInfo, int> _savedInts;
    private Dictionary<ParameterInfo, float> _savedFloats;
    private Dictionary<ParameterInfo, bool> _savedBools;
    private Dictionary<ParameterInfo, GameObject> _savedObjects;

    // Dictionary to store Parameter dictionaries
    private readonly Dictionary<Type, Func<ParameterInfo, object>> _typeHandler;
    
    

    public InvokerService(Component component) : this()
    {
        // Initialize the component and its type.
        _component = component;
        _componentType = _component.GetType();
        
        // Initialize binding flags to include public, private, static, and instance members.
        _bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        
        // Initialize dictionaries for different parameter types.
        _savedStrings = new Dictionary<ParameterInfo, string>();
        _savedInts = new Dictionary<ParameterInfo, int>();
        _savedFloats = new Dictionary<ParameterInfo, float>();
        _savedBools = new Dictionary<ParameterInfo, bool>();
        _savedObjects = new Dictionary<ParameterInfo, GameObject>();

        var invoker = this;

        // Set up handlers for different parameter types, linking them to respective editor functions.
        _typeHandler = new Dictionary<Type, Func<ParameterInfo, object>>()
        {
            {typeof(string), parInfo => invoker.GetValue(parInfo, invoker._savedStrings, label => EditorGUILayout.TextField(label), "") },
            {typeof(int), parInfo => invoker.GetValue(parInfo, invoker._savedInts, label => EditorGUILayout.IntField(label), 0) },
            { typeof(float), parInfo => invoker.GetValue(parInfo, invoker._savedFloats, label => EditorGUILayout.FloatField(label), 0) },
            { typeof(bool), parInfo => invoker.GetValue(parInfo, invoker._savedBools, label => EditorGUILayout.Toggle(label), false) },
            { typeof(GameObject), parInfo => invoker.GetValue(parInfo, invoker._savedObjects, label => (GameObject)EditorGUILayout.ObjectField(label, typeof(GameObject), true), null) }
        };
    }

    // Retrieves all methods of the Component using the specified binding flags
    public MethodInfo[] GetMethods() => _componentType.GetMethods(_bindingFlags);
    
    // Retrieves methods with names containing the specified string.
    public MethodInfo[] GetMethods(string methodName) => GetMethods().Where(m => m.Name.Contains(methodName)).ToArray();

    // Retrieves methods matching a specific parameter type signature.
    public MethodInfo[] GetMethods(params Type[] types) =>
        GetMethods().Where(m =>
        {
            // Compare parameter type arrays for equality.
            IStructuralEquatable paramTypes = m.GetParameters().Select(p => p.ParameterType).ToArray();
            return paramTypes.Equals((IStructuralEquatable)types);
        }).ToArray();

    // Invokes a method by name with specified parameters.
    public object Invoke(string methodName, params object[] parameters)
    {
        // Get parameter types to find the matching method.
        var types = parameters.Select(p => p.GetType()).ToArray();
        var method = _componentType.GetMethod(methodName, _bindingFlags, null, types, null);
        
        // Invoke the method on the component instance, if found.
        return method?.Invoke(_component, parameters);
    }
    
    // Gets or updates a cached parameter value using the appropriate Unity Editor GUI control.
    public T GetValue<T>(ParameterInfo parInfo, Dictionary<ParameterInfo, T> storage, Func<T, T> editorFunc, T defaultValue)
    {
        // Check if the parameter has a saved value, initialize if not.
        if (!storage.ContainsKey(parInfo))
        {
            storage[parInfo] = defaultValue;
        }

        // Retrieve the current value, update it via the editor function, and store it.
        T value = storage[parInfo];
        storage[parInfo] = editorFunc(value);
        return storage[parInfo];
    }
    
    // Retrieves the editor input for a parameter based on its type.
    public object GetParameterTypes(ParameterInfo parInfo)
    {
        var parameterType = parInfo.ParameterType;

        // Check if the parameter type has a handler defined.
        if (_typeHandler.TryGetValue(parameterType, out var handler))
        {
            return handler(parInfo);
        }
        
        // Return null if no handler is defined for the type.
        return null;
    }
}