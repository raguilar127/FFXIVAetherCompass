﻿using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System;

using Telepo = FFXIVClientStructs.FFXIV.Client.Game.UI.Telepo;


namespace AetherCompass.Compasses
{
    public class AetherCurrentCompass : Compass
    {
        public override string CompassName => "Aether Current Compass";
        public override string Description => "Compass that detects Aether Currents nearby." +
            "\nOptionally also shows nearby Aetherytes in the wild fields (those that can teleport to) " +
            "which have not yet been interacted.";
        
        private protected override string ClosestObjectDescription => "Aether Current";

        private static System.Numerics.Vector4 aetherCurrentInfoTextColour = new(.8f, .95f, .75f, 1);
        private static System.Numerics.Vector4 aetheryteInfoTextColour = new(.7f, .9f, 1, 1);

#if DEBUG
        private bool showAllAetherytes = false;  // for debug, show all incl. towns' and interacted
#endif


        public AetherCurrentCompass(PluginConfig config, AetherCurrentCompassConfig compassConfig, IconManager iconManager) : 
            base(config, compassConfig, iconManager) { }

        public override unsafe Action? CreateDrawDetailsAction(GameObject* obj)
        {
            if (obj == null) return null;
            return new(() =>
            {
                if (obj == null) return;
                ImGui.Text($"{CompassUtil.GetName(obj)}");
                ImGui.BulletText($"{CompassUtil.GetMapCoordInCurrentMapFormattedString(obj->Position)} (approx.)");
                ImGui.BulletText(($"{CompassUtil.GetDirectionFromPlayer(obj)} {CompassUtil.Get3DDistanceFromPlayer(obj):0.0}; " +
                    $"Altitude diff: {(int)CompassUtil.GetAltitudeDiffFromPlayer(obj)}"));
                DrawFlagButton($"##{(long)obj}", CompassUtil.GetMapCoordInCurrentMap(obj->Position));
                ImGui.Separator();
            });
        }

        public override unsafe Action? CreateMarkScreenAction(GameObject* obj)
        {
            if (obj == null) return null;
            if (obj->ObjectKind == (byte)ObjectKind.Aetheryte)
            {
                var icon = iconManager.AetheryteMarkerIcon;
                if (icon == null) return null;
                return new(() => DrawScreenMarkerDefault(obj, icon, IconManager.AetheryteMarkerIconSize,
                        .9f, $"{CompassUtil.Get3DDistanceFromPlayer(obj):0}", aetheryteInfoTextColour, out _));
            }
            else
            {
                var icon = iconManager.AetherCurrentMarkerIcon;
                if (icon == null) return null;
                return new(() => DrawScreenMarkerDefault(obj, icon, IconManager.AetherCurrentMarkerIconSize,
                        .9f, $"{CompassUtil.Get3DDistanceFromPlayer(obj):0}", aetherCurrentInfoTextColour, out _));
            }
        }

        private protected override unsafe bool IsObjective(GameObject* o)
        {
            if (o == null) return false;
            if (AetherCurrentCompassConfig != null && AetherCurrentCompassConfig.ShowAetherite && o->ObjectKind == (byte)ObjectKind.Aetheryte)
            {
#if DEBUG
                if (showAllAetherytes) return true;
#endif
                // Filter in only non interactive teleportable ones
                var aetherytes = Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Aetheryte>();
                if (aetherytes == null || aetherytes.GetRow(o->DataID) == null) return false;
                if (!aetherytes.GetRow(o->DataID)!.IsAetheryte) return false;
                var terr = CompassUtil.GetCurrentTerritoryType();
                // Only check for cities or normal fields;
                // IntendedUse == 0 && BattalionMode == 1 are towns; IntendedUse == 1 are normal fields
                if (terr == null || terr.TerritoryIntendedUse > 1 || terr.BattalionMode != 1) return false;
                var telepo = TelepoUi;
                if (telepo == null) return false;
                telepo->UpdateAetheryteList();
                var tlList = telepo->TeleportList;
                for (ulong i = 0; i < tlList.Size(); i++)
                    if (tlList.Get(i).AetheryteId == o->DataID) return false;   // interacted one
                return true;
            }
            if (o->ObjectKind != (byte)ObjectKind.EventObj) return false;
            var eObjNames = Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.EObjName>();
            if (eObjNames == null) return false;
            return IsNameOfAetherCurrent(eObjNames.GetRow(o->DataID)?.Singular.RawString) 
                || IsNameOfAetherCurrent(eObjNames.GetRow(o->DataID)?.Plural.RawString);
        }

        private unsafe static Telepo* TelepoUi => Telepo.Instance();

        private static bool IsNameOfAetherCurrent(string? name)
        {
            if (name == null) return false;
            name = name.ToLower();
            return name == "aether current" || name == "aether currents"
                || name == "風脈の泉"
                || name == "Windätherquelle" || name == "Windätherquellen"
                || name == "vent éthéré" || name == "vents éthérés"
                ;
        }

        private AetherCurrentCompassConfig? AetherCurrentCompassConfig => compassConfig as AetherCurrentCompassConfig;

        public override void DrawConfigUiExtra()
        {
            var aConfig = AetherCurrentCompassConfig;
            if (aConfig == null) return;
            ImGui.Checkbox("Detect Aetherytes (?)", ref aConfig.ShowAetherite);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Also allow this compass to detect Aetherytes " +
                    "that have not been interacted in the wild fields.\n" +
                    "Will NOT detect Aetherytes in towns, whether interacted or not.");
#if DEBUG
            if (aConfig.ShowAetherite)
                ImGui.Checkbox("[DEBUG] Detect All Aetherytes", ref showAllAetherytes);
#endif
        }
    }
}