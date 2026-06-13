using Fodot.CSharp;
using Fodot.Injection;
using Godot;
using Moon.CSharp;
using Moon.Interface;
using Moon.Library;

namespace Moon.Component;

[GlobalClass, ChildOf("CanvasItem")]
public partial class SpriteDir : Node, IFlipHInit
{
    /// <summary>
    /// The monitored moving node.
    /// </summary>
    [Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
    public NodePath TrackingNode { get ;set; }
    
    [Export(PropertyHint.NodePathValidTypes, "Rotator")]
    public NodePath RotatorNode { get ;set; }
    
    [Export]
    public bool Flip { get ;set; }
    
    [Export]
    public bool Disabled { get ;set; }
    
    private CanvasItem Root;
    private CanvasItem Sprite;
    private Rotator Rotator;
    
    private Recorder2D Recorder;
    
    public override void _EnterTree()
    {
        base._EnterTree();
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif
    
        Sprite = GetParentOrNull<CanvasItem>();
    }
    
    public override void _Ready()
    {
        base._Ready();
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif
    
        Root = GetNodeOrNull<CanvasItem>(TrackingNode);
        Rotator = GetNodeOrNull<Rotator>(RotatorNode);
        
        if (IsInstanceValid(Root)) Recorder = Recorder2DModule.get(Root);
        this.AddPhysicsProcess(Process);
    }

    public void InitFlipH()
    {
        this.WhenReady(() => SetSpriteFlip(true));
    }
    
    private void Process()
    {
        if (IsInstanceValid(Root))
        {
            var s = Recorder.LastMotion.X;
            if (s != 0f) SetSpriteFlip(s < 0f);
        }
    }

    protected void SetSpriteFlip(bool value)
    {
        if (Disabled) return;
        
        var result = Flip ? !value : value;
        Sprite?.TrySetFlipH(result);
        Rotator?.Flip = result;
    }
}