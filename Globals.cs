using Godot;
using System;
using System.Collections.Generic;
public static class Globals
{
    public static bool IsValid<T>(this T node) where T : GodotObject
    {
        if (node == null)
            return false;
        return GodotObject.IsInstanceValid(node);
    }
    public static List<T> GetChildren<T>(this Node node)
    {
        List<T> result = new List<T>();
        for (int i = 0; i < node.GetChildCount(); i++)
            if (node.GetChild(i) is T)
                result.Add((T)(object)node.GetChild(i));
        return result;
    }

    /// <summary>
    /// Function for searching for children nodes of Type T.
    /// </summary>
    /// <returns>List of all instances of Type T that are children or lower.</returns>
    public static List<T> GetAllChildren<T>(this Node node)
    {
        List<T> list = new List<T>();
        list.AddRange(GetChildren<T>(node));
        for (int i = node.GetChildCount() - 1; i >= 0; i--)
            list.AddRange(GetAllChildren<T>(node.GetChild(i)));

        return list;
    }
    public static T FindParentOfType<T>(this Node node)
    {
        return FindParentOfTypeHelper<T>(node);
    }

    private static T FindParentOfTypeHelper<T>(this Node node)
    {
        if (node == null)
            return default(T);
        if (node is T)
        {
            return (T)(object)node;
        }
        else if (node == node.GetTree().Root)
        {
            return default(T);
        }
        else
        {
            return FindParentOfTypeHelper<T>(node.GetParent());
        }
    }


    public static string RemoveNamespace(string name)
    {
        int index = name.RFind(".");
        if (index < 0)
            return name;
        else
            return name.Substring(index + 1, name.Length - (index + 1));
    }
}