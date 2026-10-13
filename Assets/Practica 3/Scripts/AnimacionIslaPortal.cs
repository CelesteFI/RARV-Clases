using System.Collections;
using UnityEngine;

public class AnimacionIslaPortal : MonoBehaviour
{
    private Light luzPortal;

    void Awake()
    {
        // Crea una fuente de luz puntual si no existe
        luzPortal = GetComponentInChildren<Light>();
        if (luzPortal == null)
        {
            GameObject objLuz = new GameObject("LuzPortal");
            objLuz.transform.SetParent(transform, false);
            objLuz.transform.localPosition = new Vector3(0, 0.15f, 0);
            luzPortal = objLuz.AddComponent<Light>();
            luzPortal.type = LightType.Point;
            luzPortal.color = new Color(0.4f, 0.8f, 1.0f); // Azul etéreo celestial
            luzPortal.range = 0.5f;
            luzPortal.intensity = 0f;
        }
    }

    public void ActivarDestelloPortal()
    {
        StartCoroutine(EfectoDestello());
    }

    private IEnumerator EfectoDestello()
    {
        float tiempo = 0f;
        float duracion = 1.2f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            // Destello que sube rápido de intensidad y decae lentamente
            float factor = Mathf.Sin((tiempo / duracion) * Mathf.PI);
            luzPortal.intensity = factor * 4.0f;
            yield return null;
        }
        luzPortal.intensity = 0.8f; // Resplandor residual suave
    }
}