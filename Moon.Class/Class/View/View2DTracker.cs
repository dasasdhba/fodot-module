using Godot;
using Microsoft.FSharp.Core;

namespace Moon.Class;

[GlobalClass]
public partial class View2DTracker : Marker2D
{
    [ExportGroup("AutoCurrent")]
    [Export(PropertyHint.GroupEnable)]
    public bool AutoCurrent { get ;set; }
    
    [Export]
    public bool ForceUpdate { get ;set; } = true;

    public bool IsCurrent()
    {
        var view = this.GetView2D();
        if (view == null || OptionModule.IsNone(view.TrackingItem)) return false;
        return view.TrackingItem.Value == this;
    }

    public void MakeCurrent()
    {
        var view = this.GetView2D();
        if (view != null) view.TrackingItem = FSharpOption<CanvasItem>.Some(this);
    }

    public View2DTracker() : base()
    {
        Ready += () =>
        {
            if (AutoCurrent)
            {
                MakeCurrent();
                if (ForceUpdate) this.GetView2D()?.ForceUpdate();
            }
        };
    }
}