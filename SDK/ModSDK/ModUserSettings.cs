using System;

namespace ModSDK
{
    /*
     * In the Mod User Settings file, you can freely modify the enums to ensure that both projects will share the same physics layers, asset labels and game object tags.
     * 
     * Note: that you can no longer use the Tags & Layers Management tools provided via Unity, 
     * in results of using Unity's toolset may cause corruption, which is easily fixed via opening the mod tool sdk ui.
    */

    [Flags]
    public enum ModPhysicLayers
    {
        SelfPlayer
    }
    [Flags]
    public enum ModAssetLabels
    {
        Scene
    }
    [Flags]
    public enum ModObjectTags
    {
        Vehicle
    }
}
