using Godot;

namespace Moon;

[GlobalClass]
public partial class PoolNode : Node
{
    [Export]
    public int PoolSize { get; set; } = 100;

    /// <summary>
    /// Make sure the pool objects can init when enter tree.
    /// </summary>
    [Export]
    public PackedScene PoolScene { get; set; }

    private NodePool pool;

    public PoolNode() : base()
    {
        Ready += () =>
        {
            pool = NodePoolModule.create(PoolScene, PoolSize, this);
        };
    }

    public Node GetPoolNode()
    {
        return pool.Get();
    }

    public T GetPoolNode<T>() where T : Node
    {
        return pool.GetAs<T>();
    }
}

public static class NodePoolExtensions
{
    /// <summary>
    /// if node is in pool, remove it from parent instead.
    /// </summary>
    public static bool TryQueueFree(this Node node)
    {
#if DEBUG
        if (Engine.IsEditorHint())
        {
            FD.PushWarning($"{node} namely {node.GetPathTo(node.GetTree().GetEditedSceneRoot())} is trying to call TryQueueFree in editor, which is not expected.");
            return false;
        }
#endif

        return NodePoolModule.returnPool(node);
    }
}
