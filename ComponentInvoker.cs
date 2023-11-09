using System.Reflection;
using UnityEngine;

public class ComponentInvoker : MonoBehaviour
{
    [SerializeField] private Component component;
    [SerializeField] private BindingFlags bindingFlags;
    [SerializeField] private string selectedMethod;
} 