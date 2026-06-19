using Godot;
using Godot.Collections;

namespace Moon.Component;

// HINT: keep emitting for a long time may cause overflow
// this is due to layer issue, and we don't like to do sort operation like Nodes
// (which is slow)

[GlobalClass, Tool]
public partial class ShadowCaster2D : Node
{
    [Export]
    public bool Emitting { get; set; } = false;
    
    [Export(PropertyHint.Range, "0.001,4096,or_greater")]
    public double Interval { get; set; } = 0.06d;
    
    [Export(PropertyHint.Range, "0.001,4096,or_greater")]
    public double ShadowTime { get; set; } = 0.5d;
    
    /// <summary>
    /// Relative to ShadowItem's ZIndex.
    /// </summary>
    [Export]
    public int ZIndex { get; set; } = -10;
    
    [ExportGroup("ProcessSettings")]
    [Export]
    public bool ForceVisible { get; set; } = false;
    
    [Export]
    public bool DuplicateMaterial { get; set; } = false;
    
    public enum ShadowCaster2DProcessCallback { Idle, Physics }
    
    [Export]
    public ShadowCaster2DProcessCallback ProcessCallback { get; set; } 
        = ShadowCaster2DProcessCallback.Physics;
    
    [ExportGroup("Dependency")]
    [Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
    public NodePath Root { get; set; } = "..";
    
    /// <summary>
    /// Must be Sprite2D or AnimatedSprite2D.
    /// </summary>
    [Export]
    public Array<NodePath> ShadowItems { get; set; } = [];
    
#if DEBUG
    public override void _ValidateProperty(Dictionary property)
    {
        base._ValidateProperty(property);

        if (property["name"].AsStringName() == PropertyName.ShadowItems)
        {
            property["hint"] = (int)PropertyHint.TypeString;
            property["hint_string"] = $"{Variant.Type.NodePath:D}/{PropertyHint.NodePathValidTypes:D}:Sprite2D,AnimatedSprite2D";
        }
    }
#endif
}