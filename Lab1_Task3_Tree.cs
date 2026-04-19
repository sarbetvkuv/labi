using System;
using System.Collections.Generic;

namespace Lab1.Task3;

public sealed class TreeNode<T>
{
    public T Value { get; }
    public IReadOnlyList<TreeNode<T>> Children => _children;

    private readonly List<TreeNode<T>> _children = new();

    public TreeNode(T value)
    {
        Value = value;
    }

    public void AddChild(TreeNode<T> child)
    {
        _children.Add(child);
    }

    public void PrintAllDescendants()
    {
        PrintInternal(this, 0);
    }

    private static void PrintInternal(TreeNode<T> node, int depth)
    {
        Console.WriteLine($"{new string(' ', depth * 2)}- {node.Value}");

        if (node._children.Count == 0)
        {
            return;
        }

        foreach (TreeNode<T> child in node._children)
        {
            PrintInternal(child, depth + 1);
        }
    }
}

public static class Lab1Task3Program
{
    public static void Main()
    {
        Console.WriteLine("=== Lab 1 / Task 3 (Tree) ===");

        var root = new TreeNode<string>("Root");

        var childA = new TreeNode<string>("A");
        childA.AddChild(new TreeNode<string>("A1"));
        childA.AddChild(new TreeNode<string>("A2"));

        var childB = new TreeNode<string>("B");
        var b1 = new TreeNode<string>("B1");
        b1.AddChild(new TreeNode<string>("B1a"));
        childB.AddChild(b1);

        root.AddChild(childA);
        root.AddChild(childB);
        root.AddChild(new TreeNode<string>("C"));

        root.PrintAllDescendants();
    }
}
