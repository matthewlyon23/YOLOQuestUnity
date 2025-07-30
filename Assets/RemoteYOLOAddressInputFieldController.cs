using TMPro;
using UnityEngine;
using YOLOQuestUnity.YOLO;

public class RemoteYOLOAddressInputFieldController : MonoBehaviour
{
    
    [SerializeField] private RemoteYOLOHandler remoteYoloHandler;
    
    void Start()
    {
        gameObject.GetComponent<TMP_InputField>().text = remoteYoloHandler.m_remoteYOLOProcessorAddress;
    }

    public void OnEndEdit(TMP_InputField inputField)
    {
        remoteYoloHandler.m_remoteYOLOClient.BaseAddress = inputField.text;   
    }
}
