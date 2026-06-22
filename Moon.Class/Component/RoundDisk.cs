using Godot;
using Moon.Class;

namespace Moon.Component;

[GlobalClass, Tool]
#if DEBUG
public partial class RoundDisk : NodeSize2D, ISerializationListener
#else
public partial class RoundDisk : NodeSize2D
#endif
{
    [Export]
    public Color Color
    {
        get  => _Color;
        set
        {
            _Color = value;
            QueueRedraw();
        }    
    }
    private Color _Color = Colors.Black;
    
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

    public override void _Draw()
    {
        DrawEllipse(Size / 2f, Size.X / 2f, Size.Y / 2f, Color);
    }
}