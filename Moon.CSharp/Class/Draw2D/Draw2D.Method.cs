using System;
using System.Collections.Generic;
using Godot;

namespace Moon.Class;

public partial class Draw2D
{
    // drawing API used in DrawProcess call.

    public enum Draw2DBlendMode
    {
        Mix,
        Add,
        Sub,
        Mul,
        PremultAlpha
    }

    private Draw2DBlendMode BlendMode = Draw2DBlendMode.Mix;

    /// <summary>
    /// Blend mode will be overridden by custom material.
    /// </summary>
    public void SetBlendMode(Draw2DBlendMode blendMode) => BlendMode = blendMode;
    public void ResetBlendMode() => BlendMode = Draw2DBlendMode.Mix;

    private Dictionary<Draw2DBlendMode, Material> BlendMaterialMap = new()
    {
        { Draw2DBlendMode.Mix, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Mix }
        },
        { Draw2DBlendMode.Add, new CanvasItemMaterial() { 
            BlendMode = CanvasItemMaterial.BlendModeEnum.Add } 
        },
        { Draw2DBlendMode.Sub, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Sub } 
        },
        { Draw2DBlendMode.Mul, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.Mul } 
        },
        { Draw2DBlendMode.PremultAlpha, new CanvasItemMaterial() {
            BlendMode = CanvasItemMaterial.BlendModeEnum.PremultAlpha } 
        }
    };

    private Material DrawMaterial = null;

    /// <summary>
    /// Non-null material will override blend mode setting.
    /// </summary>
    public void SetDrawMaterial(Material material) => DrawMaterial = material;
    public void ResetDrawMaterial() => DrawMaterial = null;

    private Color DrawModulate = Colors.White;

    public void SetDrawModulate(Color modulate) => DrawModulate = modulate;
    public void SetDrawModulateRGB(Color modulate)
    {
        DrawModulate.R = modulate.R;
        DrawModulate.G = modulate.G;
        DrawModulate.B = modulate.B;
    }
    public void SetDrawModulateAlpha(float alpha) => DrawModulate.A = alpha;
    public void ResetDrawModulate() => DrawModulate = Colors.White;
    
    private Color DrawSelfModulate = Colors.White;
    public void SetDrawSelfModulate(Color modulate) => DrawSelfModulate = modulate;
    public void SetDrawSelfModulateRGB(Color modulate)
    {
        DrawSelfModulate.R = modulate.R;
        DrawSelfModulate.G = modulate.G;
        DrawSelfModulate.B = modulate.B;
    }
    public void SetDrawSelfModulateAlpha(float alpha) => DrawSelfModulate.A = alpha;
    public void ResetDrawSelfModulate() => DrawSelfModulate = Colors.White;

    private Transform2D DrawTransform = new(0f, new Vector2(0f, 0f));
    private bool DrawGlobal = false;
    
    public void SetDrawGlobal(bool global) => DrawGlobal = global;
    public void SetDrawTransform(Transform2D transform) => DrawTransform = transform;
    public void ResetDrawTransform()
    {
        DrawTransform = Transform2D.Identity;
        DrawGlobal = false;
    }
    public void SetDrawPosition(Vector2 pos) => DrawTransform.Origin = pos;
    public void SetDrawRotation(float rotation) => DrawTransform = new(rotation, 
        DrawTransform.Scale, DrawTransform.Skew, DrawTransform.Origin);
    public void SetDrawScale(Vector2 scale) => DrawTransform = new(DrawTransform.Rotation,
        scale, DrawTransform.Skew, DrawTransform.Origin);
    public void SetDrawSkew(float skew) => DrawTransform = new(DrawTransform.Rotation,
        DrawTransform.Scale, skew, DrawTransform.Origin);

    private int DrawZIndex = 0;
    public void SetDrawZIndex(int zIndex) => DrawZIndex = zIndex;
    public void ResetDrawZIndex() => DrawZIndex = 0;
    
    private int DrawIndex = 0;
    public void SetDrawIndex(int index) => DrawIndex = index;
    public void ResetDrawIndex() => DrawIndex = 0;

    public void AddDrawingTask(Action<Rid> task)
    {
        var queuedMaterial = (DrawMaterial ?? BlendMaterialMap[BlendMode]).GetRid();
        var queuedModulate = DrawModulate;
        var queuedSelfModulate = DrawSelfModulate;
        int queuedZIndex = DrawZIndex;
        var queuedIndex = DrawIndex;
        
        var queuedTransform = DrawTransform;
        var scale = queuedTransform.Scale;
        var position = queuedTransform.Origin;
        
        if (FlipH)
        {
            scale.X *= -1f;
            position.X *= -1f;
        }

        if (FlipV)
        {
            scale.Y *= -1f;
            position.Y *= -1f;
        }
        
        queuedTransform = new(queuedTransform.Rotation, scale,
            queuedTransform.Skew, position + Offset);

        if (DrawGlobal)
        {
            queuedTransform = GlobalTransform.AffineInverse() * queuedTransform;
        }

        QueuedDrawingTasks.Add(drawer =>
        {
            RenderingServer.CanvasItemSetMaterial(drawer, queuedMaterial);
            RenderingServer.CanvasItemSetModulate(drawer, queuedModulate);
            RenderingServer.CanvasItemSetSelfModulate(drawer, queuedSelfModulate);
            RenderingServer.CanvasItemSetZIndex(drawer, queuedZIndex);
            RenderingServer.CanvasItemSetDrawIndex(drawer, queuedIndex);
            RenderingServer.CanvasItemSetTransform(drawer, queuedTransform);
            
            task.Invoke(drawer);
        });
    }

    // line

    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawLine(Vector2, Vector2, Color, float, bool)"/>
    /// </summary>
    public void QueuedDrawLine(Vector2 from, Vector2 to, Color color, 
        float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddLine(drawer, from, to, color, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawMultiline(Vector2[], Color, float)"/>
    /// </summary>
    public void QueuedDrawMultiline(Vector2[] points, Color color, float width = -1f, bool antialiased = false)
    {
        var colors = new Color[points.Length - 1];
        Array.Fill(colors, color);
        
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddMultiline(drawer, points, colors, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawMultilineColors(Vector2[], Color[], float)"/>
    /// </summary>
    public void QueuedDrawMultilineColors(Vector2[] points, Color[] colors, float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddMultiline(drawer, points, colors, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPolyline(Vector2[], Color, float, bool)"/>
    /// </summary>
    public void QueuedDrawPolyline(Vector2[] points, Color color,
        float width = -1f, bool antialiased = false)
    {
        var colors = new Color[points.Length];
        Array.Fill(colors, color);
    
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddPolyline(drawer, points, colors, width, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPolylineColors(Vector2[], Color[], float, bool)"/>
    /// </summary>
    public void QueuedDrawPolylineColors(Vector2[] points, Color[] colors,
        float width = -1f, bool antialiased = false)
    {
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddPolyline(drawer, points, colors, width, antialiased);
        });
    }

    // shape
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawArc(Vector2, float, float, float, int, Color, float, bool)"/>
    /// </summary>
    public void QueuedDrawArc(Vector2 center, float radius, float startAngle, float endAngle,
        Color color, int pointCount = 128, float width = -1f, bool antialiased = false)
    {
        var points = new Vector2[pointCount];
        
        var deltaAngle = Math.Clamp(endAngle - startAngle, -Mathf.Pi, Mathf.Pi);
        for (int i = 0; i < pointCount; i++)
        {
            var theta = (i / (pointCount - 1f)) * deltaAngle + startAngle;
            points[i] = center + new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * radius;
        }
    
        QueuedDrawPolyline(points, color, width, antialiased);
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawCircle(Vector2, float, Color)"/>
    /// </summary>
    public void QueuedDrawCircle(Vector2 center, float radius, Color color, bool anitiliased = false)
    {
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddCircle(drawer, center, radius, color, anitiliased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawRect(Rect2, Color, bool, float)"/>
    /// </summary>
    public void QueuedDrawRect(Rect2 rect, Color color, bool antialiased = false)
    {
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddRect(drawer, rect, color, antialiased);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPolygon(Vector2[], Color[], Vector2[], Texture2D)"/>
    /// </summary>
    public void QueuedDrawPolygon(Vector2[] points, Color[] colors,
        Vector2[] uvs = null, Texture2D texture = null)
    {
        var texId = texture?.GetRid() ?? default;
        
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddPolygon(drawer, points, colors, uvs, texId);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawColoredPolygon(Vector2[], Color, Vector2[], Texture2D)"/>
    /// </summary>
    public void QueuedDrawColoredPolygon(Vector2[] points, Color color,
        Vector2[] uvs = null, Texture2D texture = null)
    {
        var colors = new Color[points.Length];
        Array.Fill(colors, color);
        
        QueuedDrawPolygon(points, colors, uvs, texture);
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawPrimitive(Vector2[], Color[], Vector2[], Texture2D)"/>
    /// </summary>
    public void QueuedDrawPrimitive(Vector2[] points, Color[] colors,
        Vector2[] uvs = null, Texture2D texture = null)
    {
        var texId = texture?.GetRid() ?? default;
    
        AddDrawingTask((drawer) =>
        {
            RenderingServer.CanvasItemAddPrimitive(drawer, points, colors, uvs, texId);
        });
    }

    // texture
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawTexture(Texture2D, Vector2, Color?)"/>
    /// </summary>
    public void QueuedDrawTexture(Texture2D texture, Vector2 pos, Color? modulate = null)
    {
        if (texture == null) return;
        
        if (Centered)
        {
            var texSize = new Vector2(texture.GetWidth(), texture.GetHeight());
            pos -= texSize / 2f;
        }
        
        AddDrawingTask((drawer) =>
        {
            texture.Draw(drawer, pos, modulate, false);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawTextureRect(Texture2D, Rect2, bool, Color?, bool)"/>
    /// </summary>
    public void QueuedDrawTextureRect(Texture2D texture, Rect2 rect, bool tile,
        Color? modulate = null, bool transpose = false)
    {
        if (texture == null) return;
        
        AddDrawingTask((drawer) =>
        {
            texture.DrawRect(drawer, rect, tile, modulate, transpose);
        });
    }
    
    /// <summary>
    /// <inheritdoc cref="Godot.CanvasItem.DrawTextureRectRegion(Texture2D, Rect2, Rect2, Color?, bool, bool)"/>
    /// </summary>
    public void QueuedDrawTextureRectRegion(Texture2D texture, Rect2 rect, Rect2 srcRect,
        Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        if (texture == null) return;

        AddDrawingTask((drawer) =>
        {
            texture.DrawRectRegion(drawer, rect, srcRect, modulate, transpose, clipUV);
        });
    }

    // sprite frames
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTexture"/>
    /// </summary>
    public void QueuedDrawSpriteFrames(SpriteFrames spr, string animation, int frame, 
        Vector2 pos, Color? modulate = null)
    {
        var texture = spr.GetFrameTexture(animation, frame);
        QueuedDrawTexture(texture, pos, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRect"/>
    /// </summary>
    public void QueuedDrawSpriteFramesRect(SpriteFrames spr, string animation, int frame,
        Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
    {
        var texture = spr.GetFrameTexture(animation, frame);
        QueuedDrawTextureRect(texture, rect, tile, modulate, transpose);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRectRegion"/>
    /// </summary>
    public void QueuedDrawSpriteFramesRectRegion(SpriteFrames spr, string animation, int frame,
        Rect2 rect, Rect2 srcRect, Color? modulate = null, 
        bool transpose = false, bool clipUV = true)
    {
        var texture = spr.GetFrameTexture(animation, frame);
        QueuedDrawTextureRectRegion(texture, rect, srcRect, modulate, transpose, clipUV);
    }

    // sprite2d
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTexture"/>
    /// </summary>
    public void QueuedDrawSprite(Sprite2D spr, Vector2 pos, Color? modulate = null)
    {
        pos += spr.Offset;
        QueuedDrawTexture(spr.Texture, pos, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRect"/>
    /// </summary>
    public void QueuedDrawSpriteRect(Sprite2D spr, Rect2 rect, bool tile,
        Color? modulate = null, bool transpose = false)
    {
        QueuedDrawTextureRect(spr.Texture, rect, tile, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawTextureRectRegion"/>
    /// </summary>
    public void QueuedDrawSpriteRectRegion(Sprite2D spr, Rect2 rect, Rect2 srcRect,
        Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        QueuedDrawTextureRectRegion(spr.Texture, rect, srcRect, modulate, transpose, clipUV);
    }

    // animated sprite
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawSpriteFrames"/>
    /// </summary>
    public void QueuedDrawAnimatedSprite(AnimatedSprite2D spr, Vector2 pos, Color? modulate = null)
    {
        pos += spr.Offset;
        QueuedDrawSpriteFrames(spr.SpriteFrames, spr.Animation, spr.Frame, pos, modulate);
    }
    
    /// <summary>
    /// <inheritdoc cref="QueuedDrawSpriteFramesRect"/>
    /// </summary>
    public void QueuedDrawAnimatedSpriteRect(AnimatedSprite2D spr, Rect2 rect, bool tile,
        Color? modulate = null, bool transpose = false)
    { 
        QueuedDrawSpriteFramesRect(spr.SpriteFrames, spr.Animation, spr.Frame, 
            rect, tile, modulate, transpose);
    }

    /// <summary>
    /// <inheritdoc cref="QueuedDrawSpriteFramesRectRegion"/>
    /// </summary>
    public void QueuedDrawAnimatedSpriteRectRegion(AnimatedSprite2D spr, Rect2 rect, Rect2 srcRect,
        Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        QueuedDrawSpriteFramesRectRegion(spr.SpriteFrames, spr.Animation, spr.Frame,
            rect, srcRect, modulate, transpose, clipUV);
    }
}