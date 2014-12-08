--[[
    Utiliteee beta
    by Weee

    If you like this work and you want to donate - I'll be glad to accept skin codes for any champs (EUNE)!
    Thanks :3
    
    Main Features:
        Auto Leveler
            Advanced auto leveler, which let's you to config the level-up queue on the fly. It uses sprites, so Utiliteee will try to automatically download this for you.
            You can edit the queue by opening scriptConfig menu (shift by default). You will see new interface.
            At the bottom part there will be 4 grey buttons for each spell: Q W E R. They stand for priority order, from left to right.
            Above this you will see a grid for each level (just like on most champion builds websites). Automatically assigned (from priority) "level-ups" will have light-grey color.
            If you want - you can set certain spells on your own. Manually assigned "level-ups" will have yellow color.
            Also can see what spell is going to be leveled up with your next level on your central HUD UI with green frame.
            You can re-assign it manually, by simply right-clicking the spell icon on your central HUD.
            WARNING: by default auto-level is turned off, so you have to enable it every script-reload or every level-up. Also be careful with editing the queue - it auto-saves and auto-loads for each champion.
            
        Laser Awareness
            Draws a bad ass laser from your champion to any incoming enemies from river/jungle. Useful, if you or your team wards pretty nice, but you still manage to miss incoming enemies (junglers/roamers).
            
        Gankeee (aka Predator's Vision)
            By holding a hotkey it will swap the vision between your team and enemy team. It allows you to sneak up to the lane as jungler/roamer for a succesful gank.
            
        Item Sorter
            Automatically sorts some items. I ported it from my old FPB version, so it doesn't support some items. I'll try to add some visual configurator later (something similar to auto-leveler).
        
        Tower Ranges        [ thanks SurfaceS ]
            Cropped from original SurfaceS similar script. Code is cleaned up.
        
        Where Did He Go     [ thanks ViceVersa ]
            My modification of the original Vice Versa's script. Cleaned up the code, ported to Utiliteee, fixed Vayne logic and added a text display of used blink spell.
            
        Little Mods (just some little tools, nothing special, but I like them):
            Smooth Movement
                When you hold your right mouse button it will smoothly move your champion.
            SelfSkillShots
                When you hold ALT and cast any spell (QWER) - it will try to cast it on your coordinates. If you're running it will cast all skillshots to your champion's back.
                If it's area spell - then it will simply cast it under your champion. I find it useful for Veigar's stun, Jayce's acceleration gate, traps, Teemo shrooms, etc.
            modStop
                Just a simply mod for hold position. I use "S" hotkey (default in-game stop) a lot, but it's not working with shift+s, ctrl+s, alt+s,
                unless you bind it separately, but game supports only up to 2 keybinds. I believe you can set more keybinds in game config, but I just decided to add this little tool.
]]

local __U
local __U_Drag = false
local floor, ceil, max, min = math.floor, math.ceil, math.max, math.min


function GetWebSpriteU(url, callback, folder)
    local urlr, sprite = url:reverse(), nil
    local filename, env = urlr:sub(1, urlr:find("/") - 1):reverse(), folder or GetCurrentEnv() and GetCurrentEnv().FILE_NAME and GetCurrentEnv().FILE_NAME:gsub(".lua", "") or "WebSprites"
    if FileExist(SPRITE_PATH .. env .. "\\" .. filename) then
        sprite = createSprite(env .. "\\" .. filename)
        if type(callback) == "function" then callback(sprite) end
    else
        if type(callback) == "function" then
            MakeSurePathExists(SPRITE_PATH .. env .. "\\" .. filename)
            DownloadFile(url, SPRITE_PATH .. env .. "\\" .. filename, function()
                if FileExist(SPRITE_PATH .. env .. "\\" .. filename) then
                    sprite = createSprite(env .. "\\" .. filename)
                end
                callback(sprite)
            end)
        else
            local finished = false
            sprite = GetWebSprite(url, function(data)
                finished = true
                sprite = data
            end)
            while not (finished or sprite or FileExist(SPRITE_PATH .. env .. "\\" .. filename)) do
                RunCmdCommand("ping 127.0.0.1 -n 1 -w 1")
            end
        end
        if not sprite and FileExist(SPRITE_PATH .. env .. "\\" .. filename) then
            sprite = createSprite(env .. "\\" .. filename)
        end
    end
    return sprite
end



function OnLoad()
    print("Utiliteee beta v0.3b [DON'T USE GANKEEE]")
    __U = Utiliteee()
end

function OnTick()
    __U:OnTick()
end

function OnDraw()
    __U:OnDraw()
end

function OnProcessSpell(unit, spell)
    __U:OnProcessSpell(unit, spell)
end


-- ################################################ Utiliteee ###################################################
class("Utiliteee")
function Utiliteee:__init()
    self.enemyMinions = minionManager(MINION_ENEMY, 50000, cameraPos)
    self.allyMinions = minionManager(MINION_ALLY, 50000, cameraPos)
    self.turrets = GetTurrets()
    self.turretTable = { [player.team] = {}, [TEAM_ENEMY] = {} }
    self.heroTable = self:BuildHeroTable()
    self.config = self:Config()
    self.scale = self:GetHUDScale()

    -- Sprites:
    self.sprites = {
        grid = {
            loadTry = 1, sprite = nil, mirrors = {
                "https://dl.dropboxusercontent.com/u/93477088/BoL/Scripts/Utiliteee/utiliteee-grid.png",
            }
        },
    }
    self.getWebSpriteTick = 0
    self.gotSprites = self:GetSprites()

    return self
end

function Utiliteee:Config()
    local config = scriptConfig("Utiliteee", "utiliteee")
    
    config:addSubMenu("Auto Leveler", "autoleveler")
        config.autoleveler:addParam("toggle", "Auto Leveler:", SCRIPT_PARAM_ONOFF, true)
        config.autoleveler:addParam("simpleMode", "Simple Mode:", SCRIPT_PARAM_ONOFF, false)
    
    config:addSubMenu("Laser Awareness", "laserawareness")
        config.laserawareness:addParam("toggle", "Laser Awareness:", SCRIPT_PARAM_ONOFF, true)
        config.laserawareness:addParam("range", "Awareness range:", SCRIPT_PARAM_SLICE, 5000, 3000, 7000, 0)
        config.laserawareness:addParam("laserColor", "Laser color:", SCRIPT_PARAM_COLOR, { 175, 255, 255, 50 })
    
    config:addSubMenu("Gankeee", "gankeee")
        config.gankeee:addParam("toggle", "Gankeee:", SCRIPT_PARAM_ONOFF, true)
        config.gankeee:addParam("pvSwapVision", "Swap vision while holding...", SCRIPT_PARAM_ONKEYDOWN, false, 16)
        config.gankeee:addParam("pvShowCircles", "Show vision range with circles too", SCRIPT_PARAM_ONOFF, false)
        config.gankeee:addParam("pvCircleColor", "Vision range circles color:", SCRIPT_PARAM_COLOR, { 255, 0, 20, 0 })
        config.gankeee.pvSwapVision = false
        config.gankeee.toggle = false   -- auto disabling gankeee due to 3.12 patch problems
    
    config:addSubMenu("Tower Ranges", "towerrange")
        config.towerrange:addParam("toggle", "Draw Tower Ranges:", SCRIPT_PARAM_ONOFF, true)
        config.towerrange:addParam("trCircleColor", "Tower range circles color:", SCRIPT_PARAM_COLOR, { 255, 150, 0, 0 })

    config:addSubMenu("Item Sorter", "itemsorter")
        config.itemsorter:addParam("toggle", "Item Sorter:", SCRIPT_PARAM_ONOFF, true)
    
    config:addSubMenu("Where Did He Go?", "WDHG")
        config.WDHG:addParam("toggle", "Where Did He Go?", SCRIPT_PARAM_ONOFF, true)
        config.WDHG:addParam("displayTime", "Display time", SCRIPT_PARAM_SLICE, 3, 1, 5, 0)
        config.WDHG:addParam("lineColor", "Line Color", SCRIPT_PARAM_COLOR, {255,255,50,50})
        config.WDHG:addParam("lineWidth", "Line Width", SCRIPT_PARAM_SLICE, 3, 1, 5, 0)
        config.WDHG:addParam("circleColor", "Circle Color", SCRIPT_PARAM_COLOR, {255,255,50,50})
        config.WDHG:addParam("circleSize", "Circle Size", SCRIPT_PARAM_SLICE, 100, 50, 300, 0)
    
    config:addSubMenu("Little Mods", "littlemods")
        config.littlemods:addParam("smoothMove", "Smooth Movement", SCRIPT_PARAM_ONOFF, true)
        config.littlemods:addParam("selfSkillShots", "SSS (SelfSkillShots):", SCRIPT_PARAM_ONOFF, true)
        config.littlemods:addParam("blankspace","", SCRIPT_PARAM_INFO, "")
        config.littlemods:addParam("modStop", "modSTOP: ", SCRIPT_PARAM_ONOFF, true)
        config.littlemods:addParam("modStopHK", "Your hold position hotkey:", SCRIPT_PARAM_ONKEYDOWN, false, GetKey("S"))
        config.littlemods.modStopHK = false
        
    return config
end

function Utiliteee:BuildHeroTable()
    local hM, heroTable = heroManager, { [player.team] = {}, [TEAM_ENEMY] = {} }
    for i = 1, hM.iCount do
        local hero = hM:GetHero(i)
        heroTable[hero.team][#heroTable[hero.team]+1] = hero
    end
    return heroTable
end

function Utiliteee:OnTick()
    -- SPRITES:
    if not self.gotSprites then self.gotSprites = self:GetSprites() end

    -- LASER AWARENESS:
    if self.config.laserawareness.toggle and GetGame().map.name == "Summoner's Rift" then
        if not self.LaserAwareness then
            self.LaserAwareness = U_LaserAwareness(self)
        end
        self.LaserAwareness:Update(self.config.laserawareness.range)
    else
        self.LaserAwareness = nil
    end

    -- AUTO LEVELER:
    if self.config.autoleveler.toggle then
        if self.gotSprites then
            if not self.AutoLeveler then
                local champTypes = {
                    Udyr = 1,
                    Jayce = 2,
                    Karma = 2,
                    Elise = 2,
                }
                self.AutoLeveler = U_AutoLeveler(self, champTypes[player.charName])
            end
            self.AutoLeveler:OnTick()
        end
    else
        self.AutoLeveler = nil
    end

    -- GANKEEE [PREDATOR'S VISION]:
    if self.config.gankeee.toggle and self.Gankeee and GetGame().map.name == "Summoner's Rift" then
        self.Gankeee:CleanUpMinions(self.enemyMinions.objects)
    end
    
    if self.config.gankeee.toggle then
        self.enemyMinions:update()
        self.allyMinions:update()
    end
    
    if self.config.gankeee.toggle or self.config.towerrange.toggle then
        self.turretTable = { [player.team] = {}, [TEAM_ENEMY] = {} }
        for i, turret in pairs(self.turrets) do self.turretTable[turret.team][#self.turretTable[turret.team]+1] = turret.object end
    end
    
    if self.config.gankeee.toggle and GetGame().map.name == "Summoner's Rift" then
        if not self.Gankeee then
            self.Gankeee = U_Gankeee(self)
        end
        self.Gankeee:OnTick()
    else
        self.Gankeee = nil
    end

    -- ITEM SORTER:
    if self.config.itemsorter.toggle then
        if not self.ItemSorter then
            self.ItemSorter = U_ItemSorter()
        end
        self.ItemSorter:OnTick()
    else
        self.ItemSorter = nil
    end
    
    -- SIMPLE ENEMY TOWER RANGE:
    if self.config.towerrange.toggle then
        if not self.TowerRange then
            self.TowerRange = U_TowerRange(self)
        end
        self.TowerRange:OnTick()
    else
        self.TowerRange = nil
    end
    
    -- WHERE DID HE GO:
    if self.config.WDHG.toggle then
        if not self.WDHG then
            self.WDHG = U_WhereDidHeGo(self)
        end
        self.WDHG:OnTick()
    else
        self.WDHG = nil
    end

    -- LITTLE MODS:
    if self.config.littlemods.selfSkillShots or self.config.littlemods.modStop then
        if not self.SSSmS then
            self.SSSmS = U_SSSmS(self)
        end
        self.SSSmS:OnTick()
    else
        self.SSSmS = nil
    end

    if self.config.littlemods.smoothMove then
        if not self.SmoothMove then
            self.SmoothMove = U_SmoothMove()
        end
        self.SmoothMove:OnTick()
    else
        self.SmoothMove = nil
    end
end

function Utiliteee:OnDraw()
    if self.config.autoleveler.toggle and self.AutoLeveler and self.gotSprites then self.AutoLeveler:OnDraw() end
    if self.config.gankeee.toggle and self.Gankeee then self.Gankeee:OnDraw() end
    if self.config.laserawareness.toggle and self.LaserAwareness and GetGame().map.name == "Summoner's Rift" then self.LaserAwareness:OnDraw(self.config.laserawareness.range) end
    if self.config.towerrange.toggle and self.TowerRange then self.TowerRange:OnDraw() end
    if self.config.WDHG.toggle and self.WDHG then self.WDHG:OnDraw() end
end

function Utiliteee:OnDeleteObj()
    if self.config.gankeee.toggle and self.Gankeee then self.Gankeee:OnDeleteObj() end
end

function Utiliteee:OnScreen(unit, offset)
    local offset = offset or 0
    local pos = WorldToScreen(D3DXVECTOR3(unit.x,unit.y,unit.z))
    return pos.x <= WINDOW_W + offset and pos.x >= 0 - offset and pos.y >= 0 - offset and pos.y <= WINDOW_H + offset
end

function Utiliteee:OnProcessSpell(unit, spell)
    if self.config.WDHG.toggle and self.WDHG then self.WDHG:OnProcessSpell(unit, spell) end
end

function Utiliteee:GetHUDScale()
    -- "cropped" from gReY's allclass GetMinimap stuff
    local gameSettings = GetGameSettings()
    local windowWidth, windowHeight = WINDOW_W, WINDOW_H
    if gameSettings and gameSettings.General and gameSettings.General.Width and gameSettings.General.Height then
        windowWidth, windowHeight = gameSettings.General.Width, gameSettings.General.Height
        local path = GAME_PATH.."DATA\\menu\\hud\\hud"..windowWidth.."x"..windowHeight..".ini"
        local hudSettings = ReadIni(path)
        if hudSettings and hudSettings.Globals and hudSettings.Globals.GlobalScale then
            return hudSettings.Globals.GlobalScale
        else
            print("GetHUDScale(): something is wrong with ReadIni(path)")
            return nil
        end
    else
        print("GetHUDScale(): something is wrong with GetGameSettings()")
        return nil
    end
end

function Utiliteee:GetSprites()
    local loadCount = 0
    local setTick = false
    for i, sprite in pairs(self.sprites) do
        if sprite.sprite ~= nil then
            loadCount = loadCount + 1
        elseif os.clock() >= self.getWebSpriteTick then
            setTick = true
            sprite.sprite = GetWebSpriteU(sprite.mirrors[sprite.loadTry], function(data) sprite.sprite = data end, "Utiliteee")
            sprite.loadTry = sprite.loadTry >= #sprite.mirrors and 1 or sprite.loadTry + 1
        end
    end
    if setTick then self.getWebSpriteTick = os.clock() + 1 end
    return loadCount >= 1
end
-- ################################################ Utiliteee ###################################################





-- ############################################## LaserAwareness ################################################
class("U_LaserAwareness")
function U_LaserAwareness:__init(U)
    require "MapPosition"
    self.donors = self:GetNexusTurrets()
    self.U = U
    self.MapPosition = MapPosition()
    self.updTick = 0
    return self
end

function U_LaserAwareness:GetNexusTurrets()
    local oM = objManager
    local laserDonors = {}
    for i=1, oM.maxObjects do
        local obj = oM:GetObject(i)
        if obj and obj.valid and obj.type == "obj_AI_Turret" and (obj.name == "Turret_OrderTurretShrine_A" or obj.name == "Turret_ChaosTurretShrine_A") then
            obj.spawnPos = obj.team == player.team and { x = -236, y = 183, z = -53 } or { x = 14157, y = 183, z = 14456 }
            obj.tick = 0
            laserDonors[#laserDonors+1] = obj
        end
        if #laserDonors >= 2 then break end
    end
    return laserDonors
end

function U_LaserAwareness:Update(range)
    --[[
    if GetInGameTimer() >= self.updTick then
        local donorsUsed = 0
        for i, hero in pairs(self.U.heroTable[TEAM_ENEMY]) do
            if not player.dead and hero and hero.valid and hero.visible and not hero.dead and not self.U:OnScreen(hero, 50)
            and (self.MapPosition:inInnerJungle(hero) or self.MapPosition:inInnerRiver(hero))
            and GetDistance(player, hero) <= range then
                donorsUsed = donorsUsed + 1
                local donor = self.donors[donorsUsed]
                donor:SetPosition(D3DXVECTOR3(hero.x, hero.y, hero.z))
                if GetInGameTimer() >= donor.tick then
                    donor.tick = GetInGameTimer() + 5
                    ClientSide:TowerFocus(donor, player)
                end
            end
            if donorsUsed == 2 then break end
        end
        if donorsUsed == 0 then
            for i, donor in pairs(self.donors) do
                if donor.pos ~= donor.spawnPos then
                    donor:SetPosition(D3DXVECTOR3(donor.spawnPos.x,donor.spawnPos.y,donor.spawnPos.z))
                    donor.tick = 0
                    ClientSide:TowerIdle(donor)
                end
            end
        end
        self.updTick = GetInGameTimer() + 0.1
    end
    ]]
end

function U_LaserAwareness:OnDraw(range)
    if player.dead then return end
    local displayCount = 0
    local c = self.U.config.laserawareness.laserColor
    for i, hero in pairs(self.U.heroTable[TEAM_ENEMY]) do
        if hero and hero.valid and hero.visible and not hero.dead and not self.U:OnScreen(hero, -50)
        and (self.MapPosition:inInnerJungle(hero) or self.MapPosition:inInnerRiver(hero))
        and GetDistance(player, hero) <= range then
            --DrawLine3D(player.x, player.y, player.z, hero.x, hero.y, hero.z, 1, ARGB(255,255,100,100))    -- usual sucky line
            local playerPos = Point(myHero.x, myHero.z)
            local heroPos = Point(hero.x, hero.z)
            local directionVector = (heroPos - playerPos):normalized()
            local infoPos = playerPos + directionVector * (500 + 200 * displayCount)
            infoPos = WorldToScreen(D3DXVECTOR3(infoPos.x, player.y, infoPos.y))
            local startPos = playerPos + directionVector * 200
            DrawLine3D(startPos.x, player.y, startPos.y, hero.x, player.y, hero.z, 7, ARGB(100 * GetDrawClock(math.random(0,2)),255,255,255))
            DrawLine3D(startPos.x, player.y, startPos.y, hero.x, player.y, hero.z, 5, ARGB(100,c[2],c[3],c[4]))
            DrawLine3D(startPos.x, player.y, startPos.y, hero.x, player.y, hero.z, 1, ARGB(175,c[2],c[3],c[4]))
            -- test world to screen and drawline instead of drawline3d:
            --heroPos = WorldToScreen(D3DXVECTOR3(heroPos.x, player.y, heroPos.y))
            --startPos = WorldToScreen(D3DXVECTOR3(startPos.x, player.y, startPos.y))
            --DrawLine(startPos.x, startPos.y, heroPos.x, heroPos.y, 1, ARGB(255,255,255,255))
            local infoText = hero.charName .. " [Lv." .. hero.level .. "]"
            DrawLine(infoPos.x, infoPos.y, infoPos.x + 30, infoPos.y - 30, 1, ARGB(175,255,255,255))
            DrawLine(infoPos.x + 30, infoPos.y - 30, infoPos.x + 30 + 10 * infoText:len(), infoPos.y - 30, 1, ARGB(175,255,255,255))
            DrawTextA(infoText, 20, infoPos.x + 30 + 1, infoPos.y - 30, ARGB(175,255,255,255), "left", "bottom")
            displayCount = displayCount + 1
        end
    end
end
-- ############################################### LaserVision ##################################################





-- ############################################### AutoLeveler ##################################################
class("U_AutoLeveler")
function U_AutoLeveler:__init(U, champType)
    self.U = U
    self.W = self.U.sprites.grid.sprite.width
    self.H = self.U.sprites.grid.sprite.height
    self.x = WINDOW_W/2 - self.W/2
    self.y = WINDOW_H - self.H*3.5
    self.champType = champType or 0
    self.sliders = self:AddSliders()
    self.ready = false
    
    -- (parent, x, y, W, H, text, a, r, g, b, preciseClick, mOverAction, mDownAction, mHoldAction, mUpAction, freeAction)
    self.toggle = U_Button(self, 20, 10, 40, 20, "OFF", 150, 200, 100, 100, true,
        function()
            self.toggle.a = 200
        end,
        function()
            self.toggle.a = 100
        end,
        function()
        end,
        function()
            self.toggle.a = 150
            self.toggle.text = self.toggle.text == "OFF" and "ON" or "OFF"
            self.toggle.r = self.toggle.text == "OFF" and 200 or 100
            self.toggle.g = self.toggle.text == "OFF" and 100 or 200
            self.ready = self.toggle.text == "ON"
        end,
        function()
            self.toggle.a = 150
        end
    )
    
    self.PrioLeveler = U_PrioLeveler(self)
    self.CentralHUD = self:CentralHUD()
    self:Load()
    self.PrioLeveler:BuildOrder()

    return self
end

function U_AutoLeveler:AddSliders() --(parent, x, y, W, H, text, a, r, g, b, preciseClick, vertical, sections)
    local sliders = {}
    for i = 1, 18 do
        if self.champType == 0 and (i == 6 or i == 11 or i == 16) then
            sliders[i] = U_Slider(self, self.W/20 * (i+1.5), self.H/5 * 1.5, self.W/20, self.H/5, "", 30, 200, 50, 50, true, true, 4, i)
        else
            sliders[i] = U_Slider(self, self.W/20 * (i+1.5), self.H/5 * 1.5, self.W/20, self.H/5, "", 0, 255, 255, 255, true, true, 4, i)
        end
    end
    return sliders
end

function U_AutoLeveler:OnTick()
    if GetSave("scriptConfig").Menu.menuKey and IsKeyDown(GetSave("scriptConfig").Menu.menuKey) or not GetSave("scriptConfig").Menu.menuKey and IsKeyDown(20) then
        for i, slider in pairs(self.sliders) do
            slider:OnTick()
        end
        self.PrioLeveler:OnTick()
        self.toggle:OnTick()
    end
    if player.level < 18 then
        for i, button in pairs(self.CentralHUD.buttons) do
            button:OnTick()
        end
    end
    if self.ready then self:LevelUp() end
end

function U_AutoLeveler:OnDraw()
    if GetSave("scriptConfig").Menu.menuKey and IsKeyDown(GetSave("scriptConfig").Menu.menuKey) or not GetSave("scriptConfig").Menu.menuKey and IsKeyDown(20) then
        self.U.sprites.grid.sprite:Draw(floor(self.x), floor(self.y), 255)
        for i, slider in ipairs(self.sliders) do
            slider:OnDraw()
        end
        self.PrioLeveler:OnDraw()
        self.toggle:OnDraw()
    end
    if player.level < 18 then
        for i, button in pairs(self.CentralHUD.buttons) do
            button:OnDraw()
        end
    end
end

function U_AutoLeveler:CanLevelSpell(spell, playerLv)
    local spellLv = 0
    local spellNormalLv = 0
    local maxManualLv = playerLv
    for i = 1, 18 do
        if self.sliders[i].val == spell then
            if i <= playerLv + (ceil(playerLv/2) == ceil((playerLv+1)/2) and 1 or 0) then
                spellLv = spellLv + 1
            end
            spellNormalLv = spellNormalLv + 1
            if not self.sliders[i].auto and i > playerLv then maxManualLv = i end
        end
    end
    return (spell < 3 or self.champType == 1 and spell == 3) and spellNormalLv < 5 and spellLv < ceil(playerLv/2) and spellNormalLv < ceil(maxManualLv/2)
            or spell == 3 and self.champType == 0 and spellLv < floor((playerLv-1)/5) and spellNormalLv < floor((maxManualLv-1)/5)
            or spell == 3 and self.champType == 2 and spellLv < floor((playerLv-1)/5) and spellNormalLv < floor((maxManualLv-1)/5)
end

function U_AutoLeveler:CanLevelSpellManual(spell)
    local spellLv = 0
    local spellNormalLv = 0
    local playerLv = player.level+1
    local maxManualLv = playerLv
    local realSpellLv = player:GetSpellData(spell).level
    for i = 1, 18 do
        if self.sliders[i].val == spell then
            if i <= playerLv + (ceil(playerLv/2) == ceil((playerLv+1)/2) and 1 or 0) then
                spellLv = spellLv + 1
            end
            spellNormalLv = spellNormalLv + 1
            if not self.sliders[i].auto and i > playerLv then maxManualLv = i end
        end
    end
    return (spell < 3 or self.champType == 1 and spell == 3) and spellNormalLv < 5 and spellLv < ceil(playerLv/2) and spellNormalLv < ceil(maxManualLv/2) and realSpellLv < ceil(playerLv/2) and realSpellLv < 5
            or spell == 3 and self.champType == 0 and spellLv < floor((playerLv-1)/5) and spellNormalLv < floor((maxManualLv-1)/5) and realSpellLv < floor((playerLv-1)/5) and realSpellLv < 3
            or spell == 3 and self.champType == 2 and spellLv < floor((playerLv-1)/5) and spellNormalLv < floor((maxManualLv-1)/5) and realSpellLv < floor((playerLv-1)/5) and realSpellLv < 4
end

function U_AutoLeveler:GetQueuedSpellLevel(spell, playerLv)
    local spellLv = 0
    for i = 1, playerLv do
        if self.sliders[i].val == spell then
            spellLv = spellLv + 1
        end
    end
    return spellLv
end

function U_AutoLeveler:ClearOrder()
    for i, slider in ipairs(self.sliders) do
        if slider.auto then slider.val = -1 end
    end
end

function U_AutoLeveler:LevelUp()
    for i = 1, player.level do
        if player:GetSpellData(self.sliders[i].val).level < self:GetQueuedSpellLevel(self.sliders[i].val, i) then
            LevelSpell(self.sliders[i].val)
        end
    end
end

function U_AutoLeveler:CentralHUD()
    local hud = {}

    local scale = self.U.scale
    local centerX = WINDOW_W/2 + 10*(1+scale)           -- center of the HUD
    local centralHUD = 315 * (1+scale)                  -- HUD size
    hud.x = centerX - centralHUD*0.27                   -- action bar X pos
    hud.y = WINDOW_H - 60*(1+scale)                     -- action bar Y pos
    local iconSize = 22*(1+scale)                       -- spell icon size
    local iconGap = 30.5*(1+scale)                      -- spell icon offset

    hud.buttons = {     -- (parent, x, y, W, H, text, a, r, g, b, preciseClick, mOverAction, mDownAction, mHoldAction, mUpAction, freeAction)
        Q = U_Button(hud, iconSize/2, iconSize/2, iconSize, iconSize, "", 0, 50, 255, 50, true,
                function()
                    hud.buttons.Q.a = self.sliders[player.level+1].val == _Q and 255 or 150
                end,
                function()
                    hud.buttons.Q.a = 100
                end,
                function()
                end,
                function()
                    self:ClearOrder()
                    if not self.sliders[player.level+1].auto and self.sliders[player.level+1].val == _Q then
                        self.sliders[player.level+1].val = -1
                        self.sliders[player.level+1].auto = true
                    elseif self:CanLevelSpellManual(_Q, player.level+1) then
                        self.sliders[player.level+1].val = _Q
                        self.sliders[player.level+1].auto = false
                    end
                    self.PrioLeveler:BuildOrder()
                end,
                function()
                    hud.buttons.Q.a = self.sliders[player.level+1].val == _Q and 255 or 0
                end,
                true
            ),
        W = U_Button(hud, iconSize/2 + iconGap*1, iconSize/2, iconSize, iconSize, "", 0, 50, 255, 50, true,
                function()
                    hud.buttons.W.a = self.sliders[player.level+1].val == _W and 255 or 150
                end,
                function()
                    hud.buttons.W.a = 100
                end,
                function()
                end,
                function()
                    self:ClearOrder()
                    if not self.sliders[player.level+1].auto and self.sliders[player.level+1].val == _W then
                        self.sliders[player.level+1].val = -1
                        self.sliders[player.level+1].auto = true
                    elseif self:CanLevelSpellManual(_W, player.level+1) then
                        self.sliders[player.level+1].val = _W
                        self.sliders[player.level+1].auto = false
                    end
                    self.PrioLeveler:BuildOrder()
                end,
                function()
                    hud.buttons.W.a = self.sliders[player.level+1].val == _W and 255 or 0
                end,
                true
            ),
        E = U_Button(hud, iconSize/2 + iconGap*2, iconSize/2, iconSize, iconSize, "", 0, 50, 255, 50, true,
                function()
                    hud.buttons.E.a = self.sliders[player.level+1].val == _E and 255 or 150
                end,
                function()
                    hud.buttons.E.a = 100
                end,
                function()
                end,
                function()
                    self:ClearOrder()
                    if not self.sliders[player.level+1].auto and self.sliders[player.level+1].val == _E then
                        self.sliders[player.level+1].val = -1
                        self.sliders[player.level+1].auto = true
                    elseif self:CanLevelSpellManual(_E, player.level+1) then
                        self.sliders[player.level+1].val = _E
                        self.sliders[player.level+1].auto = false
                    end
                    self.PrioLeveler:BuildOrder()
                end,
                function()
                    hud.buttons.E.a = self.sliders[player.level+1].val == _E and 255 or 0
                end,
                true
            ),
        R = U_Button(hud, iconSize/2 + iconGap*3, iconSize/2, iconSize, iconSize, "", 0, 50, 255, 50, true,
                function()
                    hud.buttons.R.a = self.sliders[player.level+1].val == _R and 255 or 150
                end,
                function()
                    hud.buttons.R.a = 100
                end,
                function()
                end,
                function()
                    self:ClearOrder()
                    if not self.sliders[player.level+1].auto and self.sliders[player.level+1].val == _R then
                        self.sliders[player.level+1].val = -1
                        self.sliders[player.level+1].auto = true
                    elseif self:CanLevelSpellManual(_R, player.level+1) then
                        self.sliders[player.level+1].val = _R
                        self.sliders[player.level+1].auto = false
                    end
                    self.PrioLeveler:BuildOrder()
                end,
                function()
                    hud.buttons.R.a = self.sliders[player.level+1].val == _R and 255 or 0
                end,
                true
            ),
    }

    return hud
end

function U_AutoLeveler:Load()
    if not GetSave("UtiliteeeAutoLevel")[player.charName] then GetSave("UtiliteeeAutoLevel")[player.charName] = { order = {}, priority = {} } end
    local load = GetSave("UtiliteeeAutoLevel")[player.charName]
    if load then
        for i, prio in ipairs(self.PrioLeveler.prioOrder) do
            if load.priority[i] then
                self.PrioLeveler.prioOrder[i] = load.priority[i]
            end
        end
        for i, button in pairs(self.PrioLeveler.buttons) do
            self.PrioLeveler:MoveButton(button)
        end
        for i, slider in ipairs(self.sliders) do
            if load.order[i] then
                slider.val = load.order[i]
                slider.auto = false
            end
        end
    end
end

function U_AutoLeveler:Save()
    local save = { order = {}, priority = {} }
    for i, slider in ipairs(self.sliders) do
        if not slider.auto then
            save.order[i] = slider.val
        end
    end
    for i, prio in ipairs(self.PrioLeveler.prioOrder) do
        save.priority[i] = prio
    end
    if not GetSave("UtiliteeeAutoLevel")[player.charName] then GetSave("UtiliteeeAutoLevel")[player.charName] = { order = {}, priority = {} } end
    table.clear(GetSave("UtiliteeeAutoLevel")[player.charName])
    table.merge(GetSave("UtiliteeeAutoLevel")[player.charName], save, true)
end


class("U_PrioLeveler")
function U_PrioLeveler:__init(parent)
    self.parent = parent
    self.x = parent.x + 100
    self.y = parent.y + 120
    self.prioOrder = { "R", "Q", "W", "E" }
    self.buttons = {    -- action order: mOverAction, mDownAction, mHoldAction, mUpAction, freeAction
        R = U_Button(self, 10, 25, 40, 40, "R", 255, 100, 100, 100, false,
                function()
                    self.buttons.R.a = 200
                end,
                function()
                    self.buttons.R.W = 50
                    self.buttons.R.H = 50
                end,
                function()
                    self.buttons.R.xO = GetCursorPos().x - self.x
                    self.buttons.R.a = 150
                end,
                function()
                    self.buttons.R.W = 40
                    self.buttons.R.H = 40
                    self.buttons.R.a = 255
                    self.parent:ClearOrder()
                    self:BuildOrder()
                end,
                function()
                    self.buttons.R.a = 255
                end
            ),
        W = U_Button(self, 70, 25, 40, 40, "W", 255, 100, 100, 100, false,
                function()
                    self.buttons.W.a = 200
                end,
                function()
                    self.buttons.W.W = 50
                    self.buttons.W.H = 50
                end,
                function()
                    self.buttons.W.xO = GetCursorPos().x - self.x
                    self.buttons.W.a = 150
                end,
                function()
                    self.buttons.W.W = 40
                    self.buttons.W.H = 40
                    self.buttons.W.a = 255
                    self.parent:ClearOrder()
                    self:BuildOrder()
                end,
                function()
                    self.buttons.W.a = 255
                end
            ),
        E = U_Button(self, 130, 25, 40, 40, "E", 255, 100, 100, 100, false,
                function()
                    self.buttons.E.a = 200
                end,
                function()
                    self.buttons.E.W = 50
                    self.buttons.E.H = 50
                end,
                function()
                    self.buttons.E.xO = GetCursorPos().x - self.x
                    self.buttons.E.a = 150
                end,
                function()
                    self.buttons.E.W = 40
                    self.buttons.E.H = 40
                    self.buttons.E.a = 255
                    self.parent:ClearOrder()
                    self:BuildOrder()
                end,
                function()
                    self.buttons.E.a = 255
                end
            ),
        Q = U_Button(self, 190, 25, 40, 40, "Q", 255, 100, 100, 100, false,
                function()
                    self.buttons.Q.a = 200
                end,
                function()
                    self.buttons.Q.W = 50
                    self.buttons.Q.H = 50
                end,
                function()
                    self.buttons.Q.xO = GetCursorPos().x - self.x
                    self.buttons.Q.a = 150
                end,
                function()
                    self.buttons.Q.W = 40
                    self.buttons.Q.H = 40
                    self.buttons.Q.a = 255
                    self.parent:ClearOrder()
                    self:BuildOrder()
                end,
                function()
                    self.buttons.Q.a = 255
                end
            ),
    }
    
    return self
end

function U_PrioLeveler:OnTick()
    for i, button in pairs(self.buttons) do
        button:OnTick()
    end
    self:PrioChange()
end

function U_PrioLeveler:OnDraw()
    for i, button in pairs(self.buttons) do
        button:OnDraw()
        self:MoveButton(button)
    end
    for i=0, 2 do
        DrawTextA(">", 12, self.x+40+60*i, self.y+25, ARGB(255,200,200,200), "center", "center")
    end
end

function U_PrioLeveler:PrioChange()
    table.sort(self.prioOrder, function(a,b) return self.buttons[a].xO < self.buttons[b].xO end)
end

function U_PrioLeveler:MoveButton(button)
    for i, spell in pairs(self.prioOrder) do
        if spell == button.text then
            button.xO = 10 + 60*(i-1)
        end
    end
end

function U_PrioLeveler:BuildOrder()
    local spellConvert = { Q = 0, W = 1, E = 2, R = 3 }
    for i, level in ipairs(self.parent.sliders) do
        for j, spell in ipairs(self.prioOrder) do
            if level.auto and self.parent:CanLevelSpell(spellConvert[spell], i) then
                level.val = spellConvert[spell]
                break
            end
        end
    end
    self.parent:Save()
end


class("U_Button")
function U_Button:__init(parent, x, y, W, H, text, a, r, g, b, preciseClick, mOverAction, mDownAction, mHoldAction, mUpAction, freeAction, specialType)
    self.parent = parent or { x = 0, y = 0 }
    self.W, self.H = W, H
    self.xO, self.yO = x, y
    self.x, self.y = parent.x + self.xO - self.W/2, parent.y + self.yO - self.H/2
    self.r, self.g, self.b, self.a = r, g, b, a
    self.text = text
    self.preciseClick = preciseClick
    self.mOverAction = mOverAction or function() end
    self.mDownAction = mDownAction or function() end
    self.mHoldAction = mHoldAction or function() end
    self.mUpAction = mUpAction or function() end
    self.freeAction = freeAction or function() end
    self.specialType = specialType
    self.isPressed = false
    return self
end

function U_Button:OnTick()
    if self.disabled then return end
    local key = self.specialType and 0x02 or 0x01
    if not __U_Drag and not self.isPressed and not IsKeyDown(key) and CursorIsUnder(self.x, self.y, self.W, self.H) then -- mouseover
        self.mOverAction()
    elseif not __U_Drag and not self.isPressed and IsFocused() and IsKeyDown(key) and CursorIsUnder(self.x, self.y, self.W, self.H) then -- mouse down (first click. no action)
        __U_Drag = true
        self.isPressed = true
        self.mDownAction()
    elseif self.isPressed and not IsKeyDown(key) then -- mouse up/release
        self.isPressed = false
        __U_Drag = false
        if self.preciseClick and CursorIsUnder(self.x, self.y, self.W, self.H) or not self.preciseClick then -- mouse up on button. DO ACTION
            self.mUpAction()
        end
    elseif self.isPressed and IsKeyDown(key) then -- mouse down (holding)
        self.mHoldAction()
    else -- anything else. not mouse over, not mouse down, nothing...
        self.freeAction()
    end
end

function U_Button:OnDraw()
    self.x = self.parent.x + self.xO - self.W/2
    self.y = self.parent.y + self.yO - self.H/2
    if self.disabled or self.a == 0 then return end
    if self.specialType then
        local W = math.abs(self.W)
        local H = math.abs(self.H)
        local t = 3
        local c = ARGB(self.a, self.r, self.g, self.b)
        DrawRectangle(floor(self.x), floor(self.y), W, t, c)
        DrawRectangle(floor(self.x), floor(self.y) + t, t, H - t*2, c)
        DrawRectangle(floor(self.x), floor(self.y) + H - t, W, t, c)
        DrawRectangle(floor(self.x) + W - t, floor(self.y) + t, t, H - t*2, c)
    else
        DrawRectangle(floor(self.x), floor(self.y), self.W, self.H, ARGB(self.a, self.r, self.g, self.b))
        if self.text and self.text ~= "" then DrawTextA("" .. self.text, 12, floor(self.x + self.W / 2), floor(self.y + self.H / 2), ARGB(self.a, 255, 255, 255), "center", "center") end
    end
end


class("U_Slider")
function U_Slider:__init(parent, x, y, W, H, text, a, r, g, b, preciseClick, vertical, sections, level)
    self.auto = true
    self.parent = parent or { x = 0, y = 0 }
    self.W, self.H = W, H
    self.xO, self.yO = x, y
    self.x, self.y = parent.x + self.xO - self.W/2, parent.y + self.yO - self.H/2
    self.marker = { x = self.W/2, y = self.H/2, W = self.W/2, H = self.H/2, a = 150, r = self.auto and 100 or 255, g = self.auto and 100 or 255, b = self.auto and 255 or 100 }
    self.r, self.g, self.b, self.a = r, g, b, a
    self.text = text
    self.vertical = vertical or false
    self.sections = sections or 2
    self.preciseClick = preciseClick
    self.isPressed = false
    self.level = level
    self.val = -1
    return self
end

function U_Slider:OnTick()
    local change = self.vertical and "y" or "x"
    local change2 = self.vertical and "H" or "W"
    if self.disabled then return end
    if not __U_Drag and not self.isPressed and not IsKeyDown(0x01) and CursorIsUnder(self.x, self.y, self.W, self.H*self.sections) then -- mouseover
        
    elseif not __U_Drag and not self.isPressed and IsFocused() and IsKeyDown(0x01) and CursorIsUnder(self.x, self.y, self.W, self.H*self.sections) then -- mouse down (first click. no action)
        __U_Drag = true
        self.isPressed = true
        self.oldVal = self.val
    elseif self.isPressed and not IsKeyDown(0x01) then -- mouse up/release
        __U_Drag = false
        self.isPressed = false
        if self.preciseClick and CursorIsUnder(self.x, self.y, self.W, self.H*self.sections) or not self.preciseClick then -- mouse up on button. DO ACTION
            if self.oldVal == self.val then
                self.parent:ClearOrder()
                self.val = -1
                self.auto = true
                self.parent.PrioLeveler:BuildOrder()
            end
        end
    elseif self.isPressed and IsKeyDown(0x01) then -- mouse down (holding)
        self.parent:ClearOrder()
        local cursorStep = (GetCursorPos()[change] - self[change]) / (self[change2]*self.sections)
        local spell = cursorStep < 0.25 and 0
                   or cursorStep >= 0.25 and cursorStep < 0.5 and 1
                   or cursorStep >= 0.5 and cursorStep < 0.75 and 2
                   or cursorStep >= 0.75 and cursorStep < 1 and 3
                   or cursorStep >= 1 and -1
        if spell == -1 or spell ~= -1 and self.parent:CanLevelSpell(spell, self.level) then
            self.val = spell
            if self.oldVal ~= self.val then self.oldVal = -1 end
            self.auto = spell == -1
        end
        self.parent.PrioLeveler:BuildOrder()
    else -- anything else. not mouse over, not mouse down, nothing...
        
    end
    
    if self.val == -1 then
        self.marker.a = 0
    else
        self.marker.a = 200
        self.marker[change] = self.marker[change2] + 40 * self.val
    end
    self.marker.r = self.auto and 200 or 255
    self.marker.g = self.auto and 200 or 255
    self.marker.b = self.auto and 200 or 50
end

function U_Slider:OnDraw()
    self.x = self.parent.x + self.xO - self.W/2
    self.y = self.parent.y + self.yO - self.H/2
    if self.a > 0 then 
        DrawRectangle(floor(self.x), floor(self.y), self.W * (self.vertical and 1 or self.sections), self.H * (self.vertical and self.sections or 1), ARGB(self.a, self.r, self.g, self.b))
    end
    if player.level == self.level then
        DrawRectangle(floor(self.x), floor(self.y - self.H), self.W * (self.vertical and 1 or self.sections), self.H * (self.vertical and self.sections + 1 or 1), ARGB(50, 255, 255, 255))
    end
    DrawRectangle(floor(self.x + self.marker.x/2), floor(self.y + self.marker.y/2), self.marker.W, self.marker.H, ARGB(self.marker.a, self.marker.r, self.marker.g, self.marker.b))
    if self.a > 0 and self.text and self.text ~= "" then DrawTextA("" .. self.text, 12, floor(self.x + self.W / 2), floor(self.y + self.H / 2 - 5), ARGB(self.a, 255, 255, 255), "center") end
end
-- ############################################### AutoLeveler ##################################################





-- ################################################# Gankeee ####################################################
class("U_Gankeee")

function U_Gankeee:__init(U)
    self.U = U
    self.swapOver = true
    self.swapVision = false
    self.updTick = 0
    self.vRadiusTable = {
        ["obj_AI_Minion"] = 1200,
        ["obj_AI_Turret"] = 1350,
        ["obj_AI_Hero"] = 1350,
    }
    return self
end

function U_Gankeee:OnTick()
    if GetInGameTimer() >= self.updTick then
        self.swapVision = self.U.config.gankeee.pvSwapVision
        if self.swapVision or (not self.swapVision and not self.swapOver) then
            self.swapOver = not self.swapVision
            self:SwapVision(
                self.U.enemyMinions.objects, self.U.allyMinions.objects,          --minions
                self.U.turretTable[TEAM_ENEMY], self.U.turretTable[player.team],  --turrets
                self.U.heroTable[TEAM_ENEMY], self.U.heroTable[player.team]       --heroes
            )
        end
        self.updTick = GetInGameTimer() + 0.1
    end
end

function U_Gankeee:OnDraw()
    if self.swapVision and self.U.config.gankeee.pvShowCircles then
        self:DrawVisionRadius(self.U.enemyMinions.objects)
        self:DrawVisionRadius(self.U.turretTable[TEAM_ENEMY])
        self:DrawVisionRadius(self.U.heroTable[TEAM_ENEMY])
    end
end

function U_Gankeee:OnDeleteObj(obj)
    if obj and obj.valid and obj.team == TEAM_ENEMY and (obj.type == "obj_AI_Minion" or obj.type == "obj_AI_Turret") then
        if obj.data and obj.data.donor then obj.data.donor.data.isUsed = false end
        self:SetVisionObj(obj, 0)
    end
end

function U_Gankeee:CleanUpMinions(table)   -- call this before minions:update() in general OnTick()
    for i, obj in pairs(table) do
        if obj and obj.valid and obj.dead and obj.data then
            self:SetVisionObj(obj, 0)
            if obj.data.donor and obj.data.donor.valid then
                self:SetVisionObj(obj.data.donor, obj.data.donor.data.visionObj)
                obj.data.donor.data.isUsed = false
            end
            obj.data.donor = nil
        elseif obj and not obj.valid and obj.data and obj.data.donor then
            obj.data.donor.data.isUsed = false
            obj.data.donor = nil
        end
    end
end

function U_Gankeee:SwapVision(enemyMinions, allyMinions, enemyTurrets, allyTurrets, enemyHeroes, allyHeroes)
    self:SwapVisionBetween(enemyMinions, allyMinions)
    self:SwapVisionBetween(enemyTurrets, allyTurrets)
    self:SwapVisionBetween(enemyHeroes, allyHeroes)
end

function U_Gankeee:SwapVisionBetween(enemies, allies)
    if self.swapVision then
        -- Enemies
        for i, obj in pairs(enemies) do
            if obj and obj.valid then
                if not obj.dead and self.U:OnScreen(obj) then
                    if obj.data and obj.data.donor and obj.data.donor.valid and obj.data.donor.dead then obj.data.donor.data.isUsed = false obj.data.donor = nil end -- Deleting donor if it's dead:
                    if (obj.data and not obj.data.donor) or not obj.data then   -- no donor
                        for j, donor in pairs(allies) do
                            if donor and donor.valid and not donor.dead and donor.visible --and donor.health >= donor.maxHealth*0.2
                            and (donor.data and not donor.data.isUsed or not donor.data) then
                                donor.data = donor.data or {}
                                donor.data.visionObj = donor.data.visionObj or self:GetVisionObj(donor)
                                donor.data.isUsed = true
                                obj.data = obj.data or {}
                                obj.data.donor = donor
                                obj.data.visionObj = obj.data.visionObj or self:GetVisionObj(obj)
                                self:SetVisionObj(obj, donor.data.visionObj)
                                self:SetVisionObj(donor, 0)  -- try this
                                break
                            end
                        end
                    end
                else    -- dead, out of screen or/and not visible:
                    if obj.data and obj.data.donor then
                        self:SetVisionObj(obj, 0)    -- try this
                        if obj.data.donor.valid then
                            self:SetVisionObj(obj.data.donor, obj.data.donor.data.visionObj)
                            obj.data.donor.data.isUsed = false
                        end
                        obj.data.donor = nil
                    end
                end
            end
        end
        -- Allies
        for i, obj in pairs(allies) do if obj and obj.valid then obj:SetVisionRadius(0) end end
    else
        -- Enemies releasing
        for i, obj in pairs(enemies) do
            if obj and obj.valid and obj.data and obj.data.donor then
                self:SetVisionObj(obj, obj.data.visionObj)
                if obj.data.donor.valid then
                    self:SetVisionObj(obj.data.donor, obj.data.donor.data.visionObj)
                    obj.data.donor.data.isUsed = false
                end
                obj.data.donor = nil
            end
        end
        -- Allies releasing
        for i, obj in pairs(allies) do
            if obj and obj.valid then
                obj.data = obj.data or {}
                obj.data.isUsed = false
                if obj.visionRadius ~= self.vRadiusTable[obj.type] then obj:SetVisionRadius(self.vRadiusTable[obj.type]) end  -- restoring vision radius for ally units
                if obj.data.visionObj then self:SetVisionObj(obj, obj.data.visionObj) end                           -- restoring vision obj for ally units
            end
        end
    end
end

function U_Gankeee:DrawVisionRadius(table)
    for i, obj in pairs(table) do
        if obj and obj.valid and not obj.dead and obj.team == TEAM_ENEMY and obj.data and obj.data.donor and self.vRadiusTable[obj.type] then
            local c = self.U.config.gankeee.pvCircleColor
            DrawCircle(obj.x, obj.y, obj.z, self.vRadiusTable[obj.type], ARGB(255,c[2],c[3],c[4]))
        end
    end
end

function U_Gankeee:GetVisionObj(unit)
    if unit and unit.valid then
        return
            unit.type == "obj_AI_Minion" and unit:GetMinionVisionObj()
            or unit.type == "obj_AI_Turret" and unit:GetTurretVisionObj()
            or unit.type == "obj_AI_Hero" and unit:GetHeroVisionObj()
    else
        print("GetVisionObj(): wrong unit type.")
        return nil
    end
end

function U_Gankeee:SetVisionObj(unit, visionObj)
    if unit and unit.valid then
        return
            unit.type == "obj_AI_Minion" and unit:SetMinionVisionObj(visionObj)
            or unit.type == "obj_AI_Turret" and unit:SetTurretVisionObj(visionObj)
            or unit.type == "obj_AI_Hero" and unit:SetHeroVisionObj(visionObj)
    else
        print("SetVisionObj(): wrong unit type.")
        return nil
    end
end
-- ################################################# Gankeee ####################################################





-- ###################################### SSS (SelfSkillShots) + modSTOP ########################################
class("U_SSSmS")
function U_SSSmS:__init(U)
    self.U = U
    return self
end

function U_SSSmS:OnTick()
    if IsFocused() then
        if self.U.config.littlemods.selfSkillShots and IsKeyDown(18) then -- Alt
            if IsKeyDown(GetKey("Q")) then CastSpell(_Q, myHero.x, myHero.z) end
            if IsKeyDown(GetKey("W")) then CastSpell(_W, myHero.x, myHero.z) end
            if IsKeyDown(GetKey("E")) then CastSpell(_E, myHero.x, myHero.z) end
            if IsKeyDown(GetKey("R")) then CastSpell(_R, myHero.x, myHero.z) end
        end
        if self.U.config.littlemods.modStop and (IsKeyDown(18) or IsKeyDown(17) or IsKeyDown(16)) and IsKeyDown(self.U.config.littlemods._param[5].key) then player:HoldPosition() end
    end
end
-- ###################################### SSS (SelfSkillShots) + modSTOP ########################################





-- ############################################# Smooth MouseMove ###############################################
class("U_SmoothMove")
function U_SmoothMove:__init() return self end

function U_SmoothMove:OnTick()
    -- THIS SHIT NEEDS CURSOR TYPE:
    -- 19.09.2013:      0x01C52C14       "League of Legends.exe"+1852C14
    -- 01.10.2013:      0x012141E4       "League of Legends.exe"+10841E4
    -- 03.10.2013:      0x01BE41F4       "League of Legends.exe"+10841F4
    -- 1 - normal
    -- 2 - ally
    -- 3 - hostile
    -- 6 - shop
    local cursorType = ReadDWORD(0x01BE41F4)
    if not __U_Drag and IsKeyDown(0x02) and IsFocused() and (cursorType == 1 or cursorType == 2) then player:MoveTo(mousePos.x, mousePos.z) end
end
-- ############################################# Smooth MouseMove ###############################################





-- ############################################### Item Sorter ##################################################
class("U_ItemSorter")
function U_ItemSorter:__init()
    -- Slot -> Priority = Item ID
    self.itemSortTable = {
        [ITEM_1] = {
            [1] = 3157,   -- Zhonya's Hourglass
            [2] = 3144,   -- Bilgewater Cutlass
            [3] = 3069,   -- Shurelya's Reverie
        },
        [ITEM_2] = {
            [1] = 3140,   -- Quicksilver Sash
            [2] = 2003,   -- Health Potion
        },
        [ITEM_3] = {
            [1] = 3128,   -- DFG
            [2] = 3165,   -- Morello's Evil Tome
            [3] = 3142,   -- Youmuu's Ghostblade
            [4] = 3146,   -- Hextech Gunblade
            [5] = 3190,   -- Locket of the Iron Solari
            [6] = 3143,   -- Randuin's Omen
            [7] = 2044,   -- Sight Ward
            [8] = 2004,   -- Mana Potion
        },
        [ITEM_4] = {
            [1] = 3154,   -- Wriggle's Lantern
            [2] = 2043,   -- Vision Ward
        },
        [ITEM_5] = {
            [1] = 2038,   -- Agility
            [2] = 2039,   -- Brilliance
            [3] = 2037,   -- Fortitude
            [4] = 2042,   -- Oracle's Elixir
        },
    }
    self.sortTick = GetInGameTimer() + 0.5
end

function U_ItemSorter:OnTick()
    if GetInGameTimer() >= self.sortTick then
        self.sortTick = GetInGameTimer() + 0.5
        for slot, slotTable in pairs(self.itemSortTable) do
            for prio, item in pairs(slotTable) do
                if player:getInventorySlot(slot) == item then break end
                local moved = false
                for i = ITEM_1, ITEM_6, 1 do
                    if i ~= slot and player:getInventorySlot(i) == item then
                        self:MoveItem(slot, i)
                        moved = true
                        break
                    end
                end
                if moved then break end
            end
        end
    end
end

function U_ItemSorter:MoveItem(from, to)  -- use ITEM_1 .. ITEM_6
    from = math.max(4, math.min(9, from))-4 or 0
    to = math.max(4, math.min(9, to))-4 or 0
    pE = CLoLPacket(0x20)
    pE:EncodeF(player.networkID)
    pE:Encode1(from)
    pE:Encode1(to)
    SendPacket(pE)
end
-- ############################################### Item Sorter ##################################################





-- ########################################## Simple Enemy Tower Range ##########################################
-- ################################# cropped from Tower Range script by SurfaceS ################################
class("U_TowerRange")
function U_TowerRange:__init(U)
    self.U = U
    self.updTick = 0
    self.color = RGB(self.U.config.towerrange.trCircleColor[2], self.U.config.towerrange.trCircleColor[3], self.U.config.towerrange.trCircleColor[4])
    return self
end

function U_TowerRange:OnTick()
    if GetGame().isOver or player.dead then return end
    if GetInGameTimer() > self.updTick then
        self.updTick = GetInGameTimer() + 0.5
        self.color = RGB(self.U.config.towerrange.trCircleColor[2], self.U.config.towerrange.trCircleColor[3], self.U.config.towerrange.trCircleColor[4])
        for i, turret in pairs(self.U.turretTable[TEAM_ENEMY]) do
            turret.drawRange = turret and turret.valid and GetDistance(turret) < 2000
        end
    end
end

function U_TowerRange:OnDraw()
    if GetGame().isOver or player.dead then return end
    for i, turret in pairs(self.U.turretTable[TEAM_ENEMY]) do
        if turret.drawRange then DrawCircle(turret.x, turret.y, turret.z, 950, self.color) end
    end
end
-- ########################################## Simple Enemy Tower Range ##########################################




-- ############################################## Where Did He Go? ##############################################
-- ############################# slight modification of ViceVersa's original script #############################
-- ########################### slightly different logic, text markers for used spell ############################
class("U_WhereDidHeGo")
function U_WhereDidHeGo:__init(U)
    self.U = U
    self.blinksTable = {}
    --for i, hero in pairs(self.U.heroTable[player.team]) do  -- debug
    for i, hero in pairs(self.U.heroTable[TEAM_ENEMY]) do
        if hero and hero.valid then
            -- Summoner flash:
            if hero:GetSpellData(SUMMONER_1).name:find("Flash") or hero:GetSpellData(SUMMONER_2).name:find("Flash") then
                table.insert(self.blinksTable,{name = "SummonerFlash"..hero.charName, maxRange = 400, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "Flash"})
            end
            -- Normal spells:
            if hero.charName == "Ezreal" then
                table.insert(self.blinksTable,{name = "EzrealArcaneShift", maxRange = 475, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "E"})
            elseif hero.charName == "Kassadin" then
                table.insert(self.blinksTable,{name = "RiftWalk", maxRange = 700, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "R"})
            elseif hero.charName == "Katarina" then
                table.insert(self.blinksTable,{name = "KatarinaE", maxRange = 700, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "E"})
            elseif hero.charName == "Leblanc" then
                table.insert(self.blinksTable,{name = "LeblancSlide", maxRange = 600, delay = 0.5, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "W"})
                table.insert(self.blinksTable,{name = "leblancslidereturn", delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "W"})
                table.insert(self.blinksTable,{name = "LeblancSlideM", maxRange = 600, delay = 0.5, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "W"})
                table.insert(self.blinksTable,{name = "leblancslidereturnm", delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "W"})
            elseif hero.charName == "MasterYi" then
                table.insert(self.blinksTable,{name = "AlphaStrike", maxRange = 600, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "Q"})
            elseif hero.charName == "Shaco" then
                table.insert(self.blinksTable,{name = "Deceive", maxRange = 400, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "Q"})
            elseif hero.charName == "Talon" then
                table.insert(self.blinksTable,{name = "TalonCutthroat", maxRange = 700, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "E"})
            elseif hero.charName == "Vayne" then
                table.insert(self.blinksTable,{name = "VayneTumble", maxRange = 250, delay = 0, casted = false, timeCasted = 0, startPos = {}, endPos = {}, castingHero = hero, shortName = "Q", ultEndTick = 0})
            end
        end
    end
end

function U_WhereDidHeGo:OnTick()
    for i=1, #self.blinksTable, 1 do
        local blink = self.blinksTable[i]
        if blink.casted then
            if blink.castingHero.dead or os.clock() > (blink.timeCasted + self.U.config.WDHG.displayTime) or blink.castingHero.visible and os.clock() > blink.timeCasted + blink.delay + 0.2 then
                blink.casted = false
            elseif self.blinksTable[i].castingHero.visible and os.clock() > self.blinksTable[i].timeCasted + self.blinksTable[i].delay and os.clock() <= blink.timeCasted + blink.delay + 0.2 then
                blink.endPos = { x = blink.castingHero.x, y = blink.castingHero.y, z = blink.castingHero.z }
            end
        end
    end
end

function U_WhereDidHeGo:OnDraw()
    for i=1, #self.blinksTable, 1 do
        local blink = self.blinksTable[i]
        if blink.casted then
            DrawCircle(blink.endPos.x , blink.endPos.y , blink.endPos.z , self.U.config.WDHG.circleSize, RGB(self.U.config.WDHG.circleColor[2],self.U.config.WDHG.circleColor[3],self.U.config.WDHG.circleColor[4]))
            local lineStartPos = WorldToScreen(D3DXVECTOR3(blink.startPos.x, blink.startPos.y, blink.startPos.z))
            local lineEndPos = WorldToScreen(D3DXVECTOR3(blink.endPos.x, blink.endPos.y, blink.endPos.z))
            DrawLine(lineStartPos.x, lineStartPos.y, lineEndPos.x, lineEndPos.y, self.U.config.WDHG.lineWidth, RGB(self.U.config.WDHG.lineColor[2],self.U.config.WDHG.lineColor[3],self.U.config.WDHG.lineColor[4]))
            local offset = 30
            local infoText = blink.castingHero.charName .. " " .. blink.shortName
            DrawLine(lineEndPos.x, lineEndPos.y, lineEndPos.x + offset, lineEndPos.y - offset, 1, ARGB(255,255,255,255))
            DrawLine(lineEndPos.x + offset, lineEndPos.y - offset, lineEndPos.x + offset + 6 * infoText:len(), lineEndPos.y - offset, 1, ARGB(255,255,255,255))
            DrawTextA(infoText, 12, lineEndPos.x + offset + 1, lineEndPos.y - offset, ARGB(255,255,255,255), "left", "bottom")
        end
    end
end

function U_WhereDidHeGo:OnProcessSpell(unit, spell)
    --if unit and unit.valid and unit.team == TEAM_ENEMY and unit.type == "obj_AI_Hero" or unit.isMe then   -- debug
    if unit and unit.valid and unit.team == TEAM_ENEMY and unit.type == "obj_AI_Hero" then
        if spell.name == "vayneinquisition" then
            for i=1, #self.blinksTable, 1 do
                local blink = self.blinksTable[i]
                if blink.name == "VayneTumble" then blink.ultEndTick = os.clock() + 6 + 2*spell.level return end
            end
        end
        for i=1, #self.blinksTable, 1 do
            local blink = self.blinksTable[i]
            if spell.name == blink.name or spell.name..unit.charName == blink.name then
                if spell.name == "VayneTumble" and os.clock() >= blink.ultEndTick then return end
                blink.casted = true
                blink.timeCasted = os.clock()
                blink.startPos = { x = spell.startPos.x, y = spell.startPos.y, z = spell.startPos.z }
                if blink.name == "leblancslidereturn" or blink.name == "leblancslidereturnm" then   --Leblanc
                    --Cancel the other W-spells if she returns
                    if blink.name == "leblancslidereturn" then
                        self.blinksTable[i-1].casted, self.blinksTable[i+1].casted, self.blinksTable[i+2].casted = false, false, false
                    else
                        self.blinksTable[i-3].casted, self.blinksTable[i-2].casted, self.blinksTable[i-1].casted = false, false, false
                    end
                    blink.endPos = { x = self.blinksTable[i-1].startPos.x, y = self.blinksTable[i-1].startPos.y, z = self.blinksTable[i-1].startPos.z } --Set the end position to the start position of her last slide
                else
                    if GetDistance(spell.startPos, spell.endPos) <= blink.maxRange then
                        blink.endPos = { x = spell.endPos.x, y = spell.endPos.y, z = spell.endPos.z }
                    else
                        local vStartPos = Vector(spell.startPos.x, spell.startPos.y, spell.startPos.z)
                        local vEndPos = Vector(spell.endPos.x, spell.endPos.y, spell.endPos.z)
                        local tEndPos = vStartPos - (vStartPos - vEndPos):normalized() * blink.maxRange
                        blink.endPos = { x = tEndPos.x, y = tEndPos.y, z = tEndPos.z }
                    end
                end
                break
            end
        end
    end
end
-- ############################################## Where Did He Go? ##############################################






--        ________________________
-- (\(\  | 10x 4 us1ng m3, br0sk1 |
-- (^.^) |.-----------------------'
-- (")(")
--

--UPDATEURL=https://dl.dropboxusercontent.com/u/93477088/BoL/Scripts/Utiliteee/Utiliteee.lua
--HASH=9B08FBFEADB87453A750FFD8C9A58D51
