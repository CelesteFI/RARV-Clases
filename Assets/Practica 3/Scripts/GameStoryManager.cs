using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Vuforia;

public class GameStoryManager : MonoBehaviour
{
    public enum TipoElemento
    {
        Manzana,
        Tigre,
        Gema,
        Isla
    }

    [Header("UI")]
    public TextMeshProUGUI textoHistoria;

    [Header("Protagonista")]
    public GameObject pajaro;
    public ObserverBehaviour marcadorPajaroBase; // ImageTarget (4)
    public float velocidadVuelo = 0.8f;
    public float alturaArcoVuelo = 0.14f;

    [Header("Marcadores Secundarios (4)")]
    public ObserverBehaviour[] marcadoresSecundarios; // ImageTarget (0) al (3)

    [Header("Signos de Interrogación por Marcador (4)")]
    public GameObject[] questionsMarcadores; // Question_0 al Question_3 hijos de los targets

    [Header("Contenedores de Escena (4)")]
    public GameObject escenaManzana; // Escena_Manzana
    public GameObject escenaTigre;   // Escena_Tigre
    public GameObject escenaGema;    // Escena_Altar
    public GameObject escenaIsla;    // Escena_Isla

    private int pasoHistoria = 0;
    private bool seEstaMoviendo = false;
    private bool historiaIniciada = false;
    private Animator animatorPajaro;

    private ObserverBehaviour marcadorActualPajaro;
    private Dictionary<ObserverBehaviour, TipoElemento> asignacionMarcadores = new Dictionary<ObserverBehaviour, TipoElemento>();
    private HashSet<ObserverBehaviour> targetsRevelados = new HashSet<ObserverBehaviour>();

    void Start()
    {
        if (pajaro != null)
        {
            animatorPajaro = pajaro.GetComponentInChildren<Animator>();
        }

        ReiniciarAventura();
    }

    void Update()
    {
        ActualizarVisibilidadPorTracking();
    }

    public void ReiniciarAventura()
    {
        StopAllCoroutines();
        seEstaMoviendo = false;
        pasoHistoria = 0;
        historiaIniciada = false;
        targetsRevelados.Clear();

        // 1. Restaurar posición inicial del pájaro en el nido
        if (marcadorPajaroBase != null && pajaro != null)
        {
            marcadorActualPajaro = marcadorPajaroBase;
            Transform puntoInicial = marcadorPajaroBase.transform.Find("Escena_Pajaro/Base/Punto Pajaro");
            if (puntoInicial == null) puntoInicial = marcadorPajaroBase.transform.Find("Base/Punto Pajaro");

            pajaro.transform.SetParent(marcadorPajaroBase.transform, false);

            if (puntoInicial != null)
            {
                pajaro.transform.localPosition = puntoInicial.localPosition;
                pajaro.transform.localRotation = puntoInicial.localRotation;
            }
            else
            {
                pajaro.transform.localPosition = Vector3.zero;
                pajaro.transform.localRotation = Quaternion.identity;
            }

            pajaro.SetActive(false);
        }

        // 2. Barajar los dioramas entre los marcadores secundarios
        BarajarModelos();

        // 3. Ocultar escenas y reactivar los signos de interrogación
        if (escenaManzana != null) escenaManzana.SetActive(false);
        if (escenaTigre != null) escenaTigre.SetActive(false);
        if (escenaIsla != null) escenaIsla.SetActive(false);

        if (escenaGema != null)
        {
            escenaGema.SetActive(false);
            Transform cristal = escenaGema.transform.Find("altar completo/Gema");
            if (cristal == null) cristal = escenaGema.transform.Find("Gema");
            if (cristal != null) cristal.gameObject.SetActive(true);
        }

        foreach (var q in questionsMarcadores)
        {
            if (q != null) q.SetActive(true);
        }

        ActualizarTextoMision();
    }

