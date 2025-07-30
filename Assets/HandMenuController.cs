using UnityEngine;
using UnityEngine.UI;
using YOLOQuestUnity.YOLO;

public class HandMenuController : MonoBehaviour
{

    [SerializeField] private RemoteYOLOHandler remoteYoloHandler;

    void Start()
    {
        gameObject.GetComponent<Toggle>().isOn = remoteYoloHandler.m_useCustomModel;
    }
    
    public async void OnCustomModelToggleChanged(Toggle toggle)
    {
        remoteYoloHandler.m_useCustomModel = toggle.isOn;
        
        if (toggle.isOn)
        {
            await remoteYoloHandler.UploadCustomModelAsync();
        }
        
        toggle.isOn = remoteYoloHandler.m_useCustomModel;
    }
}
