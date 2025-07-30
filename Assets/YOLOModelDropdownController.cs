using TMPro;
using UnityEngine;
using YOLOQuestUnity.YOLO;
using YOLOQuestUnity.YOLO.RemoteYOLO;

public class YOLOModelDropdownController : MonoBehaviour
{

    [SerializeField] private RemoteYOLOHandler remoteYoloHandler;
    
    
    void Start()
    {
        gameObject.GetComponent<TMP_Dropdown>().value = (int)remoteYoloHandler.m_YOLOModel;
    }

    public void OnYOLOModelDropdownValueChanged(TMP_Dropdown dropdown)
    {
        remoteYoloHandler.m_YOLOModel = (YOLOModel)dropdown.value;
    }
}
