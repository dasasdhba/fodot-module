namespace Moon.Script

open Fodot
open Moon.Class
open Moon.Module
open Moon.View

[<FScript(typeof<View2DHost>)>]
type private View2DHostScript(host : View2DHost) =
    do host.add_Ready (fun _ ->
        host
        |> View2D.tryGet
        |> Option.iter (fun v ->
            v.Region <-
                host
                |> Node.tryGetNode<View2DRect> host.RegionRect
                |> Option.map _.GetRect()
                |> Option.defaultValue host.Region
            v.Margin <- host.Margin
            v.Zoom <- host.Zoom
            v.MinZoom <- host.MinZoom
            v.Rotation <- host.Rotation
            v.SmoothPositionRate <-
                if host.SmoothEnabled then Some host.SmoothRate else None
            v.SmoothZoomRate <-
                if host.SmoothZoomEnabled then Some host.SmoothZoomRate else None
            v.SmoothRotationRate <-
                if host.SmoothRotEnabled then Some host.SmoothRotRate else None
            v.TrackingItem <-
                host.FollowItem |> Option.ofObj
            
            v.ForceUpdate ()
        )
    )

[<FScript(typeof<View2DSetting>)>]
type private View2DSettingScript(setting : View2DSetting) =
    let apply (v : View2D) =
        if setting.RegionOverride then
            let region =
                setting
                |> Node.tryGetNode<View2DRect> setting.RegionRect
                |> Option.map _.GetRect()
                |> Option.defaultValue setting.Region
            let time =
                if setting.RegionSmoothed then setting.RegionSmoothTime else 0.0
            v.ChangeRegion(region, time)
            
        if setting.FollowOverride then
            v.TrackingItem <- setting.FollowNode |> Option.ofObj
            
        if setting.MarginOverride then
            v.Margin <- setting.Margin
            
        if setting.ZoomOverride then
            v.Zoom <- setting.Zoom
            v.MinZoom <- setting.MinZoom

        if setting.RotationOverride then
            v.Rotation <- setting.Rotation

        if setting.SmoothRateOverride then
            v.SmoothPositionRate <-
                if setting.SmoothEnabled then Some setting.SmoothRate else None

        if setting.SmoothZoomRateOverride then
            v.SmoothZoomRate <-
                if setting.SmoothZoomEnabled then Some setting.SmoothZoomRate else None

        if setting.SmoothRotRateOverride then
            v.SmoothRotationRate <-
                if setting.SmoothRotEnabled then Some setting.SmoothRotRate else None
            
        if setting.ForceUpdate then
            v.ForceUpdate ()
    
    let applyView () =
        setting
        |> View2D.tryGet
        |> Option.iter apply
    
    do setting.add_Applied applyView
    do if setting.AutoSetup then
        setting |> Node.whenReady applyView
