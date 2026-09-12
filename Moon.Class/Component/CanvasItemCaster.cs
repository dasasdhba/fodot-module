using Godot;

namespace Moon;

[GlobalClass]
public partial class CanvasItemCaster : Node
{
    [Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
    public NodePath Root { get; set; } = "..";

    [Export]
    public PackedScene ItemScene { get; set; }

    [Export(PropertyHint.None, "suffix:px")]
    public Vector2 Offset { get; set; }

    [Export]
    public int BufferCount { get; set; } = 1;

    [Signal]
    public delegate void CastedEventHandler(CanvasItem node);

    public AsyncScene<CanvasItem> ItemLoader { get; set; }

    public CanvasItemCaster() : base()
    {
        Ready += () =>
        {
            _root = GetNodeOrNull<CanvasItem>(Root);
            ItemLoader = AsyncScene.create<CanvasItem>(ItemScene, BufferCount, 0, this);
        };
    }

    private CanvasItem _root;

    public Node GetRoot() => _root;

    public void Cast()
    {
        if (_root == null)
        {
            FD.Print($"CanvasItemCaster {GetPath()}: Root is invalid.");
        }

        var node = ItemLoader.Get();
        CanvasItemModule.setPosition(
        CanvasItemModule.getPosition(_root) + Offset, node);
        EmitSignalCasted(node);
        _root.AddSiblingSafely(node);
    }
}
