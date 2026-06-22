using Godot;

namespace Moon.Class;

[GlobalClass, Tool]
#if DEBUG
public partial class GradientFill : NodeSize2D, ISerializationListener
#else
public partial class GradientFill : NodeSize2D
#endif
{
    [Export]
    public Gradient Gradient
    {
        get => _gradient;
        set
        {
            _gradient = value;
            QueueRedraw();
        }
    }
    private Gradient _gradient;

    [Export]
    public int Sample
    {
        get => _sample;
        set
        {
            _sample = value;
            QueueRedraw();
        }
    }
    private int _sample = 256;    
    
    public enum GradientMode { Horizontal, Vertical }

    [Export]
    public GradientMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            QueueRedraw();
        }
    }
    private GradientMode _mode = GradientMode.Vertical;

    [Export]
    public bool Flip
    {
        get => _flip;
        set
        {
            _flip = value;
            QueueRedraw();
        }
    }
    private bool _flip = false;
    
    private GradientTexture1D GradientTexture;

    public override void _Draw()
    {
        if (Gradient == null) return;
        
        GradientTexture ??= new();
        GradientTexture.Gradient = Gradient;
        GradientTexture.Width = Sample;
        var vertical = Mode == GradientMode.Vertical;
        
        var size = Size;
        if (Flip) size.X *= -1f;
        if (vertical) size = new Vector2(size.Y, size.X);
        
        DrawTextureRect(GradientTexture, new(Vector2.Zero, size), 
        false, null, vertical);
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