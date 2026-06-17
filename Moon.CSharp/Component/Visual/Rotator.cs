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
}