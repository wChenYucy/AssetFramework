using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HotPatchPanel : MonoBehaviour
{
    [FormerlySerializedAs("silder")] public Image Silder;
    public Text OptionText;
    public Text SpeedText;
    [Header("热更信息界面")]
    public GameObject InfoPanel;
    public Text HotContentTex;
}
