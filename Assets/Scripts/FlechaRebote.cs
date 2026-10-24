using UnityEngine;

// Mueve un sprite hacia arriba y abajo (la flecha de la cueva)
public class FlechaRebote : MonoBehaviour
{
    Vector3 baseP;
    void Start() { baseP = transform.position; }
    void Update() { transform.position = baseP + new Vector3(0f, Mathf.Sin(Time.time * 4f) * 0.12f, 0f); }
}
