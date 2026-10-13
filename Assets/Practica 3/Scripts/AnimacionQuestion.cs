using UnityEngine;

public class AnimacionQuestion : MonoBehaviour
{
    public float velocidadGiro = 80f;
    public float amplitudFlotacion = 0.015f;
    public float frecuenciaFlotacion = 3f;

    private Vector3 posInicial;

    void Start()
    {
        posInicial = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * (velocidadGiro * Time.deltaTime), Space.Self);
        float offset = Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.localPosition = new Vector3(posInicial.x, posInicial.y + offset, posInicial.z);
    }
}