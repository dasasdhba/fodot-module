using Fodot.CSharp;
using Godot;

namespace Moon.Component;

[GlobalClass]
public partial class MarkerFlipSync : Marker2D
{
    [ExportCategory("MarkerFlipSync")]
    [Export(PropertyHint.NodePathValidTypes, "Sprite2D, AnimatedSprite2D")]
    public NodePath AnimNode {  get; set; } = "..";
    
    public enum MarkerFlipSyncProcessCallback { Idle, Physics }
    
    [Export]
    public MarkerFlipSyncProcessCallback ProcessCallback { get; set; } 
        = MarkerFlipSyncProcessCallback.Physics;
    
    private Node2D Anim;
    public bool FlipH { get; set; } = false;
    public bool FlipV { get; set; } = false;
    public Vector2 Origin { get; set; }
    
    public override void _Ready()
    {
        Origin = Position;
        
        Anim = GetNode<Node2D>(AnimNode);
        Update();
        
        this.AddProcess(Update, ProcessCallback == MarkerFlipSyncProcessCallback.Physics);
    }

    public void Update()
    {
        if (Anim is Sprite2D spr)
        {
            FlipH = spr.FlipH;
            FlipV = spr.FlipV;
        }
        else if (Anim is AnimatedSprite2D anim)
        {
            FlipH = anim.FlipH;
            FlipV = anim.FlipV;
        }

        var pos = Origin;
        if (FlipH) pos.X *= -1f;
        if (FlipV) pos.Y *= -1f;

        Position = pos;
    }
}