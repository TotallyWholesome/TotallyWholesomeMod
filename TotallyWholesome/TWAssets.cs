using System.IO;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
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
        public static Sprite BadgeGold, BadgeSilver, BadgeBronze;
        public static Sprite TWTagNormalIcon, TWTagBetaIcon;
        public static Material Classic, Chain, Gradient, Leather, Magic, Amogus, Asexual, Bisexual, Gay, Genderfluid, Lesbian, LGBT, Nonbinary, Pansexual, Polysexual, Trans, Christmas;
        public static GameObject StatusPrefab, NotificationPrefab, TWRaycaster, TWBlindness;
        public static AudioMixer TWMixer;

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
                Amogus = _twAssetsBundle.LoadAsset<Material>("AMOGUS");
                Amogus.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                NotificationPrefab = _twAssetsBundle.LoadAsset<GameObject>("NotificationRoot");
                NotificationPrefab.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                TWRaycaster = _twAssetsBundle.LoadAsset<GameObject>("TWRaycaster");
                TWRaycaster.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                TWBlindness = _twAssetsBundle.LoadAsset<GameObject>("TWBlindness");
                TWBlindness.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                TWMixer = _twAssetsBundle.LoadAsset<AudioMixer>("TWMixer");
                TWMixer.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                BadgeGold = _twAssetsBundle.LoadAsset<Sprite>("Badge-Gold");
                BadgeGold.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                BadgeSilver = _twAssetsBundle.LoadAsset<Sprite>("Badge-Silver");
                BadgeSilver.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                BadgeBronze = _twAssetsBundle.LoadAsset<Sprite>("Badge-Bronze");
                BadgeBronze.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                TWTagNormalIcon = _twAssetsBundle.LoadAsset<Sprite>("TW_Logo_Pride");
                TWTagNormalIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                TWTagBetaIcon = _twAssetsBundle.LoadAsset<Sprite>("TW_Logo_Pride-Beta");
                TWTagBetaIcon.hideFlags |= HideFlags.DontUnloadUnusedAsset;

                //Pride flag materials
                Asexual = _twAssetsBundle.LoadAsset<Material>("Asexual");
                Asexual.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Bisexual = _twAssetsBundle.LoadAsset<Material>("Bisexual");
                Bisexual.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Gay = _twAssetsBundle.LoadAsset<Material>("Gay");
                Gay.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Genderfluid = _twAssetsBundle.LoadAsset<Material>("Genderfluid");
                Genderfluid.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Lesbian = _twAssetsBundle.LoadAsset<Material>("Lesbian");
                Lesbian.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                LGBT = _twAssetsBundle.LoadAsset<Material>("LGBT");
                LGBT.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Nonbinary = _twAssetsBundle.LoadAsset<Material>("Nonbinary");
                Nonbinary.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Pansexual = _twAssetsBundle.LoadAsset<Material>("Pansexual");
                Pansexual.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Polysexual = _twAssetsBundle.LoadAsset<Material>("Polysexual");
                Polysexual.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                Trans = _twAssetsBundle.LoadAsset<Material>("Trans");
                Trans.hideFlags |= HideFlags.DontUnloadUnusedAsset;

                Christmas = _twAssetsBundle.LoadAsset<Material>("Christmas");
                Christmas.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            }

            Con.Debug("Successfully loaded in assets from TWNotification bundle!");
        }
    }
}