using Godot;
using Godot.Collections;

public static class Node3DExtension
{
    public static void Reparent(this Node3D node, Node3D newParent)
    {
        Transform3D globalTransform = node.GlobalTransform;
        node.GetParent().RemoveChild(node);
        newParent.AddChild(node);
        node.GlobalTransform = globalTransform;
    }

    public static Array<Node> GetAllChildren(this Node node)
    {
        Array<Node> result = new Array<Node>();
        result.Add(node);
        int i = 0;
        while (i < result.Count)
        {
            result.AddRange(result[i].GetChildren());
            i++;
        }

        return result;
    }
}
