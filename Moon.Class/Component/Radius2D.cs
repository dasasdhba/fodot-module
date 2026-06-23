using Fodot.CSharp;
using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class Radius2D : Node2D
{
    [Export(PropertyHint.None, "radians_as_degrees,suffix:\u00B0/s")]
    public float Speed { get; set; } = Mathf.DegToRad(200f);

    [Export(PropertyHint.None, "suffix:px")]
    public Vector2 Radius { get; set; } = new(0f, 16f);

    [Export(PropertyHint.Range, "-360,360,0.1,radians_as_degrees")]
    public float Phase { get; set; }

    [Export]
    public bool Transposed { get; set; }

    [Export]
    public bool Disabled { get; set; }

    public enum Radius2DProcessCallback { Idle, Physics }

    [Export]
    public Radius2DProcessCallback ProcessCallback { get; set; }
        = Radius2DProcessCallback.Physics;
        
    public Vector2 Origin { get ;set; }

    public Radius2D() : base()
    {
        Ready += () =>
        {
            Origin = Position;
            this.AddPhysicsProcess(Update);
        };
    }

    private void Update(double delta)
    {
        Phase = Mathe.WrapAngle(Phase + Speed * (float)delta);
        var radius = Transposed ? Radius.Transpose() : Radius;
        Position = Origin + Vector2.Right.Rotated(Phase) * radius;
    }
    
}
