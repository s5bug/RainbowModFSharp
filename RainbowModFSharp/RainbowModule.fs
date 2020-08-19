namespace Celeste.Mod.RainbowFs

open Celeste
open Celeste.Mod
open FMOD.Studio
open Microsoft.Xna.Framework
open Monocle
open System
open System.Collections.Generic
open System.Linq
open System.Reflection
open System.Text
open System.Threading.Tasks
open Mono.Cecil.Cil

type RainbowModule() =
    inherit EverestModule()
    
    static let mutable instance: Option<RainbowModule> = None
    static member Instance with get(): Option<RainbowModule> = instance
    
    override this.Load() =
        match RainbowModule.Instance with
        | Some(x) -> Logger.Log(LogLevel.Warn, "rainbowmodfs", "Load called while instance already loaded!")
        | None -> instance <- Some this
        
        On.Celeste.PlayerHair.add_GetHairColor(On.Celeste.PlayerHair.hook_GetHairColor(RainbowModule.GetHairColor))
        
    override this.Unload() =
        match RainbowModule.Instance with
        | Some(x) -> instance <- None
        | None -> Logger.Log(LogLevel.Warn, "rainbowmodfs", "Unload called while no instance loaded!")

    override this.SettingsType: Type = typeof<RainbowModuleSettings>
    static member Settings with get(): Option<RainbowModuleSettings> = Option.map (fun (i: RainbowModule) -> i._Settings :?> _) RainbowModule.Instance
    
    override this.LoadSettings() =
        base.LoadSettings()
        match RainbowModule.Settings with
        | Some(settings) ->
            let updateLight = settings.FoxColorLight.A = byte 0
            let updateDark = settings.FoxColorDark.A = byte 0
            let update = updateLight || updateDark
            
            if updateLight then settings.FoxColorLight <- Color(0.8f, 0.5f, 0.05f, 1.0f)
            if updateDark then settings.FoxColorDark <- Color(0.1f, 0.05f, 0.0f, 1.0f)
            
            if update then this.SaveSettings()
        | None -> Logger.Log(LogLevel.Warn, "rainbowmodfs", "LoadSettings called but no Settings were available!")

    static member GetHairColor (orig: On.Celeste.PlayerHair.orig_GetHairColor) (self: PlayerHair) (index: int): Color =
        let colorOrig = orig.Invoke(self, index)
        if not (self.Entity :? Player) || self.GetSprite().Mode = PlayerSpriteMode.Badeline
            then colorOrig
            else
                invalidOp "Unimplemented"
