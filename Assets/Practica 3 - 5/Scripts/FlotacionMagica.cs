using UnityEngine;

public class FlotacionMagica : MonoBehaviour
{
    [Header("Parámetros de Flotación")]
    public float amplitudFlotacion = 0.08f; // Mayor rango de subida y bajada
    public float frecuenciaFlotacion = 3.5f; // Ritmo más dinámico y perceptible

    private Vector3 posInicialLocal;

    void Start()
    {
        posInicialLocal = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frecuenciaFlotacion) * amplitudFlotacion;
        transform.localPosition = new Vector3(posInicialLocal.x, posInicialLocal.y + offset, posInicialLocal.z);
    }
}