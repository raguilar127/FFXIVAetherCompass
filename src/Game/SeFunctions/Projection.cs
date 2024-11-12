using Dalamud.Interface;
using Dalamud.Interface.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace AetherCompass.Game.SeFunctions
{
    internal unsafe static class Projection
    {
        private delegate IntPtr GetMatrixSingletonDelegate();
        private static readonly GetMatrixSingletonDelegate getMatrixSingleton;
        private static readonly Device* device;

        static Projection()
        {
            IntPtr addr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
            getMatrixSingleton ??= Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(addr);
            device = Device.Instance();
        }
        public static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos) => Plugin.GameGui.WorldToScreen(worldPos, out screenPos);
        
    }
}
