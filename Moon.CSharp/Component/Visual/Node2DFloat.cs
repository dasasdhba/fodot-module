using Godot;
using Moon.Utils;

namespace Moon.Component;

[GlobalClass]
public partial class Node2DFloat : Node2D
{
    [ExportCategory("Node2DFloat")]
    [Export]
    public float Speed { get ;set; } = 200f;
    
    [Export]
    public Vector2 Radius { get ;set; } = new Vector2(0f, 16f);
    
    [Export]
    public float Phase { get ;set; } = 0f;
    
    public Vector2 Origin { get ;set; }
    
    public Node2DFloat() : base()
    {
        TreeEntered += () =>
        {
            Origin = Position;
        };
        
        Ready += () =>
        {
            this.AddPhysicsProcess(delta =>
            {
                Phase += (float)(Speed * delta);
                Phase = Mathf.Wrap(Phase, -180f, 180f);
                Position = Origin + Radius
                    * Mathf.Sin(Mathf.DegToRad(Phase));
            });
        };
    }
}