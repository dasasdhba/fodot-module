using Godot;
using System;
using System.Collections.Generic;
using Fodot.CSharp;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Moon.Utils;

// useful node extension functions

public static class MoonExtensions
{
    #region CanvasItem

    /// <summary>
    /// Useful to draw atlas texture with tiled mode
    /// </summary>
    public static void DrawTextureRectTiled(this CanvasItem item, Texture2D texture,
        Rect2 rect, Color? modulate = null)
    {
        var size = texture.GetSize();
        item.DrawTextureRectRegionTiled(texture, rect, 
            new(Vector2.Zero, size), modulate);
    }
    
    /// <summary>
    /// Useful to draw atlas texture with tiled mode
    /// </summary>
    public static void DrawTextureRectRegionTiled(this CanvasItem item, Texture2D texture,
        Rect2 rect, Rect2 srcRect, Color? modulate = null)
    {
        var flipH = rect.Size.X * srcRect.Size.X < 0f;
        var flipV = rect.Size.Y * srcRect.Size.Y < 0f;
        rect = new Rect2(rect.Position, rect.Size.Abs());
        srcRect = new Rect2(srcRect.Position, srcRect.Size.Abs());
        
        var rx = rect.Size.X;
        var ry = rect.Size.Y;
        var ux = srcRect.Size.X;
        var uy = srcRect.Size.Y;
        if (ux <= 0f || uy <= 0f) return;
        
        var px = 0f;
        while (px < rx)
        {
            var py = 0f;
            while (py < ry)
            {
                var w = Math.Min(ux, rx - px);
                var h = Math.Min(uy, ry - py);
                var x = flipH ? ux - w : 0f;
                var y = flipV ? uy - h : 0f;
                var sRect = new Rect2(x, y ,w ,h);
                if (flipH) w *= -1f;
                if (flipV) h *= -1f;
                var rRect = new Rect2(rect.Position + new Vector2(px, py), w, h);
                item.DrawTextureRectRegion(texture, rRect, sRect, modulate);
                py += uy;
            }
            px += ux;
        }
    }

    public static Tween FadeIn(this CanvasItem item, double time)
    {
        item.Modulate = item.Modulate with { A = 0f };
        var t = item.CreatePhysicsTween();
        t.TweenProperty(item, "modulate:a", 1f, time);
        return t;
    }
    
    public static Tween FadeOut(this CanvasItem item, double time)
    {
        var t = item.CreatePhysicsTween();
        t.TweenProperty(item, "modulate:a", 0f, time);
        return t;
    }
    
    #endregion
    
    #region PhysicsBody
    
    public static bool IsOverlapping(this PhysicsBody2D body, Vector2 offset = default)
        => body.TestMove(
            body.GlobalTransform with { Origin = body.GlobalPosition + offset },
            Vector2.Zero
        );
        
    public static bool IsOverlapping(this PhysicsBody3D body, Vector3 offset = default)
        => body.TestMove(
            body.GlobalTransform with { Origin = body.GlobalPosition + offset },
            Vector3.Zero
        );
    
    #endregion
}