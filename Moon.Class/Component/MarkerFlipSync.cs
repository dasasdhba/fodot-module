using Godot;
using Godot.Warning;

namespace Moon.Component;

[GlobalClass, ChildOf("Sprite2D, AnimatedSprite2D")]
public partial class MarkerFlipSync : Marker2D
{
    public enum MarkerFlipSyncProcessCallback { Idle, Physics }
    
    [Export]
    public MarkerFlipSyncProcessCallback ProcessCallback { get; set; } 
        = MarkerFlipSyncProcessCallback.Physics;
    
    public bool FlipH { get; set; } = false;
    public bool FlipV { get; set; } = false;
    public Vector2 Origin { get; set; }
}
