using System;
using Godot;
using Godot.Collections;

namespace Moon.Class;

[GlobalClass, Tool]
public partial class View2DSetting : Node
{
    [Export]
    public bool AutoSetup { get; set; } = false;
    
    [Export]
    public bool ForceUpdate { get ;set; } = false;

    [Export]
    public bool RegionOverride
    {
        get => _RegionOverride;
        set
        {
            _RegionOverride = value;
            NotifyPropertyListChanged();
        }
    }

    private bool _RegionOverride = false;

    [Export(PropertyHint.NodePathValidTypes, "ViewRect2D")]
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
    
    [Export]
    public Rect2 Region { get ;set; } = new(Vector2.Zero, new(512f, 288f));

    [Export]
    public bool RegionSmoothed
    {
        get => _RegionSmoothed;
        set
        {
            _RegionSmoothed = value;
            NotifyPropertyListChanged();
        }
    }
    private bool _RegionSmoothed = true;

    [Export]
    public double RegionSmoothTime { get; set; } = 0.5d;

    [Export]
    public bool FollowOverride
    {
        get => _FollowOverride;
        set
        {
            _FollowOverride = value;
            NotifyPropertyListChanged();
        }
    }

    private bool _FollowOverride = false;

    [Export]
    public CanvasItem FollowNode { get ;set; }

    [ExportGroup("Transform")]
    [Export]
    public bool MarginOverride
    {
        get => _MarginOverride;
        set
        {
            _MarginOverride = value;
            NotifyPropertyListChanged();
        }
    }
    private bool _MarginOverride = false;

    [Export]
    public Rect2 Margin { get ;set; } = new(Vector2.Zero, Vector2.Zero);

    [Export]
    public bool ZoomOverride
    {
        get => _ZoomOverride;
        set
        {
            _ZoomOverride = value;
            NotifyPropertyListChanged();
        }
    }
    private bool _ZoomOverride = false;

    [Export(PropertyHint.Link)]
    public Vector2 Zoom { get ;set; } = Vector2.One;

    [Export]
    public float MinZoom { get ;set; } = 1f;

    [ExportGroup("Smooth", "Smooth")]
    [Export]
    public bool SmoothRateOverride
    {
        get => _SmoothRateOverride;
        set
        {
            _SmoothRateOverride = value;
            NotifyPropertyListChanged();
        }
    }
    private bool _SmoothRateOverride = false;

    [Export]
    public bool SmoothEnabled { get ;set; } = true;

    [Export]
    public double SmoothRate { get ;set; } = 20d;

    [Export]
    public bool SmoothZoomRateOverride
    {
        get => _SmoothZoomRateOverride;
        set
        {
            _SmoothZoomRateOverride = value;
            NotifyPropertyListChanged();
        }
    }
    private bool _SmoothZoomRateOverride = false;

    [Export]
    public bool SmoothZoomEnabled { get ;set; } = true;

    [Export]
    public double SmoothZoomRate { get ;set; } = 10d;

    [Export]
    public bool SmoothRotRateOverride
    {
        get => _SmoothRotRateOverride;
        set
        {
            _SmoothRotRateOverride = value;
            NotifyPropertyListChanged();
        }
    }
    private bool _SmoothRotRateOverride = false;

    [Export]
    public bool SmoothRotEnabled { get ;set; } = true;

    [Export]
    public double SmoothRotRate { get ;set; } = 10d;
    
#if DEBUG
    public override void _ValidateProperty(Dictionary property)
    {
        var name = property["name"].AsStringName();
        if (
            (name == PropertyName.RegionRect && !RegionOverride) ||
            (name == PropertyName.Region && !RegionOverride) ||
            (name == PropertyName.Region && GetNodeOrNull(RegionRect) != null) ||
            (name == PropertyName.RegionSmoothed && !RegionOverride) ||
            (name == PropertyName.RegionSmoothTime && !RegionOverride) ||
            (name == PropertyName.RegionSmoothTime && !RegionSmoothed) ||
            (name == PropertyName.FollowNode && !FollowOverride) ||
            (name == PropertyName.Margin && !MarginOverride) ||
            (name == PropertyName.Zoom && !ZoomOverride) ||
            (name == PropertyName.MinZoom && !ZoomOverride) ||
            (name == PropertyName.SmoothEnabled && !SmoothRateOverride) ||
            (name == PropertyName.SmoothRate && !SmoothRateOverride) ||
            (name == PropertyName.SmoothZoomEnabled && !SmoothZoomRateOverride) ||
            (name == PropertyName.SmoothZoomRate && !SmoothZoomRateOverride) ||
            (name == PropertyName.SmoothRotEnabled && !SmoothRotRateOverride) ||
            (name == PropertyName.SmoothRotRate && !SmoothRotRateOverride)
            )
        {
            property["usage"] = (uint)PropertyUsageFlags.ReadOnly;
        }
    }
#endif    
    
    public static string[] _GetFScripts() => ["view_2d_setting"];
    
    public event Action Applied; 
    
    public void Apply()
    {
        Applied?.Invoke();
    }
}
