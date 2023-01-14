using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using WholesomeLoader;

namespace TotallyWholesome
{
    public class TWAssets
    {
        public static Sprite Alert;
        public static Sprite Crown;
        public static Sprite Handcuffs;
        public static Sprite Key;
        public static Sprite Link;
        public static Sprite Megaphone;
        public static Sprite MicrophoneOff;
        public static Sprite Close;
        public static Sprite Checkmark;
        public static Material Classic, Chain, Gradient, Leather, Magic;
        public static GameObject StatusPrefab, NotificationPrefab, TWRaycaster;

        //AssetBundle Parts
        private static AssetBundle _twAssetsBundle;
        
        public static void LoadAssets()
        {
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TotallyWholesome.twassets"))
            {
                Con.Debug("Loaded TWAssets AssetBundle");
                if (assetStream != null)
                {
                    using var tempStream = new MemoryStream((int) assetStream.Length);
                    assetStream.CopyTo(tempStream);

                    _twAssetsBundle = AssetBundle.LoadFromMemory(tempStream.ToArray(), 0);
                    _twAssetsBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }
            }

            if (_twAssetsBundle != null)
            {
                //Load Sprites
                Alert = _twAssetsBundle.LoadAsset<Sprite>("Alert");
                Alert.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Crown = _twAssetsBundle.LoadAsset<Sprite>("Crown - Stars");
                Crown.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Handcuffs = _twAssetsBundle.LoadAsset<Sprite>("Handcuffs");
                Handcuffs.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Key = _twAssetsBundle.LoadAsset<Sprite>("Key");
                Key.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Link = _twAssetsBundle.LoadAsset<Sprite>("Link");
                Link.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Megaphone = _twAssetsBundle.LoadAsset<Sprite>("Megaphone");
                Megaphone.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                MicrophoneOff = _twAssetsBundle.LoadAsset<Sprite>("Microphone Off");
                MicrophoneOff.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Close = _twAssetsBundle.LoadAsset<Sprite>("Close");
                Close.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Checkmark = _twAssetsBundle.LoadAsset<Sprite>("Checkmark");
                Checkmark.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                StatusPrefab = _twAssetsBundle.LoadAsset<GameObject>("NameplateStatus");
                StatusPrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Classic = _twAssetsBundle.LoadAsset<Material>("TWClassic");
                Classic.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Chain = _twAssetsBundle.LoadAsset<Material>("TWChain");
                Chain.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Gradient = _twAssetsBundle.LoadAsset<Material>("TWGradient");
                Gradient.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Leather = _twAssetsBundle.LoadAsset<Material>("TWLeather");
                Leather.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Magic = _twAssetsBundle.LoadAsset<Material>("TWMagic");
                Magic.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                NotificationPrefab = _twAssetsBundle.LoadAsset<GameObject>("Notification");
                NotificationPrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                TWRaycaster = _twAssetsBundle.LoadAsset<GameObject>("TWRaycaster");
                TWRaycaster.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }

            Con.Debug("Successfully loaded in assets from TWNotification bundle!");
        }
    }
}