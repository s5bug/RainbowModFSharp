namespace Celeste.Mod.RainbowFs

open Celeste.Mod
open FMOD.Studio
open Microsoft.Xna.Framework
open Monocle
open System
open System.Collections
open System.Collections.Generic
open System.Linq
open System.Text
open System.Threading.Tasks
open YamlDotNet.Serialization

type RainbowModuleSettings() =
    inherit EverestModuleSettings()
    
    member val RainbowEnabled: bool = Unchecked.defaultof<bool> with get, set
    
    [<SettingRange(0, 20)>]
    member val RainbowSpeed: int = 10 with get, set
    [<YamlIgnore>]
    [<SettingIgnore>]
    member this.RainbowSpeedFactor = float32 this.RainbowSpeed / 20.0f
    
    member val FoxEnabled: bool = Unchecked.defaultof<bool> with get, set
    
    [<YamlMember(Alias = "FoxColorLight")>]
    [<SettingIgnore>]
    member this.FoxColorLightHex
        with get() = this.FoxColorLight.R.ToString("X2") + this.FoxColorLight.G.ToString("X2") + this.FoxColorLight.B.ToString("X2")
        and set(value: string) =
            if String.IsNullOrEmpty(value) then () else
                try this.FoxColorLight <- Calc.HexToColor(value)
                with
                    (e: Exception) ->
                        Logger.Log(LogLevel.Warn, "rainbowmodfs", "Invalid FoxColorLight!")
                        Logger.LogDetailed(e)

    [<YamlIgnore>]
    [<SettingIgnore>]
    member val FoxColorLight: Color = Unchecked.defaultof<Color> with get, set
    
    [<YamlMember(Alias = "FoxColorDark")>]
    [<SettingIgnore>]
    member this.FoxColorDarkHex
        with get() = this.FoxColorDark.R.ToString("X2") + this.FoxColorDark.G.ToString("X2") + this.FoxColorDark.B.ToString("X2")
        and set(value: string) =
            if String.IsNullOrEmpty(value) then () else
                try this.FoxColorDark <- Calc.HexToColor(value)
                with
                    (e: Exception) ->
                        Logger.Log(LogLevel.Warn, "rainbowmodfs", "Invalid FoxColorLight!")
                        Logger.LogDetailed(e)

    [<YamlIgnore>]
    [<SettingIgnore>]
    member val FoxColorDark: Color = Unchecked.defaultof<Color> with get, set

    member val WoomyEnabled: bool = Unchecked.defaultof<bool> with get, set
    
    member val SkateboardEnabled: bool = Unchecked.defaultof<bool> with get, set
    
    member val DuckToDabEnabled: bool = Unchecked.defaultof<bool> with get, set
    
    member val DuckToSneezeEnabled: bool = Unchecked.defaultof<bool> with get, set
    
    member val BaldelineEnabled: bool = Unchecked.defaultof<bool> with get, set
