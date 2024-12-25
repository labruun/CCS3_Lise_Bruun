using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateTextWithSlider : MonoBehaviour
{
    [SerializeField]
    private TextMesh textMesh;

    [SerializeField]
    private UnityEngine.UI.Slider slider;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        textMesh.text = slider.value.ToString();
    }
}
