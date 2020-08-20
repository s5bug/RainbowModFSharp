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
        On.Celeste.PlayerHair.add_GetHairColor (this.GetHairColorHook)
        On.Celeste.Player.add_GetTrailColor (this.GetTrailColorHook)
        On.Celeste.PlayerHair.add_GetHairTexture (this.GetHairTextureHook)
        On.Celeste.PlayerHair.add_Render (this.RenderHairHook)
        On.Celeste.Player.add_Render (this.RenderPlayerHook)
        On.Celeste.PlayerSprite.add_Render (this.RenderPlayerSpriteHook)

    override this.Unload() =
        On.Celeste.PlayerHair.remove_GetHairColor (this.GetHairColorHook)
        On.Celeste.Player.remove_GetTrailColor (this.GetTrailColorHook)
        On.Celeste.PlayerHair.remove_GetHairTexture (this.GetHairTextureHook)
        On.Celeste.PlayerHair.remove_Render (this.RenderHairHook)
        On.Celeste.Player.remove_Render (this.RenderPlayerHook)
        On.Celeste.PlayerSprite.remove_Render (this.RenderPlayerSpriteHook)

    override this.SettingsType: Type = typeof<RainbowModuleSettings>
    member this.Settings: RainbowModuleSettings = this._Settings :?> _

    member val private trailIndex = 0 with get, set

    member val private FoxBangs: System.Collections.Generic.List<MTexture> = System.Collections.Generic.List<MTexture>() with get, set

    member val private FoxHair: System.Collections.Generic.List<MTexture> = System.Collections.Generic.List<MTexture>() with get, set

    member val private Skateboard: MTexture = Unchecked.defaultof<MTexture> with get, set
    static member SkateboardPlayerOffset = Vector2(0.0f, -3.0f)

    member val private Dab: MTexture = Unchecked.defaultof<MTexture> with get, set
    static member DabPlayerOffset = Vector2(0.0f, -5.0f)

    override this.LoadSettings() =
        base.LoadSettings()
        let settings = this.Settings
        let updateLight = settings.FoxColorLight.A = byte 0
        let updateDark = settings.FoxColorDark.A = byte 0
        let update = updateLight || updateDark

        if updateLight
        then settings.FoxColorLight <- Color(0.8f, 0.5f, 0.05f, 1.0f)
        if updateDark
        then settings.FoxColorDark <- Color(0.1f, 0.05f, 0.0f, 1.0f)

        if update then this.SaveSettings()

    override this.LoadContent(firstLoad: bool) =
        this.FoxBangs <- GFX.Game.GetAtlasSubtextures("characters/player/foxbangs")
        this.FoxHair <- GFX.Game.GetAtlasSubtextures("characters/player/foxhair")
        this.Skateboard <- GFX.Game.["characters/player/skateboard"]
        this.Dab <- GFX.Game.["characters/player/dab"]

    member this.GetHairColor (orig: On.Celeste.PlayerHair.orig_GetHairColor) (self: PlayerHair) (index: int): Color =
        let colorOrig = orig.Invoke(self, index)
        if not (self.Entity :? Player)
           || self.GetSprite().Mode = PlayerSpriteMode.Badeline then
            colorOrig
        else
            let settings = this.Settings
            let mutable color = colorOrig

            if settings.FoxEnabled then
                if index % 2 = 0 then
                    let colorFox = settings.FoxColorLight
                    color <-
                        Color
                            ((float32 color.R / 255.0f)
                             * 0.1f
                             + (float32 colorFox.R / 255.0f) * 0.9f,
                             (float32 color.G / 255.0f)
                             * 0.05f
                             + (float32 colorFox.G / 255.0f) * 0.95f,
                             (float32 color.B / 255.0f)
                             * 0.2f
                             + (float32 colorFox.B / 255.0f) * 0.8f,
                             float32 color.A / 255.0f)
                else
                    let colorFox = settings.FoxColorDark
                    color <-
                        Color
                            ((float32 color.R / 255.0f)
                             * 0.1f
                             + (float32 colorFox.R / 255.0f) * 0.7f,
                             (float32 color.G / 255.0f)
                             * 0.1f
                             + (float32 colorFox.G / 255.0f) * 0.7f,
                             (float32 color.B / 255.0f)
                             * 0.1f
                             + (float32 colorFox.B / 255.0f) * 0.7f,
                             float32 color.A / 255.0f)

            if settings.RainbowEnabled then
                let wave =
                    self.GetWave()
                    * 60.0f
                    * settings.RainbowSpeedFactor

                let colorRainbow: Color =
                    RainbowModule.ColorFromHSV
                        ((((float32 index)
                           / (float32 (self.GetSprite().HairCount)))
                          * 180.0f)
                         + wave)
                        0.6f
                        1.0f

                color <-
                    Color
                        ((float32 color.R / 255.0f)
                         * 0.3f
                         + (float32 colorRainbow.R / 255.0f) * 0.7f,
                         (float32 color.G / 255.0f)
                         * 0.3f
                         + (float32 colorRainbow.G / 255.0f) * 0.7f,
                         (float32 color.B / 255.0f)
                         * 0.3f
                         + (float32 colorRainbow.B / 255.0f) * 0.7f,
                         float32 color.A / 255.0f)

            color

    member this.GetHairColorHook =
        On.Celeste.PlayerHair.hook_GetHairColor (this.GetHairColor)

    member this.GetTrailColor (orig: On.Celeste.Player.orig_GetTrailColor) (self: Player) (wasDashB: bool): Color =
        let settings = this.Settings
        if not settings.RainbowEnabled
           || self.Sprite.Mode = PlayerSpriteMode.Badeline
           || self.Hair = null then
            orig.Invoke(self, wasDashB)
        else
            let result =
                self.Hair.GetHairColor(this.trailIndex % self.Hair.GetSprite().HairCount)

            this.trailIndex <- this.trailIndex + 1
            result

    member this.GetTrailColorHook =
        On.Celeste.Player.hook_GetTrailColor (this.GetTrailColor)

    member this.GetHairTexture (orig: On.Celeste.PlayerHair.orig_GetHairTexture) (self: PlayerHair) (index: int)
                               : MTexture =
        let settings = this.Settings
        if not (self.Entity :? Player)
           || self.GetSprite().Mode = PlayerSpriteMode.Badeline
           || not settings.FoxEnabled then
            orig.Invoke(self, index)
        else if index = 0 then
            this.FoxBangs.[self.GetSprite().HairFrame]
        else
            this.FoxHair.[index % this.FoxHair.Count]

    member this.GetHairTextureHook =
        On.Celeste.PlayerHair.hook_GetHairTexture (this.GetHairTexture)

    member this.RenderHair (orig: On.Celeste.PlayerHair.orig_Render) (self: PlayerHair): unit =
        let settings = this.Settings
        if not settings.BaldelineEnabled then
            match self.Entity with
            | :? Player as player ->
                if self.GetSprite().Mode = PlayerSpriteMode.Badeline then
                    orig.Invoke(self)
                else
                    if settings.SkateboardEnabled then
                        for i = 0 to self.Nodes.Count - 1 do
                            self.Nodes.[i] <- self.Nodes.[i]
                                              + RainbowModule.SkateboardPlayerOffset
                    if settings.DuckToDabEnabled && player.Ducking then
                        for i = 0 to self.Nodes.Count - 1 do
                            self.Nodes.[i] <- self.Nodes.[i] + RainbowModule.DabPlayerOffset

                    if settings.WoomyEnabled then
                        let sprite = self.GetSprite()
                        // TODO figure out the HasHair thing

                        let woomyOffs = 3.0f
                        let woomyScaleMul = Vector2(0.7f, 0.7f)
                        let woomyScaleOffs = Vector2(-0.2f, -0.2f)

                        let origin = Vector2(5.0f, 5.0f)
                        let colorBorder = self.Border * self.Alpha

                        this.RenderHairPlayerOutline self

                        let mutable pos: Vector2 = Unchecked.defaultof<Vector2>
                        let mutable tex: MTexture = Unchecked.defaultof<MTexture>
                        let mutable color: Color = Unchecked.defaultof<Color>
                        let mutable scale: Vector2 = Unchecked.defaultof<Vector2>

                        self.Nodes.[0] <- self.Nodes.[0].Floor()

                        if colorBorder.A > byte 0 then
                            tex <- self.GetHairTexture(0)
                            scale <- self.GetHairScale(0)
                            pos <- self.Nodes.[0]
                            tex.Draw(pos + Vector2(-1.0f, 0.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(1.0f, 0.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(0.0f, -1.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(0.0f, 1.0f), origin, colorBorder, scale)

                            tex <- self.GetHairTexture(2)
                            scale <-
                                self.GetHairScale(sprite.HairCount - 2)
                                * woomyScaleMul
                                + woomyScaleOffs
                            pos <- self.Nodes.[0]
                            tex.Draw(pos + Vector2(-1.0f - woomyOffs, 0.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(1.0f - woomyOffs, 0.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(0.0f - woomyOffs, -1.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(0.0f - woomyOffs, 1.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(-1.0f + woomyOffs, 0.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(1.0f + woomyOffs, 0.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(0.0f + woomyOffs, -1.0f), origin, colorBorder, scale)
                            tex.Draw(pos + Vector2(0.0f + woomyOffs, 1.0f), origin, colorBorder, scale)

                            for i = 1 to sprite.HairCount - 1 do
                                tex <- self.GetHairTexture(i)
                                scale <-
                                    self.GetHairScale(sprite.HairCount - (i + 1))
                                    * woomyScaleMul
                                    + woomyScaleOffs * woomyScaleMul
                                    + woomyScaleOffs
                                pos <- self.Nodes.[i]
                                tex.Draw(pos + Vector2(-1.0f - woomyOffs, 0.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(1.0f - woomyOffs, 0.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(0.0f - woomyOffs, -1.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(0.0f - woomyOffs, 1.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(-1.0f + woomyOffs, 0.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(1.0f + woomyOffs, 0.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(0.0f + woomyOffs, -1.0f), origin, colorBorder, scale)
                                tex.Draw(pos + Vector2(0.0f + woomyOffs, 1.0f), origin, colorBorder, scale)

                        tex <- self.GetHairTexture(0)
                        color <- self.GetHairColor(0)
                        scale <- self.GetHairScale(0)
                        tex.Draw(self.Nodes.[0], origin, color, scale)

                        tex <- self.GetHairTexture(0)
                        color <- self.GetHairColor(0)
                        scale <-
                            self.GetHairScale(sprite.HairCount - 2)
                            * woomyScaleMul
                            + woomyScaleOffs
                        tex.Draw(self.Nodes.[0] + Vector2(-woomyOffs, 0.0f), origin, color, scale)
                        tex.Draw(self.Nodes.[0] + Vector2(woomyOffs, 0.0f), origin, color, scale)

                        for i = sprite.HairCount - 1 downto 1 do
                            tex <- self.GetHairTexture(i)
                            color <- self.GetHairColor(i)
                            scale <-
                                self.GetHairScale(sprite.HairCount - (i + 1))
                                * woomyScaleMul
                                + woomyScaleOffs
                            tex.Draw(self.Nodes.[i] + Vector2(-woomyOffs, 0.0f), origin, color, scale)
                            tex.Draw(self.Nodes.[i] + Vector2(woomyOffs, 0.0f), origin, color, scale)

                    else
                        orig.Invoke(self)

                    if settings.SkateboardEnabled then
                        for i = 0 to self.Nodes.Count - 1 do
                            self.Nodes.[i] <- self.Nodes.[i]
                                              - RainbowModule.SkateboardPlayerOffset
                    if settings.DuckToDabEnabled && player.Ducking then
                        for i = 0 to self.Nodes.Count - 1 do
                            self.Nodes.[i] <- self.Nodes.[i] - RainbowModule.DabPlayerOffset
            | _ -> orig.Invoke(self)

    member this.RenderHairHook =
        On.Celeste.PlayerHair.hook_Render (this.RenderHair)

    member this.RenderHairPlayerOutline(self: PlayerHair): unit =
        let sprite = self.GetSprite()
        if self.DrawPlayerSpriteOutline then
            let origin = sprite.Position
            let color = sprite.Color

            sprite.Color <- self.Border * self.Alpha

            sprite.Position <- origin + Vector2(0.0f, -1.0f)
            sprite.Render()
            sprite.Position <- origin + Vector2(0.0f, 1.0f)
            sprite.Render()
            sprite.Position <- origin + Vector2(-1.0f, 0.0f)
            sprite.Render()
            sprite.Position <- origin + Vector2(1.0f, 0.0f)
            sprite.Render()

            sprite.Color <- color
            sprite.Position <- origin
            
    member this.RenderPlayer (orig: On.Celeste.Player.orig_Render) (self: Player): unit =
        let renderPos = self.Sprite.RenderPosition
        let settings = this.Settings
        if settings.SkateboardEnabled then
            self.Sprite.RenderPosition <- self.Sprite.RenderPosition + RainbowModule.SkateboardPlayerOffset
        if settings.DuckToDabEnabled && self.Ducking then
            self.Sprite.RenderPosition <- self.Sprite.RenderPosition + RainbowModule.DabPlayerOffset
        
        orig.Invoke(self)
        
        if settings.SkateboardEnabled then
            this.Skateboard.Draw(
                                    renderPos.Floor() + Vector2((if self.Facing = Facings.Left then 9.0f else -8.0f), -4.0f),
                                    Vector2.Zero,
                                    Color.White,
                                    Vector2((if self.Facing = Facings.Left then -1.0f else 1.0f), 1.0f)
                                )
        
        if settings.SkateboardEnabled then
            self.Sprite.RenderPosition <- self.Sprite.RenderPosition - RainbowModule.SkateboardPlayerOffset
        if settings.DuckToDabEnabled && self.Ducking then
            self.Sprite.RenderPosition <- self.Sprite.RenderPosition - RainbowModule.DabPlayerOffset
            
    member this.RenderPlayerHook =
        On.Celeste.Player.hook_Render (this.RenderPlayer)
    
    member this.RenderPlayerSprite (orig: On.Celeste.PlayerSprite.orig_Render) (self: PlayerSprite): unit =
        let settings = this.Settings
        match self.Entity with
        | :? Player as player ->
             if self.Mode <> PlayerSpriteMode.Badeline && settings.DuckToDabEnabled && player.Ducking then
                this.Dab.Draw(
                                 self.RenderPosition.Floor() + Vector2((if player.Facing = Facings.Left then 6.0f else -6.0f), -7.0f),
                                 Vector2.Zero,
                                 Color.White,
                                 self.Scale
                             )
             else
                 orig.Invoke(self)
        | _ -> orig.Invoke(self)
    
    member this.RenderPlayerSpriteHook =
        On.Celeste.PlayerSprite.hook_Render (this.RenderPlayerSprite)

    static member ColorFromHSV (hue: float32) (saturation: float32) (value: float32): Color =
        let hi =
            int (Math.Floor(double (hue / 60.0f))) % 6

        let f =
            (hue / 60.0f)
            - float32 (Math.Floor(double (hue / 60.0f)))

        let nv = value * 255.0f
        let v = int (Math.Round(double nv))

        let p =
            int (Math.Round(double (nv * (1.0f - saturation))))

        let q =
            int (Math.Round(double (nv * (1.0f - (f * saturation)))))

        let t =
            int (Math.Round(double (nv * (1.0f - ((1.0f - f) * saturation)))))

        match hi with
        | 0 -> Color(v, t, p, 255)
        | 1 -> Color(q, v, p, 255)
        | 2 -> Color(p, v, t, 255)
        | 3 -> Color(p, q, v, 255)
        | 4 -> Color(t, p, v, 255)
        | _ -> Color(v, p, q, 255)
