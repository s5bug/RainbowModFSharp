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
    
    override this.Load() =
        On.Celeste.PlayerHair.add_GetHairColor(this.GetHairColorHook)
        On.Celeste.Player.add_GetTrailColor(this.GetTrailColorHook)
        
    override this.Unload() =
        On.Celeste.PlayerHair.remove_GetHairColor(this.GetHairColorHook)
        On.Celeste.Player.remove_GetTrailColor(this.GetTrailColorHook)

    override this.SettingsType: Type = typeof<RainbowModuleSettings>
    member this.Settings with get(): RainbowModuleSettings = this._Settings :?> _
    
    member val private trailIndex = 0 with get, set
    
    override this.LoadSettings() =
        base.LoadSettings()
        let settings = this.Settings
        let updateLight = settings.FoxColorLight.A = byte 0
        let updateDark = settings.FoxColorDark.A = byte 0
        let update = updateLight || updateDark
            
        if updateLight then settings.FoxColorLight <- Color(0.8f, 0.5f, 0.05f, 1.0f)
        if updateDark then settings.FoxColorDark <- Color(0.1f, 0.05f, 0.0f, 1.0f)
            
        if update then this.SaveSettings()

    member this.GetHairColor (orig: On.Celeste.PlayerHair.orig_GetHairColor) (self: PlayerHair) (index: int): Color =
        let colorOrig = orig.Invoke(self, index)
        if not (self.Entity :? Player) || self.GetSprite().Mode = PlayerSpriteMode.Badeline
            then colorOrig
            else
                let settings = this.Settings
                let mutable color = colorOrig
                
                if settings.FoxEnabled
                then
                    if index % 2 = 0
                    then
                        let colorFox = settings.FoxColorLight
                        color <- Color(
                                          (float32 color.R / 255.0f) * 0.1f + (float32 colorFox.R / 255.0f) * 0.9f,
                                          (float32 color.G / 255.0f) * 0.05f + (float32 colorFox.G / 255.0f) * 0.95f,
                                          (float32 color.B / 255.0f) * 0.2f + (float32 colorFox.B / 255.0f) * 0.8f,
                                          float32 color.A / 255.0f
                                      )
                    else
                        let colorFox = settings.FoxColorDark
                        color <- Color(
                                          (float32 color.R / 255.0f) * 0.1f + (float32 colorFox.R / 255.0f) * 0.7f,
                                          (float32 color.G / 255.0f) * 0.1f + (float32 colorFox.G / 255.0f) * 0.7f,
                                          (float32 color.B / 255.0f) * 0.1f + (float32 colorFox.B / 255.0f) * 0.7f,
                                          float32 color.A / 255.0f
                                      )
                
                if settings.RainbowEnabled
                then
                    let wave = self.GetWave() * 60.0f * settings.RainbowSpeedFactor
                    let colorRainbow: Color = RainbowModule.ColorFromHSV ((((float32 index) / (float32 (self.GetSprite().HairCount))) * 180.0f) + wave) 0.6f 1.0f
                    color <- Color(
                                      (float32 color.R / 255.0f) * 0.3f + (float32 colorRainbow.R / 255.0f) * 0.7f,
                                      (float32 color.G / 255.0f) * 0.3f + (float32 colorRainbow.G / 255.0f) * 0.7f,
                                      (float32 color.B / 255.0f) * 0.3f + (float32 colorRainbow.B / 255.0f) * 0.7f,
                                      float32 color.A / 255.0f
                                  )
                
                color
                
    member this.GetHairColorHook = On.Celeste.PlayerHair.hook_GetHairColor(this.GetHairColor)

    member this.GetTrailColor (orig: On.Celeste.Player.orig_GetTrailColor) (self: Player) (wasDashB: bool): Color =
        let settings = this.Settings
        if not settings.RainbowEnabled || self.Sprite.Mode = PlayerSpriteMode.Badeline || self.Hair = null
        then
            orig.Invoke(self, wasDashB)
        else
            let result = self.Hair.GetHairColor(this.trailIndex % self.Hair.GetSprite().HairCount)
            this.trailIndex <- this.trailIndex + 1
            result
        
    member this.GetTrailColorHook = On.Celeste.Player.hook_GetTrailColor(this.GetTrailColor)
    
    static member ColorFromHSV (hue: float32) (saturation: float32) (value: float32): Color =
        let hi = int (Math.Floor (double (hue / 60.0f))) % 6
        let f = (hue / 60.0f) - float32 (Math.Floor (double (hue / 60.0f)))
        
        let nv = value * 255.0f
        let v = int (Math.Round (double nv))
        let p = int (Math.Round (double (nv * (1.0f - saturation))))
        let q = int (Math.Round (double (nv * (1.0f - (f * saturation)))))
        let t = int (Math.Round (double (nv * (1.0f - ((1.0f - f) * saturation)))))
        
        match hi with
        | 0 -> Color(v, t, p, 255)
        | 1 -> Color(q, v, p, 255)
        | 2 -> Color(p, v, t, 255)
        | 3 -> Color(p, q, v, 255)
        | 4 -> Color(t, p, v, 255)
        | _ -> Color(v, p, q, 255)
