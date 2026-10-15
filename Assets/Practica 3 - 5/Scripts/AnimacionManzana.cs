using UnityEngine;

public class AnimacionManzana : MonoBehaviour
{
    public float velocidadGiro = 45f;
    public float amplitudFlotacion = 0.02f;
    public float frecuenciaFlotacion = 3f;

    private Vector3 posInicial;

    void Start()
    {
        posInicial = transform.localPosition;
    }

    void Update()
    {
        // Giro continuo sobre su propio eje
        transform.Rotate(Vector3.up * (velocidadGiro * Time.deltaTime), Space.Self);

        // Flotación senoidal vertical
        float offset = Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.localPosition = new Vector3(posInicial.x, posInicial.y + offset, posInicial.z);
    }
}