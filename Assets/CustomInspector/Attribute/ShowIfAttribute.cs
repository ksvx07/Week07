using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ShowIfAttribute : PropertyAttribute
{
    public string Condition { get; private set; }

    public ShowIfAttribute(string condition)
    {
        Condition = condition;
    }
}