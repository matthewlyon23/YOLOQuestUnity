using System;
using System.Globalization;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using YOLOQuestUnity.YOLO;

public class ConfidenceSliderController : MonoBehaviour
{
    
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;
    [SerializeField] private RemoteYOLOHandler remoteYoloHandler;
    
    void Start()
    {
        gameObject.GetComponent<Slider>().value = (float)Math.Round(remoteYoloHandler.m_confidenceThreshold, 2);
        textMeshProUGUI.text = Math.Round(remoteYoloHandler.m_confidenceThreshold, 2).ToString(CultureInfo.InvariantCulture);
    }

    public void OnConfidenceSliderValueChanged(Slider slider)
    {
        textMeshProUGUI.text = Math.Round(slider.value, 2).ToString(CultureInfo.InvariantCulture);
        remoteYoloHandler.m_confidenceThreshold = (float)Math.Round(slider.value, 2);
    }
}
