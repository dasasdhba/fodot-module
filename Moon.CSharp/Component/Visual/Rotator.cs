using Fodot.CSharp;
using Fodot.Injection;
using Godot;

namespace Moon.Component;

[GlobalClass, ChildOf("CanvasItem")]
public partial class Rotator : Node
{
    [Export]
    public float Speed { get; set; } = 500f;
    
    [Export]
    public bool Flip { get; set; } = false;
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    public enum RotatorProcessCallback { Idle, Physics }
    
    [Export]
    public RotatorProcessCallback ProcessCallback { get; set; } = RotatorProcessCallback.Physics;
    
    private CanvasItem Parent;
    
    public override void _EnterTree()
    {
        base._EnterTree();
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif
        
        Parent = GetParentOrNull<CanvasItem>();
    }

    public override void _Ready()
    {
        base._Ready();
    #if DEBUG
        if (Engine.IsEditorHint()) return;
    #endif
    
        this.AddProcess(RotateProcess, ProcessCallback == RotatorProcessCallback.Physics);
    }

    private void RotateProcess(double delta)
    {
        if (Disabled || !IsInstanceValid(Parent)) return;
        
        var rotation = Fodot.Module.CanvasItem.getRotation(Parent);
        rotation += (float)Mathf.DegToRad(Speed * delta) * (Flip ? -1 : 1);
        rotation = Mathf.Wrap(rotation, -float.Pi, float.Pi);
        Fodot.Module.CanvasItem.setRotation(rotation, Parent);
    }
}