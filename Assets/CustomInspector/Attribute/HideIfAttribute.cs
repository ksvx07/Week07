using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class HideIfAttribute : PropertyAttribute
{
    public string Condition { get; private set; }

    public HideIfAttribute(string condition)
    {
        Condition = condition;
    }
}