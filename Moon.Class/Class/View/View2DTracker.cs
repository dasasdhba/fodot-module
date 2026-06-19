using Godot;
using Microsoft.FSharp.Core;

namespace Moon.Class;

[GlobalClass]
public partial class View2DTracker : Marker2D
{
    [Export]
    public bool AutoCurrent { get ;set; }

    public bool IsCurrent()
    {
        var view = this.GetView2D();
        if (view == null || OptionModule.IsNone(view.TrackingItem)) return false;
        return view.TrackingItem.Value == this;
    }

    public void MakeCurrent()
    {
        this.GetView2D()?.TrackingItem = FSharpOption<CanvasItem>.Some(this);
    }

    public View2DTracker() : base()
    {
        Ready += () =>
        {
            if (AutoCurrent) MakeCurrent();
        };
    }
}