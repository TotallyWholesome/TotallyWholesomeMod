using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace TotallyWholesome.Managers.Status
{
    public class StatusComponent : MonoBehaviour
    {
        //Special Mark
        public Image specialMark;
        public TextMeshProUGUI specialMarkText;
        //Pair role indicator
        public GameObject petIndicator;
        public GameObject masterIndicator;
        //Status Group
        public GameObject buttplugDevice;
        public GameObject piShockDevice;
        public Image statusBackground;
        public Image masterAuto;
        public Image petAuto;
        //Background
        public Image backgroundImage;
        private static readonly int MaskEnabled = Shader.PropertyToID("_MaskEnabled");

        public void SetupStatus(GameObject statusInstance)
        {
            specialMark = statusInstance.transform.Find("SpecialMark").GetComponent<Image>();
            specialMarkText = specialMark.transform.Find("SpecialMarkText").GetComponent<TextMeshProUGUI>();
            petIndicator = statusInstance.transform.Find("PetIndicator").gameObject;
            masterIndicator = statusInstance.transform.Find("MasterIndicator").gameObject;
            backgroundImage = statusInstance.transform.Find("Background").GetComponent<Image>();
            buttplugDevice = statusInstance.transform.Find("AutoAcceptGroup/Buttplug").gameObject;
            piShockDevice = statusInstance.transform.Find("AutoAcceptGroup/PiShock").gameObject;
            masterAuto = statusInstance.transform.Find("AutoAcceptGroup/MasterAuto/Image").GetComponent<Image>();
            petAuto = statusInstance.transform.Find("AutoAcceptGroup/PetAuto/Image").GetComponent<Image>();
            statusBackground = statusInstance.transform.Find("AutoAcceptGroup/Background").GetComponent<Image>();
        }
        
        public void ResetStatus()
        {
            specialMark.gameObject.SetActive(false);
            petIndicator.SetActive(false);
            masterIndicator.SetActive(false);
            specialMarkText.text = "";
            buttplugDevice.SetActive(false);
            piShockDevice.SetActive(false);
            masterAuto.gameObject.SetActive(false);
            petAuto.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void UpdateAutoAcceptGroup(bool piShock, bool buttplug, bool pet, bool master)
        {
            var bothActive = master && pet ? 1 : 0;
            masterAuto.material.SetInteger(MaskEnabled, bothActive);
            petAuto.material.SetInteger(MaskEnabled, bothActive);
            
            masterAuto.gameObject.SetActive(master);
            petAuto.gameObject.SetActive(pet);
            
            piShockDevice.SetActive(piShock);
            buttplugDevice.SetActive(buttplug);
            statusBackground.gameObject.SetActive(piShock || master);
        }
    }
}