    private void BarajarModelos()
    {
        asignacionMarcadores.Clear();

        List<TipoElemento> elementos = new List<TipoElemento>
        {
            TipoElemento.Manzana,
            TipoElemento.Tigre,
            TipoElemento.Gema,
            TipoElemento.Isla
        };

        // Algoritmo Fisher-Yates
        for (int i = 0; i < elementos.Count; i++)
        {
            int rnd = Random.Range(i, elementos.Count);
            TipoElemento temp = elementos[i];
            elementos[i] = elementos[rnd];
            elementos[rnd] = temp;
        }

        for (int i = 0; i < marcadoresSecundarios.Length; i++)
        {
            ObserverBehaviour marcador = marcadoresSecundarios[i];
            TipoElemento tipo = elementos[i];
            asignacionMarcadores[marcador] = tipo;

            GameObject escenaAsignada = ObtenerGameObjectPorTipo(tipo);
            if (escenaAsignada != null && marcador != null)
            {
                escenaAsignada.transform.SetParent(marcador.transform, false);
                escenaAsignada.transform.localPosition = Vector3.zero;
                escenaAsignada.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private GameObject ObtenerGameObjectPorTipo(TipoElemento tipo)
    {
        switch (tipo)
        {
            case TipoElemento.Manzana: return escenaManzana;
            case TipoElemento.Tigre: return escenaTigre;
            case TipoElemento.Gema: return escenaGema;
            case TipoElemento.Isla: return escenaIsla;
            default: return null;
        }
    }

    private void ActualizarVisibilidadPorTracking()
    {
        if (seEstaMoviendo) return;

        // Control del ave protagonista
        if (pajaro != null && marcadorActualPajaro != null)
        {
            bool targetVisible = EsTargetVisible(marcadorActualPajaro);
            if (!historiaIniciada)
            {
                if (pajaro.activeSelf != targetVisible)
                    pajaro.SetActive(targetVisible);
            }
            else
            {
                if (!pajaro.activeSelf)
                    pajaro.SetActive(true);
            }
        }

        // Control de visibilidad en marcadores secundarios
        for (int i = 0; i < marcadoresSecundarios.Length; i++)
        {
            ObserverBehaviour marcador = marcadoresSecundarios[i];
            GameObject question = (i < questionsMarcadores.Length) ? questionsMarcadores[i] : null;

            if (marcador == null) continue;

            bool visibleEnCamara = EsTargetVisible(marcador);
            bool yaFueRevelado = targetsRevelados.Contains(marcador);

            // Al concluir la misión, se ocultan elementos secundarios ajenos a la isla
            if (pasoHistoria >= 4)
            {
                if (question != null) question.SetActive(false);

                if (asignacionMarcadores.ContainsKey(marcador))
                {
                    TipoElemento tipo = asignacionMarcadores[marcador];
                    GameObject escena = ObtenerGameObjectPorTipo(tipo);
                    if (escena != null)
                    {
                        if (tipo == TipoElemento.Isla)
                            escena.SetActive(visibleEnCamara);
                        else
                            escena.SetActive(false);
                    }
                }
                continue;
            }

            if (!yaFueRevelado)
            {
                if (question != null && question.activeSelf != visibleEnCamara)
                {
                    question.SetActive(visibleEnCamara);
                }
            }
            else
            {
                if (question != null && question.activeSelf)
                {
                    question.SetActive(false);
                }

                if (asignacionMarcadores.ContainsKey(marcador))
                {
                    TipoElemento tipo = asignacionMarcadores[marcador];
                    GameObject escena = ObtenerGameObjectPorTipo(tipo);
                    if (escena != null)
                    {
                        if (tipo == TipoElemento.Manzana && pasoHistoria > 0)
                        {
                            escena.SetActive(false);
                        }
                        else if (escena.activeSelf != visibleEnCamara)
                        {
                            escena.SetActive(visibleEnCamara);
                        }
                    }
                }
            }
        }
    }

    private bool EsTargetVisible(ObserverBehaviour mb)
    {
        if (mb == null) return false;
        TargetStatus status = mb.TargetStatus;
        return status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
    }

    public void PresionarBotonAccion()
    {
        if (seEstaMoviendo || pasoHistoria >= 4) return;

        ObserverBehaviour marcadorDestino = ObtenerMarcadorDestinoValido();

        if (marcadorDestino == null)
        {
            if (textoHistoria != null)
                textoHistoria.text = "Enfoca la carta misteriosa hacia donde quieres que vuele el pájaro.";
            return;
        }

        if (!asignacionMarcadores.ContainsKey(marcadorDestino)) return;

        TipoElemento elemento = asignacionMarcadores[marcadorDestino];
        StartCoroutine(ProcesarViajeEInteraccion(marcadorDestino, elemento));
    }

    private ObserverBehaviour ObtenerMarcadorDestinoValido()
    {
        foreach (var mb in marcadoresSecundarios)
        {
            if (mb != null && mb != marcadorActualPajaro && EsTargetVisible(mb))
            {
                return mb;
            }
        }
        return null;
    }

    private Transform ObtenerPuntoAterrizaje(GameObject escena)
    {
        if (escena == null) return null;

        // Búsqueda en el prefab base (rama)
        Transform punto = escena.transform.Find("Base/Punto Pajaro");
        if (punto != null) return punto;

        punto = escena.transform.Find("EscenaBase/Punto Pajaro");
        if (punto != null) return punto;

        // Búsqueda en la jerarquía de la isla
        punto = escena.transform.Find("island/Punto Pajaro");
        if (punto != null) return punto;

        punto = escena.transform.Find("Punto Pajaro");
        return punto;
    }

    private IEnumerator ProcesarViajeEInteraccion(ObserverBehaviour destino, TipoElemento elemento)
    {
        seEstaMoviendo = true;
        historiaIniciada = true;

        pajaro.transform.SetParent(null);
        pajaro.SetActive(true);

        if (animatorPajaro != null)
        {
            animatorPajaro.SetBool("active", true);
        }

        GameObject escenaDestino = ObtenerGameObjectPorTipo(elemento);
        Transform puntoRef = ObtenerPuntoAterrizaje(escenaDestino);

        Vector3 posInicial = pajaro.transform.position;
        Vector3 posFinal = (puntoRef != null) ? puntoRef.position : destino.transform.position;
        Quaternion rotFinal = (puntoRef != null) ? puntoRef.rotation : destino.transform.rotation;

        // Orientación inicial hacia el destino en el plano horizontal
        Vector3 dirHorizontal = (posFinal - posInicial);
        dirHorizontal.y = 0;
        if (dirHorizontal != Vector3.zero)
        {
            pajaro.transform.rotation = Quaternion.LookRotation(dirHorizontal.normalized);
        }

        // Vuelo parabólico
        float progreso = 0f;
        while (progreso < 1.0f)
        {
            progreso += Time.deltaTime * velocidadVuelo;
            Vector3 posActual = Vector3.Lerp(posInicial, posFinal, progreso);
            posActual.y += Mathf.Sin(progreso * Mathf.PI) * alturaArcoVuelo;

            pajaro.transform.position = posActual;

            // Interpolación de rotación final al aproximarse al aterrizaje
            if (progreso > 0.6f)
            {
                float factorGiro = (progreso - 0.6f) / 0.4f;
                pajaro.transform.rotation = Quaternion.Slerp(pajaro.transform.rotation, rotFinal, factorGiro);
            }

            yield return null;
        }

        pajaro.transform.position = posFinal;
        pajaro.transform.rotation = rotFinal;

        // Emparentar con el marcador destino conservando su posición
        pajaro.transform.SetParent(destino.transform, true);
        marcadorActualPajaro = destino;

        if (animatorPajaro != null)
        {
            animatorPajaro.SetBool("active", false);
        }

        // --- REVELACIÓN DEL DIORAMA ---
        targetsRevelados.Add(destino);

        int indexDestino = System.Array.IndexOf(marcadoresSecundarios, destino);
        if (indexDestino >= 0 && indexDestino < questionsMarcadores.Length && questionsMarcadores[indexDestino] != null)
        {
            questionsMarcadores[indexDestino].SetActive(false);
        }

        if (escenaDestino != null)
        {
            escenaDestino.SetActive(true);
        }

        EjecutarEfectoEspecial(elemento);
        EvaluarPaso(elemento);

        seEstaMoviendo = false;
    }

    private void EjecutarEfectoEspecial(TipoElemento elemento)
    {
        if (elemento == TipoElemento.Tigre && escenaTigre != null)
        {
            AnimacionGuardian guardianAnim = escenaTigre.GetComponentInChildren<AnimacionGuardian>();
            if (guardianAnim != null) guardianAnim.ReaccionarLlegada();
        }
        else if (elemento == TipoElemento.Isla && escenaIsla != null)
        {
            AnimacionIslaPortal portalAnim = escenaIsla.GetComponentInChildren<AnimacionIslaPortal>();
            if (portalAnim != null) portalAnim.ActivarDestelloPortal();
        }
    }

    private void EvaluarPaso(TipoElemento elemento)
    {
        switch (pasoHistoria)
        {
            case 0:
                if (elemento == TipoElemento.Manzana)
                {
                    if (animatorPajaro != null) animatorPajaro.SetTrigger("comer");
                    StartCoroutine(ConsumirManzanaConAnimacion());
                }
                else
                {
                    textoHistoria.text = "¡Descubriste un lugar incorrecto! El pájaro está hambriento y necesita la Manzana primero.";
                }
                break;

            case 1:
                if (elemento == TipoElemento.Tigre)
                {
                    pasoHistoria = 2;
                    textoHistoria.text = "El Guardián dice: 'Has demostrado vigor. Encuentra la Gema Astral para activar el santuario'.";
                }
                else
                {
                    textoHistoria.text = "Debes presentar tus respetos al Guardián antes de continuar.";
                }
                break;

            case 2:
                if (elemento == TipoElemento.Gema)
                {
                    Transform cristal = escenaGema.transform.Find("altar completo/Gema");
                    if (cristal == null) cristal = escenaGema.transform.Find("Gema");
                    if (cristal != null) cristal.gameObject.SetActive(false);

                    pasoHistoria = 3;
                    textoHistoria.text = "¡Obtuviste la Gema Astral! Vuela a la Isla Flotante para abrir el portal final.";
                }
                else
                {
                    textoHistoria.text = "Aún te falta encontrar la Gema Astral.";
                }
                break;

            case 3:
                if (elemento == TipoElemento.Isla)
                {
                    pasoHistoria = 4;
                    textoHistoria.text = "¡FELICIDADES! Has completado la travesía astral y activado el portal sagrado.";

                    // Desactivar las demás escenas para limpiar la escena final
                    if (escenaManzana != null) escenaManzana.SetActive(false);
                    if (escenaTigre != null) escenaTigre.SetActive(false);
                    if (escenaGema != null) escenaGema.SetActive(false);

                    foreach (var q in questionsMarcadores)
                    {
                        if (q != null) q.SetActive(false);
                    }
                }
                break;
        }
    }

    private IEnumerator ConsumirManzanaConAnimacion()
    {
        yield return new WaitForSeconds(1.2f);
        if (escenaManzana != null) escenaManzana.SetActive(false);
        pasoHistoria = 1;
        textoHistoria.text = "¡El pájaro comió la Manzana Dorada y recuperó su energía! Ahora busca al Tigre Guardián.";
    }

    private void ActualizarTextoMision()
    {
        if (textoHistoria == null) return;

        switch (pasoHistoria)
        {
            case 0:
                textoHistoria.text = "Capítulo 1: Explora las cartas misteriosas para encontrar la Manzana Dorada.";
                break;
            case 1:
                textoHistoria.text = "Capítulo 2: Con energía renovada, localiza la carta del Tigre Guardián.";
                break;
            case 2:
                textoHistoria.text = "Capítulo 3: Encuentra el altar con la Gema Astral.";
                break;
            case 3:
                textoHistoria.text = "Capítulo 4: Descubre la carta de la Isla Flotante para abrir el portal final.";
                break;
            case 4:
                textoHistoria.text = "Aventura completada. Presiona 'Reinicio' para barajar los marcadores.";
                break;
        }
    }
}