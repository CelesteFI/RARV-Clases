using UnityEngine;

public class AnimacionGema : MonoBehaviour
{
    public float velocidadRotacion = 60f;
    public float amplitudEscala = 0.08f;
    public float frecuenciaPulso = 4f;

    private Vector3 escalaInicial;

    void Start()
    {
        escalaInicial = transform.localScale;
    }

    void Update()
    {
        // Giro místico
        transform.Rotate(Vector3.forward * (velocidadRotacion * Time.deltaTime), Space.Self);

        // Efecto de latido/pulso de energía
        float pulso = 1.0f + Mathf.Sin(Time.time * frecuenciaPulso) * amplitudEscala;
        transform.localScale = escalaInicial * pulso;
    }
}