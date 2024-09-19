using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.UIElements;

[CustomEditor(typeof(GameObject))]
public class ComponentFilter : Editor
{
    private SerializedProperty _componentProperty;
    private SerializedProperty _bindingFlagsProperty;

    private Component _targetComponent;
    private BindingFlags _bindingFlags ;

    private InvokerService? _invokerService;
    private MethodInfo[] _allMethods = Array.Empty<MethodInfo>();

    private bool[] _methodFoldouts;
    private bool _showMethodInvoker;
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        RenderMethodInvoker();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    }
    
    private void RenderMethodInvoker()
    {
        _targetComponent = (Component) EditorGUILayout.ObjectField("Script: ", _targetComponent, typeof(Component));
        
        if (GUI.changed)
        {
            OnValidate();
        }

        EditorGUILayout.Separator();
        
        DisplayMethods();
    }

    private void DisplayMethods()
    {
        var methodCount = _allMethods.Length;

        if (_methodFoldouts == null || _methodFoldouts.Length != methodCount)
        {
            _methodFoldouts = new bool[methodCount];
        }

        for (var i = 0; i < methodCount; i++)
        {
            var currentMethodInfo = _allMethods[i];

            GUILayout.FlexibleSpace();

            _methodFoldouts[i] = EditorGUILayout.Foldout(_methodFoldouts[i], currentMethodInfo.Name +":", EditorStyles.foldoutHeader);
            if (_methodFoldouts[i])
            {
                EditorGUILayout.BeginVertical();

                var parameters = currentMethodInfo.GetParameters();
                var argList = new List<object>();
                foreach (var parameter in parameters)
                {
                    EditorGUILayout.LabelField(parameter.Name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Type: " + parameter.ParameterType.FullName);
                    var obj = _invokerService?.GetParameterTypes(parameter);
                    if (obj != null)
                        argList.Add(obj);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Invoke", GUILayout.MinWidth(500), GUILayout.Width(100)))
                    _invokerService?.Invoke(currentMethodInfo.Name, argList.ToArray());
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Separator();
            }
        }
    }

    private void OnValidate()
    {
        if (_targetComponent == null)
            return;
        
        _invokerService = new InvokerService(_targetComponent);
        _allMethods = _invokerService.Value.GetMethods();
    }
    
}