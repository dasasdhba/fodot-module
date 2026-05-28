using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using GodotTask;
using Moon.Utils;

namespace Moon.Class;

/// <summary>
/// Inherit this node to implement your save values.
/// It's recommended to make it a singleton.
/// Multiple SaveNode is possible, but in which case you need to
/// handle multiple save / load calls.
/// For most games, a single SaveNode should be enough.
/// </summary>
public partial class SaveNode : Node
{

#if DEBUG

    [Export(PropertyHint.File, "*.cfg,*.ini")]
    public string DebugTable { get ;set; } = "";

#endif

    [Export]
    public string SectionKey { get ;set; } = "save";

    [Export]
    public string Password { get ;set; } = "";
    
    private Dictionary<string, Variant> DefaultValues = new();

    public SaveNode() : base()
    {
        Ready += () =>
        {
            DefaultValues = ExportDictionary();
            
        #if DEBUG
            var config = new ConfigFile();
            if (config.Load(DebugTable) == Error.Ok)
            {
                var debug = config.GetSection(SectionKey);
                ImportDictionary(debug);
            }
        #endif
        };
    }

    public Dictionary<string, Variant> ExportDictionary()
    {
        var result = new Dictionary<string, Variant>();
        foreach (var item in GetPropertyList())
        {
            var usageInt = item["usage"].AsInt32();
            var usage = (PropertyUsageFlags)usageInt;
            if (!usage.HasFlag(PropertyUsageFlags.ScriptVariable)) continue;
            
            var name = item["name"].AsString();
            if (name is "SectionKey" or "Password") continue;
            
        #if DEBUG
            if (name is "DebugTable") continue;
        #endif
        
            result.Add(name, Get(name));
        }
        return result;
    }
    
    public void ImportDictionary(Dictionary<string, Variant> dict)
    {
        foreach (var (name, value) in dict)
        {
            if (name is "SectionKey" or "Password") continue;
            
        #if DEBUG
            if (name is "DebugTable") continue;
        #endif    
        
            Set(name, value);
        }
    }
    
    public void Save(string file, string section)
    {
        var config = new ConfigFile();
        config.LoadEncryptedPass(file, Password);
        config.SetSection(section, ExportDictionary());
        config.SaveEncryptedPass(file, Password);
        FD.Print($"{this.GetUniquePath()} saved at {file} ({section})");
#if DEBUG
        if (Password != "") config.Save(file + ".cfg");
#endif    
    }
    
    public void Save(string file)
        => Save(file, SectionKey);
    
    public void Load(string file, string section)
    {
        var config = new ConfigFile();
        if (config.LoadEncryptedPass(file, Password) == Error.Ok)
        {
            FD.Print($"{this.GetUniquePath()} loaded from {file} ({section})");
            ImportDictionary(config.GetSection(section));
        }
        else
        {
            FD.Print($"{this.GetUniquePath()} loading failed from {file} ({section}), values are all reset");
            ImportDictionary(DefaultValues);
        }
    }
    
    public void Load(string file)
        => Load(file, SectionKey);

    public void Reset()
    {
        ImportDictionary(DefaultValues);
    }
    
    public async Task SaveAsync(string file, string section)
    {
        await GDTask.RunOnThreadPool(() => Save(file, section));
    }
    
    public Task SaveAsync(string file)
        => SaveAsync(file, SectionKey);
    
    public async Task LoadAsync(string file, string section)
    {
        await GDTask.RunOnThreadPool(() => Load(file, section));
    }
    
    public Task LoadAsync(string file)
        => LoadAsync(file, SectionKey);
}