using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class MarkerFlipSync : Marker2D
{
    [ExportCategory("MarkerFlipSync")]
    [Export(PropertyHint.NodePathValidTypes, "Sprite2D, AnimatedSprite2D, Sprite3D, AnimatedSprite3D")]
    public NodePath AnimNode {  get; set; } = "..";
    
    public enum MarkerFlipSyncProcessCallback { Idle, Physics }
    
    [Export]
    public MarkerFlipSyncProcessCallback ProcessCallback { get; set; } 
        = MarkerFlipSyncProcessCallback.Physics;
    
    public bool FlipH { get; set; } = false;
    public bool FlipV { get; set; } = false;
    public Vector2 Origin { get; set; }
    
    public string[] _GetFScripts() => ["marker_flip_sync"];
}