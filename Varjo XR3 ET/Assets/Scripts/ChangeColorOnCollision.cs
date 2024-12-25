using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeColorOnCollision : MonoBehaviour
{
    public Color normalColor;
    public Color collisionColor;
    private Renderer rend;

    
    void Awake()
    {
        rend = transform.GetComponent<Renderer>();
        rend.material.color = normalColor;
    }
    
    void OnTriggerStay(Collider other)
    {
        rend.material.color = collisionColor;
    }

    void OnTriggerExit(Collider other)
    {
        rend.material.color = normalColor;
    }
}
