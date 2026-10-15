using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class MoverObjeto : MonoBehaviour
{
    public GameObject modelo; //modelo que se mueve entre los marcadores
    public ObserverBehaviour[] marcadores;// 0: Pájaro, 1: Perro, 2: Flor
    public int indiceActual = 0;
    public float velocidad = 0.3f;

    public GameObject modeloPerro;
    public GameObject modeloFlor;


    private bool seEstaMoviendo = false;
     private Animator animator;

    void Start()
    {
        if (modelo != null)
        {
            animator = modelo.GetComponentInChildren<Animator>();
        }

        ActualizarVisibilidad();
    }

    private void ActualizarVisibilidad()
    {
        // El pájaro SIEMPRE se ve
        if (modelo != null)
        {
            modelo.SetActive(true);
        }

        // Si llegó al 0 (su marcador): muestra a los dos sin problema
        if (indiceActual == 0)
        {
            if (modeloPerro != null) modeloPerro.SetActive(true);
            if (modeloFlor != null) modeloFlor.SetActive(true);
        }
        // Si llegó al 1 (marcador del perro): oculta al perro, deja la flor
        else if (indiceActual == 1)
        {
            if (modeloPerro != null) modeloPerro.SetActive(false);
            if (modeloFlor != null) modeloFlor.SetActive(true);
        }
        // Si llegó al 2 (marcador de la flor): oculta la flor, deja al perro
        else if (indiceActual == 2)
        {
            if (modeloPerro != null) modeloPerro.SetActive(true);
            if (modeloFlor != null) modeloFlor.SetActive(false);
        }
    }

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

        // Enciende la animación
        if (animator != null)
        {
            animator.SetBool("active", true);
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

        // Apaga la animación al aterrizar
        if (animator != null)
        {
            animator.SetBool("active", false);
        }

        ActualizarVisibilidad();
        seEstaMoviendo = false;
    }

    private ObserverBehaviour ObtenerSiguienteObjetivo()
    {
        List<int> candidatos = new List<int>();

        for (int i = 0; i < marcadores.Length; i++)
        {
            if (i == indiceActual) continue;

            ObserverBehaviour mb = marcadores[i];
            if (mb != null)
            {
                TargetStatus status = mb.TargetStatus;
                if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
                {
                    candidatos.Add(i);
                }
            }
        }
        if (candidatos.Count == 0) return null;

        // Elige aleatoriamente entre los que estén a la vista (evita ciclarse siempre en los mismos dos)
        int indiceElegido = candidatos[Random.Range(0, candidatos.Count)];
        indiceActual = indiceElegido;
        return marcadores[indiceElegido];

    }
}