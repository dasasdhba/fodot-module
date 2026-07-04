using System;
using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class MarkerFlipSync : Marker2D
{
    public enum MarkerFlipSyncProcessCallback { Idle, Physics }
    
    [Export(PropertyHint.NodePathValidTypes, "Sprite2D,AnimatedSprite2D")]
    public NodePath Target { get ;set; } = "..";
    
    [Export]
    public MarkerFlipSyncProcessCallback ProcessCallback { get; set; } 
        = MarkerFlipSyncProcessCallback.Physics;
    
    public bool FlipH { get; set; } = false;
    public bool FlipV { get; set; } = false;
    public Vector2 Origin { get; set; }
    
    public event Action Updated;
    public void Update() => Updated?.Invoke();
}
