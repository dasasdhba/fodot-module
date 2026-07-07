using Godot;

namespace Moon.Class;

[GlobalClass, Tool]
#if DEBUG
public partial class DebugFill : NodeSize2D, ISerializationListener
#else
public partial class DebugFill : NodeSize2D
#endif
{
#if DEBUG
    [Export]
    public Color DebugColor
    {
        get => _DebugColor;
        set
        {
            _DebugColor = value;
            QueueRedraw();
        }
    }
    
    [Export]
    public bool ShowInGame { get ;set; }
    
    private Color _DebugColor = new(0f, 1f, 0f, 0.3f);

    public override void _Draw()
    {
        if (!Engine.IsEditorHint() && !ShowInGame) return;

        DrawRect(new (new(0f, 0f), Size), DebugColor);
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        if (!Engine.IsEditorHint() && !ShowInGame) return;
        
        QueueRedraw();
        SizeChanged += QueueRedraw;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (!Engine.IsEditorHint()) return;
        
        SizeChanged -= QueueRedraw;
    }
    
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