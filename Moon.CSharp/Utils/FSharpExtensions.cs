using System;
using System.Threading;
using System.Threading.Tasks;
using Fodot.CSharp;
using Godot;
using Microsoft.FSharp.Core;

namespace Moon.Utils;

public static class FSharpExtensions
{
    // action

    public static Guid ActionRepeat(this Node node, double interval, Action action, bool physics = false)
    {
        return Module.Action.repeat(interval, action.AsFSharpFunc(), physics, node);
    }

    public static Guid ActionRepeatIdle(this Node node, double interval, Action action)
    {
        return node.ActionRepeat(interval, action, false);
    }
    
    public static Guid ActionRepeatPhysics(this Node node, double interval, Action action)
    {
        return node.ActionRepeat(interval, action, true);
    }

    public static Task<Unit> ActionDelay(this Node node, double interval, Action action, bool physics = false)
    {
        return Module.Action.delay(interval, action.AsFSharpFunc(), physics, node);
    }
    
    public static Task<Unit> ActionDelayIdle(this Node node, double interval, Action action)
    {
        return node.ActionDelay(interval, action, false);
    }
    
    public static Task<Unit> ActionDelayPhysics(this Node node, double interval, Action action)
    {
        return node.ActionDelay(interval, action, true);
    }
    
    public static Task<Unit> ActionDelay(this Node node, double interval, Action action, CancellationToken ct, bool physics = false)
    {
        return Module.Action.delayWith(ct, interval, action.AsFSharpFunc(), physics, node);
    }
    
    public static Task<Unit> ActionDelayIdle(this Node node, double interval, Action action, CancellationToken ct)
    {
        return node.ActionDelay(interval, action, ct, false);
    }
    
    public static Task<Unit> ActionDelayPhysics(this Node node, double interval, Action action, CancellationToken ct)
    {
        return node.ActionDelay(interval, action, ct, true);
    }
}