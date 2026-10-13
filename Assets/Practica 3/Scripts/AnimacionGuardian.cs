using System.Collections;
using UnityEngine;

public class AnimacionGuardian : MonoBehaviour
{
    public float amplitudRespiracion = 0.03f;
    public float frecuenciaRespiracion = 2.0f;

    private Vector3 escalaInicial;

    void Start()
    {
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        // Respiración orgánica suave mientras espera
        float offset = Mathf.Sin(Time.time * frecuenciaRespiracion) * amplitudRespiracion;
        transform.localScale = new Vector3(escalaInicial.x, escalaInicial.y * (1f + offset), escalaInicial.z);
    }

    // Efecto de saludo o rugido cuando aterriza el pájaro
    public void ReaccionarLlegada()
    {
        StartCoroutine(EfectoSalto());
    }

    private IEnumerator EfectoSalto()
    {
        Vector3 escalaSalto = escalaInicial * 1.15f;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaInicial, escalaSalto, t / 0.25f);
            yield return null;
        }
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaSalto, escalaInicial, t / 0.25f);
            yield return null;
        }
    }
}