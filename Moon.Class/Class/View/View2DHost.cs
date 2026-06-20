using Godot;
using Godot.Collections;

namespace Moon.Class;

[GlobalClass, Tool]
public partial class View2DHost : Node
{
    [Export(PropertyHint.NodePathValidTypes, "View2DRect")]
    public NodePath RegionRect
    {
        get => _RegionRect;
        set
        {
            _RegionRect = value;
            NotifyPropertyListChanged();
        }
    }
    private NodePath _RegionRect;
    
    [Export(PropertyHint.None, "suffix:px")]
    public Rect2 Region { get ;set; } = new(Vector2.Zero, new(512f, 288f));
    
    [ExportGroup("Transform")]
    [Export(PropertyHint.None, "suffix:px")]
    public Rect2 Margin { get ;set; } = new(Vector2.Zero, Vector2.Zero);

    [Export(PropertyHint.Link)]
    public Vector2 Zoom { get ;set; } = Vector2.One;

    [Export]
    public float MinZoom { get ;set; } = 1f;

    [Export(PropertyHint.Range, "-360,360,0.1,radians_as_degrees")]
    public float Rotation { get ; set; } = 0f;

    [ExportGroup("Smooth", "Smooth")]
    [Export]
    public bool SmoothEnabled { get ;set; }  = true;

    [Export]
    public double SmoothRate { get ;set; } = 20d;

    [Export]
    public bool SmoothZoomEnabled { get ;set; } = true;

    [Export]
    public double SmoothZoomRate { get ;set; } = 10d;

    [Export]
    public bool SmoothRotEnabled { get ;set; } = true;

    [Export]
    public double SmoothRotRate { get ;set; } = 10d;

    [ExportGroup("Follow")]
    [Export]
    public CanvasItem FollowItem { get ; set; }
    
#if DEBUG

    public override void _ValidateProperty(Dictionary property)
    {
        if (property["name"].AsStringName() == PropertyName.Region && GetNodeOrNull(RegionRect) != null)
        {
            property["usage"] = (uint)PropertyUsageFlags.ReadOnly;
        }
    }

#endif
}