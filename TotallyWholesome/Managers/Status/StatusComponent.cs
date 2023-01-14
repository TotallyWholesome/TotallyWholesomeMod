using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TotallyWholesome.Managers.Status
{
    public class StatusComponent : MonoBehaviour
    {
        public Image statusImage;
        public Image specialMark;
        public TextMeshProUGUI specialMarkText;
        public GameObject petIndicator;
        public GameObject masterIndicator;
        public Image backgroundImage;

        public void SetupStatus(GameObject statusInstance)
        {
            statusImage = statusInstance.transform.Find("Status").GetComponent<Image>();
            specialMark = statusInstance.transform.Find("SpecialMark").GetComponent<Image>();
            specialMarkText = specialMark.transform.Find("SpecialMarkText").GetComponent<TextMeshProUGUI>();
            petIndicator = statusInstance.transform.Find("PetIndicator").gameObject;
            masterIndicator = statusInstance.transform.Find("MasterIndicator").gameObject;
            backgroundImage = statusInstance.transform.Find("Background").GetComponent<Image>();
        }
        
        public void ResetStatus()
        {
            statusImage.gameObject.SetActive(false);
            specialMark.gameObject.SetActive(false);
            petIndicator.SetActive(false);
            masterIndicator.SetActive(false);
            specialMarkText.text = "";
            gameObject.SetActive(false);
        }
    }
}