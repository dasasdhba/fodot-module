using Fodot.CSharp;
using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class Rotator : Node
{
    /// <summary>
    /// Default value is parent.
    /// </summary>
    [ExportCategory("Rotator")]
    [Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
    public NodePath RotateNode { get; set; } = "..";
    
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
    
    public Rotator() : base()
    {
        Ready += () =>
        {
            Parent = GetNodeOrNull<CanvasItem>(RotateNode);
            this.AddProcess(RotateProcess, ProcessCallback == RotatorProcessCallback.Physics);
        };
    }

    private void RotateProcess(double delta)
    {
        if (!IsInstanceValid(Parent) || Disabled) return;
        
        var rotation = Fodot.Module.CanvasItem.getRotation(Parent);
        rotation += (float)Mathf.DegToRad(Speed * delta) * (Flip ? -1 : 1);
        rotation = Mathf.Wrap(rotation, -float.Pi, float.Pi);
        Fodot.Module.CanvasItem.setRotation(rotation, Parent);
    }
}