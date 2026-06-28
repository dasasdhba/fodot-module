using System;
using Fodot.CSharp;
using Godot;
using Moon.View;

namespace Moon;

/// <summary>
/// Useful function to get current view info.
/// </summary>
public static class View2DExtension
{

    public static View2D GetView2D(this Node node)
    {
        return View2D.tryGet(node).AsObj();
    }
    
    public static Rect2 GetCanvasRect(this Node node)
    {
        var viewport = node.GetViewport();
        var canvas = viewport.CanvasTransform;
        var topLeft = -canvas.Origin / canvas.Scale;
        var size = viewport.GetVisibleRect().Size / canvas.Scale;
                    
        return new(topLeft, size);
    }

    /// <summary>
    /// Return the current view rect.
    /// </summary>
    public static Rect2 GetViewRect(this Node node)
    {
        var view = node.GetView2D();
        return view?.CurrentRect ?? node.GetCanvasRect();
    }

    /// <summary>
    /// Whether the CanvasItem is in current view
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInView(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRect().Grow(eps).HasPoint(pos);
    }

    /// <summary>
    /// Whether the CanvasItem is in current view left
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewLeft(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRect().Position.X - eps <= pos.X;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view right
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRight(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRect().End.X + eps >= pos.X;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view top
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewTop(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRect().Position.Y - eps <= pos.Y;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view bottom
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewBottom(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRect().End.Y + eps >= pos.Y;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view with specific direction.
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="dir">The direction to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewDir(this CanvasItem item, Vector2 dir, float eps = 0f)
    {
        if (Math.Abs(dir.Y) >= Math.Abs(dir.X))
        {
            return dir.Y >= 0 ? item.IsInViewBottom(eps) : item.IsInViewTop(eps);
        }

        return dir.X >= 0 ? item.IsInViewRight(eps) : item.IsInViewLeft(eps);
    }
    
    /// <summary>
    /// Return the current view region
    /// </summary>
    public static Rect2 GetViewRegion(this Node node)
    {
        var view = node.GetView2D();
        return view?.Region ?? default;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view region
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRegion(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRegion().Grow(eps).HasPoint(pos);
    }

    /// <summary>
    /// Whether the CanvasItem is in current view region left
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRegionLeft(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRegion().Position.X - eps <= pos.X;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view region right
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRegionRight(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRegion().End.X + eps >= pos.X;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view region top
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRegionTop(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRegion().Position.Y - eps <= pos.Y;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view region bottom
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRegionBottom(this CanvasItem item, float eps = 0f)
    {
        var pos = Fodot.Module.CanvasItem.getGlobalPosition(item);
        return item.GetViewRegion().End.Y + eps >= pos.Y;
    }

    /// <summary>
    /// Whether the CanvasItem is in current view region with specific direction.
    /// </summary>
    /// <param name="item">The CanvasItem to query.</param>
    /// <param name="dir">The direction to query.</param>
    /// <param name="eps">Set positive to extend judging view, or negative to reduce.</param>
    public static bool IsInViewRegionDir(this CanvasItem item, Vector2 dir, float eps = 0f)
    {
        if (Math.Abs(dir.Y) >= Math.Abs(dir.X))
        {
            return dir.Y >= 0 ? item.IsInViewRegionBottom(eps) : item.IsInViewRegionTop(eps);
        }

        return dir.X >= 0 ? item.IsInViewRegionRight(eps) : item.IsInViewRegionLeft(eps);
    }
}