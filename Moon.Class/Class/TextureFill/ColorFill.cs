using Godot;

namespace Moon.Class;

[GlobalClass, Tool]
#if DEBUG
public partial class ColorFill : NodeSize2D, ISerializationListener
#else
public partial class ColorFill : NodeSize2D
#endif
{
    [Export]
    public Color Color
    {
        get => _Color;
        set
        {
            _Color = value;
            QueueRedraw();
        }
    }
    
    private Color _Color = new(0f, 0f, 0f, 1f);

    public override void _Draw()
    {
        DrawRect(new (new(0f, 0f), Size), Color);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        
        QueueRedraw();
        SizeChanged += QueueRedraw;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        SizeChanged -= QueueRedraw;
    }

#if DEBUG
    
    public void OnBeforeSerialize()
    {
        SizeChanged -= QueueRedraw;
    }

    public void OnAfterDeserialize()
    {
        QueueRedraw();
        SizeChanged += QueueRedraw;
    }

#endif
}