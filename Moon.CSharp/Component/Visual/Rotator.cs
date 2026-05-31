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
    [Export]
    public CanvasItem RotateNode { get; set; }
    
    [Export]
    public float Speed { get; set; } = 500f;
    
    [Export]
    public bool Flip { get; set; } = false;
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    public enum RotatorProcessCallback { Idle, Physics }
    
    [Export]
    public RotatorProcessCallback ProcessCallback { get; set; } = RotatorProcessCallback.Physics;

    public Rotator() : base()
    {
        TreeEntered += () =>
        {
            if (RotateNode == null && GetParent() is CanvasItem parent) RotateNode = parent;
        };
        
        Ready += () =>
        {
            this.AddProcess(RotateProcess, ProcessCallback == RotatorProcessCallback.Physics);
        };
    }

    private void RotateProcess(double delta)
    {
        if (!IsInstanceValid(RotateNode) || Disabled) return;
        
        var rotation = Fodot.Module.CanvasItem.getRotation(RotateNode);
        rotation += (float)Mathf.DegToRad(Speed * delta) * (Flip ? -1 : 1);
        rotation = Mathf.Wrap(rotation, -float.Pi, float.Pi);
        Fodot.Module.CanvasItem.setRotation(rotation, RotateNode);
    }
}