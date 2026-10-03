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
    public ObserverBehaviour marcadorPajaroBase; // Marcador 0 (Nido)
    public float velocidadVuelo = 0.8f;
    public float alturaArcoVuelo = 0.12f; // Altura del arco ajustada a escala de mesa

    [Header("Distancias de Aterrizaje (Escala Real Vuforia en Metros)")]
    // Tarjeta física: 8 cm (0.08 m de ancho total, radio de 0.04 m desde el centro)
    public float distanciaManzana = 0.015f; // 1.5 cm del centro (frente a la manzana)
    public float distanciaGema = 0.020f;    // 2.0 cm del centro (frente a la gema)
    public float distanciaTigre = 0.035f;   // 3.5 cm del centro (frente a las garras del tigre)
    public float distanciaIsla = 0.045f;    // 4.5 cm del centro (en el borde exterior del santuario)

    [Header("Marcadores Secundarios (4)")]
    public ObserverBehaviour[] marcadoresSecundarios; // Marcadores 1 al 4

    [Header("Modelos Secundarios / Contenedores (4)")]
    public GameObject modeloManzana; // Contenedor_Apple
    public GameObject modeloTigre;   // Contenedor_Tigre
    public GameObject modeloGema;    // altar completo
    public GameObject modeloIsla;    // island

    private int pasoHistoria = 0;
    private bool seEstaMoviendo = false;
    private bool historiaIniciada = false;
    private Animator animatorPajaro;

    private ObserverBehaviour marcadorActualPajaro;
    private Dictionary<ObserverBehaviour, TipoElemento> asignacionMarcadores = new Dictionary<ObserverBehaviour, TipoElemento>();

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

        // 1. Regresar pájaro a su base e inicializarlo apagado
        if (marcadorPajaroBase != null && pajaro != null)
        {
            marcadorActualPajaro = marcadorPajaroBase;
            pajaro.transform.SetParent(marcadorPajaroBase.transform, false);
            pajaro.transform.localPosition = Vector3.zero;
            pajaro.transform.localRotation = Quaternion.identity;

            // Permanece oculto hasta que la cámara enfoque la carta nido
            pajaro.SetActive(false);
        }

        // 2. Barajar los modelos secundarios
        BarajarModelos();

        // 3. Restaurar modelos secundarios
        if (modeloManzana != null) modeloManzana.SetActive(false);
        if (modeloTigre != null) modeloTigre.SetActive(false);
        if (modeloIsla != null) modeloIsla.SetActive(false);

        if (modeloGema != null)
        {
            modeloGema.SetActive(false);
            Transform cristal = modeloGema.transform.Find("Gema");
            if (cristal != null) cristal.gameObject.SetActive(true);
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

            GameObject modeloAsignado = ObtenerGameObjectPorTipo(tipo);
            if (modeloAsignado != null && marcador != null)
            {
                modeloAsignado.transform.SetParent(marcador.transform, false);
                modeloAsignado.transform.localPosition = Vector3.zero;
                modeloAsignado.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private GameObject ObtenerGameObjectPorTipo(TipoElemento tipo)
    {
        switch (tipo)
        {
            case TipoElemento.Manzana: return modeloManzana;
            case TipoElemento.Tigre: return modeloTigre;
            case TipoElemento.Gema: return modeloGema;
            case TipoElemento.Isla: return modeloIsla;
            default: return null;
        }
    }

    private void ActualizarVisibilidadPorTracking()
    {
        if (seEstaMoviendo) return;

        // Visibilidad del pájaro
        if (pajaro != null && marcadorActualPajaro != null)
        {
            bool targetVisible = EsTargetVisible(marcadorActualPajaro);

            if (!historiaIniciada)
            {
                // Solo se ve al inicio si la carta nido está enfocada
                if (pajaro.activeSelf != targetVisible)
                {
                    pajaro.SetActive(targetVisible);
                }
            }
            else
            {
                // Una vez que comienza la travesía permanece activo
                if (!pajaro.activeSelf)
                {
                    pajaro.SetActive(true);
                }
            }
        }

        // Visibilidad de los modelos secundarios
        foreach (var par in asignacionMarcadores)
        {
            ObserverBehaviour marcador = par.Key;
            TipoElemento tipo = par.Value;
            GameObject go = ObtenerGameObjectPorTipo(tipo);

            if (go != null && marcador != null)
            {
                bool targetEnCamara = EsTargetVisible(marcador);

                // Si la manzana ya fue consumida, mantenerla apagada
                if (tipo == TipoElemento.Manzana && pasoHistoria > 0)
                {
                    go.SetActive(false);
                    continue;
                }

                if (go.activeSelf != targetEnCamara)
                {
                    go.SetActive(targetEnCamara);
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
        // Se congela el botón durante el vuelo o tras completar la misión final en la isla
        if (seEstaMoviendo || pasoHistoria >= 4) return;

        ObserverBehaviour marcadorDestino = ObtenerMarcadorDestinoValido();

        if (marcadorDestino == null)
        {
            if (textoHistoria != null)
                textoHistoria.text = "Enfoca la carta hacia donde quieres que vuele el pájaro.";
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

    private float ObtenerDistanciaFrenado(TipoElemento elemento)
    {
        switch (elemento)
        {
            case TipoElemento.Manzana: return distanciaManzana;
            case TipoElemento.Gema: return distanciaGema;
            case TipoElemento.Tigre: return distanciaTigre;
            case TipoElemento.Isla: return distanciaIsla;
            default: return 0.02f;
        }
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

        Vector3 posInicial = pajaro.transform.position;
        Vector3 posCentroDestino = destino.transform.position;

        // Vector horizontal en el plano de la mesa
        Vector3 direccion = (posCentroDestino - posInicial);
        direccion.y = 0;

        float distancia = direccion.magnitude;
        Vector3 direccionNorm = direccion.normalized;

        // Calcular punto de aterrizaje con margen de seguridad
        float margenSeguridad = ObtenerDistanciaFrenado(elemento);
        Vector3 posFinal = posCentroDestino;

        if (distancia > margenSeguridad)
        {
            posFinal = posCentroDestino - (direccionNorm * margenSeguridad);
        }

        posFinal.y = posCentroDestino.y;

        if (direccionNorm != Vector3.zero)
        {
            pajaro.transform.rotation = Quaternion.LookRotation(direccionNorm);
        }

        // Vuelo en arco parabólico
        float progreso = 0f;
        while (progreso < 1.0f)
        {
            progreso += Time.deltaTime * velocidadVuelo;
            Vector3 posActual = Vector3.Lerp(posInicial, posFinal, progreso);
            posActual.y += Mathf.Sin(progreso * Mathf.PI) * alturaArcoVuelo;

            pajaro.transform.position = posActual;
            yield return null;
        }

        pajaro.transform.position = posFinal;

        // Emparentar al nuevo marcador conservando su posición física
        pajaro.transform.SetParent(destino.transform, true);
        marcadorActualPajaro = destino;

        if (animatorPajaro != null)
        {
            animatorPajaro.SetBool("active", false);
        }

        EvaluarPaso(elemento);

        seEstaMoviendo = false;
    }

    private void EvaluarPaso(TipoElemento elemento)
    {
        switch (pasoHistoria)
        {
            case 0:
                if (elemento == TipoElemento.Manzana)
                {
                    if (animatorPajaro != null)
                    {
                        animatorPajaro.SetTrigger("comer");
                    }
                    StartCoroutine(ConsumirManzanaConAnimacion());
                }
                else
                {
                    textoHistoria.text = "El pájaro está débil de hambre. Necesita la Manzana Dorada primero.";
                }
                break;

            case 1:
                if (elemento == TipoElemento.Tigre)
                {
                    pasoHistoria = 2;
                    textoHistoria.text = "El Guardián dice: 'Has demostrado vigor. Encuentra la Gema Astral para encender el santuario'.";
                }
                else
                {
                    textoHistoria.text = "Debes presentar tus respetos al Guardián antes de continuar.";
                }
                break;

            case 2:
                if (elemento == TipoElemento.Gema)
                {
                    Transform cristal = modeloGema.transform.Find("Gema");
                    if (cristal != null) cristal.gameObject.SetActive(false);
                    else modeloGema.SetActive(false);

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
                    textoHistoria.text = "¡Misión cumplida! El portal de la Isla Flotante ha sido activado.";
                }
                break;

            case 4:
                textoHistoria.text = "Aventura completada. Presiona 'Reinicio' para barajar los marcadores.";
                break;
        }
    }

    private IEnumerator ConsumirManzanaConAnimacion()
    {
        yield return new WaitForSeconds(1.2f);
        if (modeloManzana != null) modeloManzana.SetActive(false);
        pasoHistoria = 1;
        textoHistoria.text = "¡El pájaro comió la Manzana Dorada y recuperó su energía! Ahora busca al Tigre Guardián.";
    }

    private void ActualizarTextoMision()
    {
        if (textoHistoria == null) return;

        switch (pasoHistoria)
        {
            case 0:
                textoHistoria.text = "Capítulo 1: El mensajero ha despertado débil. Encuentra la Manzana Dorada.";
                break;
            case 1:
                textoHistoria.text = "Capítulo 2: Con energía renovada, localiza al Tigre Guardián.";
                break;
            case 2:
                textoHistoria.text = "Capítulo 3: Encuentra la Gema Astral para activar el santuario.";
                break;
            case 3:
                textoHistoria.text = "Capítulo 4: Vuela hacia la Isla Flotante para completar la travesía.";
                break;
        }
    }
}