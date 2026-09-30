using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class MoverObjeto : MonoBehaviour
{
    public GameObject modelo;
    public ObserverBehaviour[] marcadores;
    public int indiceActual = 0;
    public float velocidad = 1.0f;
    private bool seEstaMoviendo = false;

    public void MoverAlSiguienteMarcador()
    {
        if (!seEstaMoviendo)
        {
            StartCoroutine(TrasladarModelo());
        }
    }

    private IEnumerator TrasladarModelo()
    {
        seEstaMoviendo = true;
        ObserverBehaviour objetivo = ObtenerSiguienteObjetivo();

        if (objetivo == null)
        {
            seEstaMoviendo = false;
            yield break;
        }

        Vector3 posicionInicial = modelo.transform.position;
        Vector3 posicionFinal = objetivo.transform.position;

        float progreso = 0f;
        while (progreso < 1.0f)
        {
            progreso += Time.deltaTime * velocidad;
            modelo.transform.position = Vector3.Lerp(posicionInicial, posicionFinal, progreso);
            yield return null;
        }

        modelo.transform.position = posicionFinal;
        seEstaMoviendo = false;
    }

    private ObserverBehaviour ObtenerSiguienteObjetivo()
    {
        for (int i = 0; i < marcadores.Length; i++)
        {
            if (i == indiceActual) continue;

            ObserverBehaviour mb = marcadores[i];
            if (mb != null)
            {
                TargetStatus status = mb.TargetStatus;
                if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
                {
                    indiceActual = i;
                    return mb;
                }
            }
        }
        return null;
    }
}