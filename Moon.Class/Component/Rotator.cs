using Fodot.Injection;
using Godot;

namespace Moon.Component;

[GlobalClass, ChildOf("CanvasItem")]
public partial class Rotator : Node
{
    [Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
    public float Speed { get; set; } = Mathf.DegToRad(500f);
    
    [Export]
    public bool Flip { get; set; } = false;
    
    [Export]
    public bool Disabled { get; set; } = false;
    
    public enum RotatorProcessCallback { Idle, Physics }
    
    [Export]
    public RotatorProcessCallback ProcessCallback { get; set; } = RotatorProcessCallback.Physics;
}