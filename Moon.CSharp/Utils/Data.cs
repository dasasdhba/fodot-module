#pragma warning disable CS0618 // Type or member is obsolete

using System;
using Godot;

namespace Moon.Utils;

// better metadata for godot objects

public static class Data
{
    public static void SetData(this GodotObject obj, StringName tag, Variant value)
        => obj.SetMeta(tag, value);

    public static void SetData(this GodotObject obj, Rid rid, StringName tag, Variant value)
    {
        if (obj is TileMap tilemap)
        {
            var layer = tilemap.GetLayerForBodyRid(rid);
            var coord = tilemap.GetCoordsForBodyRid(rid);
            var data = tilemap.GetCellTileData(layer, coord);
            data.SetCustomData(tag, value);
            return;
        }

        if (obj is TileMapLayer tilelayer)
        {
            var coord = tilelayer.GetCoordsForBodyRid(rid);
            var data = tilelayer.GetCellTileData(coord);
            data.SetCustomData(tag, value);
            return;
        }
        
        obj.SetMeta(tag, value);
    }
    
    public static bool HasData(this GodotObject obj, StringName tag)
    {
        return Fodot.Core.GodotObject.hasMeta(tag, obj);
    }

    public static bool RemoveData(this GodotObject obj, StringName tag)
    {
        return Fodot.Core.GodotObject.removeMeta(tag, obj);
    }
    
    public static bool HasCustomData(this TileSet tileset, string tag)
    {
        for (int i = 0; i < tileset.GetCustomDataLayersCount(); i++)
        {
            if (tileset.GetCustomDataLayerName(i) == tag)
                return true;
        }
        
        return false;
    }
    
    public static void RemoveCustomData(this TileSet tileset, string tag)
    {
        for (int i = 0; i < tileset.GetCustomDataLayersCount(); i++)
        {
            if (tileset.GetCustomDataLayerName(i) == tag)
                tileset.RemoveCustomDataLayer(i);
        }
    }
    
    /// <summary>
    /// If the obj is TileMap or TileMapLayer, this method checks custom layer data instead.
    /// To check the metadata, using HasData Instead.
    /// </summary>
    public static bool HasTilesetData(this GodotObject obj, StringName tag)
    {
        if (obj is TileMap tilemap) return tilemap.TileSet.HasCustomData(tag);
        if (obj is TileMapLayer tilelayer) return tilelayer.TileSet.HasCustomData(tag);
        return obj.HasData(tag);
    }
    
    /// <summary>
    /// If the obj is TileMap or TileMapLayer, this method removes custom layer data instead.
    /// To remove the metadata, using RemoveData Instead.
    /// </summary>
    public static void RemoveTilesetData(this GodotObject obj, StringName tag)
    {
        if (obj is TileMap tilemap)
        {
            tilemap.TileSet.RemoveCustomData(tag);
            return;
        }

        if (obj is TileMapLayer tilelayer)
        {
            tilelayer.TileSet.RemoveCustomData(tag);
            return;
        }
        
        obj.RemoveData(tag);
    }

    public static T GetData<[MustBeVariant] T>(this GodotObject obj, StringName tag, T defaultValue = default)
    {
        return Fodot.Core.GodotObject.getMetaWithDefaultAs(tag, new Lazy<T>(() => defaultValue), obj);
    }
    
    public static T GetData<[MustBeVariant] T>(this GodotObject obj, Rid rid, StringName tag, T defaultValue = default)
    {
        if (obj is TileMap tilemap)
        {
            if (!tilemap.TileSet.HasCustomData(tag)) return defaultValue;
            
            var layer = tilemap.GetLayerForBodyRid(rid);
            var coord = tilemap.GetCoordsForBodyRid(rid);
            var data = tilemap.GetCellTileData(layer, coord);
            return data.GetCustomData(tag).As<T>();
        }

        if (obj is TileMapLayer tilelayer)
        {
            if (!tilelayer.TileSet.HasCustomData(tag)) return defaultValue;
        
            var coord = tilelayer.GetCoordsForBodyRid(rid);
            var data = tilelayer.GetCellTileData(coord);
            return data.GetCustomData(tag).As<T>();
        }
        
        return obj.GetData(tag, defaultValue);
    }
}