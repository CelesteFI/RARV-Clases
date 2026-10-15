using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class GameStoryManager : MonoBehaviour
{
    [Header("1. Portada / Pantalla de Inicio")]
    [Tooltip("Arrastra Panel_Inicio")]
    [SerializeField] private GameObject panelInicio;
    [Tooltip("Arrastra Start (Button)")]
    [SerializeField] private Button botonComenzar;

    [Header("2. HUD Jugable (Contenedor Principal de Juego)")]
    [Tooltip("Arrastra Panel_Jugable")]
    [SerializeField] private GameObject panelJugable;

    [Header("3. Barra de Progreso y Misión")]
    [Tooltip("Arrastra Barra Progreso (Slider)")]
    [SerializeField] private Slider sliderProgreso;
    [Tooltip("Arrastra Texto_MisionActiva")]
    [SerializeField] private TextMeshProUGUI textoMisionActiva;
    [Tooltip("Opcional: Si tienes texto de porcentaje, sino None")]
    [SerializeField] private TextMeshProUGUI textoPorcentaje;

    [Header("4. Panel de Texto (Narrativa e Indicaciones)")]
    [Tooltip("Arrastra TextoMision")]
    [SerializeField] private TextMeshProUGUI textoNarrativa;
    [Tooltip("Arrastra Panel_Texto (Canvas Group)")]
    [SerializeField] private CanvasGroup panelTextoCanvasGroup;

    [Header("Botones de Control")]
    [SerializeField] private Button botonSiguiente;
    [SerializeField] private Button botonReinicio;
    [SerializeField] private Button botonCambiarAccesorio;

    [Header("5. Pantalla de Victoria (WINNER)")]
    [Tooltip("Arrastra Panel_Victoria")]
    [SerializeField] private GameObject panelVictoria;
    [Tooltip("Arrastra VolverEmpezar (Button)")]
    [SerializeField] private Button botonVolverEmpezar;

    [Header("Protagonista")]
    [SerializeField] private GameObject robinPajaro;
    [SerializeField] private Animator robinAnimator;
    [SerializeField] private Renderer robinRenderer;
    [SerializeField] private float rotacionYFinal = 90f;
    [SerializeField] private Color colorNormalPajaro = Color.white;
    [SerializeField] private Color colorManzanaPajaro = new Color(1f, 0.82f, 0.15f); // Dorado resplandeciente

    [Header("Accesorios (Sombreros en Head)")]
    [Tooltip("En orden: 0: Crown, 1: TopHat, 2: mountie_hat, 3: Cowboy")]
    [SerializeField] private List<GameObject> listaAccesorios;
    private int indiceAccesorioActual = -1;
    private bool accesorioDesbloqueado = false;
    private float ultimoTiempoClickAccesorio = 0f;

    [Header("Marcadores Físicos Secundarios (4 ImageTargets)")]
    [Tooltip("Arrastra ImageTarget, (1), (2), (3)")]
    [SerializeField] private List<Transform> targetsFisicos;

    [Tooltip("Los 4 Question flotantes sobre cada ImageTarget")]
    [SerializeField] private List<GameObject> questionsFisicos;

    [Header("Contenedores de Escena (0:Manzana, 1:Tigre, 2:Altar, 3:Isla)")]
    [SerializeField] private List<GameObject> dioramas;

    [Header("Elementos de Escena Específicos")]
    [SerializeField] private GameObject manzanaObjeto;
    [SerializeField] private GameObject gemaObjeto;
    [SerializeField] private Light luzIslaBrillo;

    // Progreso: 0 = Manzana | 1 = Tigre | 2 = Altar | 3 = Isla | 4 = Victoria Total
    private int progresoHistoria = 0;
    private bool enVuelo = false;
    private int targetFisicoActual = 99; // 99 = Nido Base (ImageTarget 4)

    // Relación aleatoria
    private int[] dioramaEnTarget = new int[4];
    private int[] targetDeDiorama = new int[4];

    // Respaldo nido base
    private Transform padreBase;
    private Vector3 posBaseLocal;
    private Quaternion rotBaseLocal;
    private Vector3 escalaBaseLocal;

    // Tracking estricto de Vuforia
    private ObserverBehaviour observerBase;
    private List<ObserverBehaviour> observersSecundarios = new List<ObserverBehaviour>();

    // Misiones activas visibles en el recuadro superior
    private readonly string[] pistasMision = new string[]
    {
        "Misión: Encuentra el Manzano Sagrado y recolecta el fruto místico.",
        "Misión: Busca al Guardián Silente y gana su bendición protectora.",
        "Misión: Localiza el Altar Ancestral para despertar la reliquia real.",
        "Misión Final: Descubre la Isla del Origen y enciende el gran portal celestial."
    };

    void Awake()
    {
        if (robinPajaro != null)
        {
            padreBase = robinPajaro.transform.parent;
            posBaseLocal = robinPajaro.transform.localPosition;
            rotBaseLocal = robinPajaro.transform.localRotation;
            escalaBaseLocal = robinPajaro.transform.localScale;

            if (robinAnimator == null)
                robinAnimator = robinPajaro.GetComponentInChildren<Animator>();

            if (padreBase != null)
                observerBase = padreBase.GetComponent<ObserverBehaviour>();
        }

        observersSecundarios.Clear();
        foreach (var t in targetsFisicos)
        {
            if (t != null)
                observersSecundarios.Add(t.GetComponent<ObserverBehaviour>());
            else
                observersSecundarios.Add(null);
        }
    }

    void Start()
    {
        // Conexión por seguridad (además de la que ya tienes en el Inspector)
        if (botonComenzar != null)
        {
            botonComenzar.onClick.RemoveAllListeners();
            botonComenzar.onClick.AddListener(BotonComenzarPresionado);
        }

        if (botonVolverEmpezar != null)
        {
            botonVolverEmpezar.onClick.RemoveAllListeners();
            botonVolverEmpezar.onClick.AddListener(BotonReinicioPresionado);
        }

        // Mostrar pantalla de inicio al abrir la aplicación
        MostrarPantallaInicio();
    }

    public void MostrarPantallaInicio()
    {
        StopAllCoroutines();
        enVuelo = false;

        if (panelInicio != null) panelInicio.SetActive(true);
        if (panelJugable != null) panelJugable.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);

        ResetearMundoYVariables();
    }

    public void BotonComenzarPresionado()
    {
        if (panelInicio != null) panelInicio.SetActive(false);
        if (panelVictoria != null) panelVictoria.SetActive(false);
        if (panelJugable != null) panelJugable.SetActive(true);

        BarajarYArrancarPartida();
    }

    private void ResetearMundoYVariables()
    {
        progresoHistoria = 0;
        targetFisicoActual = 99; // Nido base
        accesorioDesbloqueado = false;
        indiceAccesorioActual = -1;

        // 1. Restaurar al Robin al nido base
        if (robinPajaro != null)
        {
            robinPajaro.transform.SetParent(padreBase);
            robinPajaro.transform.localPosition = posBaseLocal;
            robinPajaro.transform.localRotation = rotBaseLocal;
            robinPajaro.transform.localScale = escalaBaseLocal;
            robinPajaro.SetActive(true);

            if (robinAnimator != null)
            {
                robinAnimator.SetBool("active", false);
                robinAnimator.Play("Robin_Bird_Idle");
            }
        }

        // 2. Restaurar color original del ave
        if (robinRenderer != null)
            robinRenderer.material.color = colorNormalPajaro;

        // 3. Restaurar manzana y gema
        if (manzanaObjeto != null)
        {
            manzanaObjeto.SetActive(true);
            manzanaObjeto.transform.localScale = Vector3.one;
        }
        if (gemaObjeto != null)
        {
            gemaObjeto.SetActive(true);
            gemaObjeto.transform.localScale = Vector3.one;
        }

        // 4. Sombreros apagados y ocultar botón accesorio
        ApagarTodosLosAccesorios();
        if (botonCambiarAccesorio != null)
            botonCambiarAccesorio.gameObject.SetActive(false);

        // 5. Apagar portal de la isla
        if (luzIslaBrillo != null)
        {
            luzIslaBrillo.intensity = 0f;
            luzIslaBrillo.enabled = false;
        }

        ActualizarBarraProgresoUI(0);
    }

    public void BarajarYArrancarPartida()
    {
        ResetearMundoYVariables();

        // Barajar aleatoriamente los 4 dioramas entre los 4 targets
        List<int> disponibles = new List<int> { 0, 1, 2, 3 };
        for (int i = disponibles.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            int temp = disponibles[i];
            disponibles[i] = disponibles[rnd];
            disponibles[rnd] = temp;
        }

        for (int d = 0; d < dioramas.Count; d++)
        {
            int targetAsignado = disponibles[d];
            dioramaEnTarget[targetAsignado] = d;
            targetDeDiorama[d] = targetAsignado;

            if (dioramas[d] != null && targetAsignado < targetsFisicos.Count)
            {
                dioramas[d].transform.SetParent(targetsFisicos[targetAsignado], false);
                dioramas[d].SetActive(false); // Oculto tras el question
            }
        }

        // Encender todos los questions misteriosos
        if (questionsFisicos != null)
        {
            foreach (var q in questionsFisicos)
                if (q != null) q.SetActive(true);
        }

        ActualizarTextoNarrativaConFade("El viaje comienza.\nEnfoca con la cámara un marcador misterioso y presiona el botón 'Play' para enviar al ave.");
        if (botonSiguiente != null) botonSiguiente.interactable = true;
    }

    public void BotonSiguientePresionado()
    {
        if (enVuelo) return;

        if (progresoHistoria >= 4)
        {
            MostrarModalVictoria();
            return;
        }

        int targetVisto = DetectarTargetEnCamara();

        if (targetVisto == -1)
        {
            ActualizarTextoNarrativaConFade("⚠️ Enfoca con claridad uno de los marcadores con la cámara antes de avanzar.");
            return;
        }

        int dioramaDestino = (targetVisto == 99) ? -1 : dioramaEnTarget[targetVisto];
        StartCoroutine(RutinaVuelo(targetVisto, dioramaDestino));
    }

    private bool EstaTargetTrackeado(ObserverBehaviour obs)
    {
        if (obs == null) return false;
        return (obs.TargetStatus.Status == Status.TRACKED);
    }

    private int DetectarTargetEnCamara()
    {
        for (int i = 0; i < targetsFisicos.Count; i++)
        {
            if (i == targetFisicoActual) continue;

            if (i < observersSecundarios.Count && EstaTargetTrackeado(observersSecundarios[i]))
            {
                return i;
            }
        }

        if (targetFisicoActual != 99 && EstaTargetTrackeado(observerBase))
        {
            return 99;
        }

        return -1;
    }

    private IEnumerator RutinaVuelo(int targetIdx, int dioramaIdx)
    {
        enVuelo = true;
        if (botonSiguiente != null) botonSiguiente.interactable = false;

        Transform targetDestino = (targetIdx == 99) ? padreBase : targetsFisicos[targetIdx];

        // 1. Activar animación de vuelo
        if (robinAnimator != null)
        {
            robinAnimator.SetBool("active", true);
        }

        // 2. Emparentar y volar hacia Vector3.zero
        if (robinPajaro != null && targetDestino != null)
        {
            robinPajaro.transform.SetParent(targetDestino, true);

            Vector3 posLocalInicio = robinPajaro.transform.localPosition;
            Quaternion rotLocalInicio = robinPajaro.transform.localRotation;

            Vector3 posLocalFin = Vector3.zero;
            Quaternion rotLocalFin = Quaternion.Euler(0f, rotacionYFinal, 0f);

            float duracion = 1.4f;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime / duracion;

                robinPajaro.transform.localPosition = Vector3.Lerp(posLocalInicio, posLocalFin, t);
                robinPajaro.transform.localRotation = Quaternion.Slerp(rotLocalInicio, rotLocalFin, t);
                yield return null;
            }

            robinPajaro.transform.localPosition = Vector3.zero;
            robinPajaro.transform.localRotation = rotLocalFin;
            robinPajaro.transform.localScale = escalaBaseLocal;
        }

        // 3. Regresar a Idle
        if (robinAnimator != null)
        {
            robinAnimator.SetBool("active", false);
        }

        // Retorno al nido base
        if (targetIdx == 99)
        {
            targetFisicoActual = 99;
            ActualizarTextoNarrativaConFade("El ave regresa a descansar a su nido de origen.");
            yield return new WaitForSeconds(2.0f);
            if (botonSiguiente != null && progresoHistoria < 4) botonSiguiente.interactable = true;
            enVuelo = false;
            yield break;
        }

        // ==========================================================
        // APAGAR QUESTION Y REVELAR DIORAMA
        // ==========================================================
        if (questionsFisicos != null && targetIdx < questionsFisicos.Count && questionsFisicos[targetIdx] != null)
            questionsFisicos[targetIdx].SetActive(false);

        if (dioramas[dioramaIdx] != null)
            dioramas[dioramaIdx].SetActive(true);

        targetFisicoActual = targetIdx;

        // ==========================================================
        // CASOS NARRATIVOS Y PROGRESIÓN
        // ==========================================================
        // CASO 1: MANZANA (dioramaIdx == 0)
        if (dioramaIdx == 0)
        {
            if (progresoHistoria == 0)
            {
                ActualizarTextoNarrativaConFade("¡Has hallado el Manzano Sagrado!\nObserva el fruto reluciente...");
                yield return new WaitForSeconds(2.0f);

                if (robinAnimator != null) robinAnimator.SetTrigger("comer");
                yield return new WaitForSeconds(1.2f);

                if (manzanaObjeto != null)
                {
                    Vector3 escalaIni = manzanaObjeto.transform.localScale;
                    float elapsed = 0f;
                    while (elapsed < 1f)
                    {
                        elapsed += Time.deltaTime * 1.5f;
                        manzanaObjeto.transform.localScale = Vector3.Lerp(escalaIni, Vector3.zero, elapsed);
                        yield return null;
                    }
                    manzanaObjeto.SetActive(false);
                }

                StartCoroutine(EfectoColor(colorManzanaPajaro));
                ActualizarTextoNarrativaConFade("¡El ave consume el fruto sagrado!\nSus plumas se impregnan de energía y resplandecen en oro puro.");
                yield return new WaitForSeconds(3.0f);

                progresoHistoria = 1;
                ActualizarBarraProgresoUI(1);
                ActualizarTextoNarrativaConFade("¡Paso completado!\nEnfoca otro marcador misterioso para buscar al Guardián.");
            }
            else
            {
                ActualizarTextoNarrativaConFade("El Manzano Sagrado florece en paz. El ave ya posee la bendición dorada.");
            }
        }
        // CASO 2: TIGRE (dioramaIdx == 1)
        else if (dioramaIdx == 1)
        {
            if (progresoHistoria == 0)
            {
                ActualizarTextoNarrativaConFade("¡Has encontrado al Guardián Silente!\nSin embargo, la fiera no detecta poder místico en el ave. Busca primero el Manzano.");
            }
            else if (progresoHistoria == 1)
            {
                ActualizarTextoNarrativaConFade("¡El Guardián contempla las plumas doradas!\nEl noble felino reconoce tu valía y te concede el paso libre.");
                yield return new WaitForSeconds(3.5f);

                progresoHistoria = 2;
                ActualizarBarraProgresoUI(2);
                ActualizarTextoNarrativaConFade("¡Paso completado!\nEnfoca otro marcador misterioso en busca del Altar Ancestral.");
            }
            else
            {
                ActualizarTextoNarrativaConFade("El Guardián Silente reposa satisfecho tras otorgar su bendición.");
            }
        }
        // CASO 3: ALTAR (dioramaIdx == 2)
        else if (dioramaIdx == 2)
        {
            if (progresoHistoria == 0)
            {
                ActualizarTextoNarrativaConFade("Llegas al Altar Ancestral, pero está inerte. El explorador aún no ha comido el fruto sagrado.");
            }
            else if (progresoHistoria == 1)
            {
                ActualizarTextoNarrativaConFade("La piedra vibra tímidamente, pero la reliquia permanece sellada. Te falta la aprobación del Guardián.");
            }
            else if (progresoHistoria == 2)
            {
                ActualizarTextoNarrativaConFade("¡El Altar Ancestral responde a la bendición del Guardián!\nLa gema sagrada resuena intensamente...");
                yield return new WaitForSeconds(2.5f);

                if (gemaObjeto != null)
                {
                    Vector3 escalaIniGema = gemaObjeto.transform.localScale;
                    float elapsed = 0f;
                    while (elapsed < 1f)
                    {
                        elapsed += Time.deltaTime * 1.5f;
                        gemaObjeto.transform.localScale = Vector3.Lerp(escalaIniGema, Vector3.zero, elapsed);
                        yield return null;
                    }
                    gemaObjeto.SetActive(false);
                }

                accesorioDesbloqueado = true;
                ActivarAccesorioPorIndice(0); // Corona

                if (botonCambiarAccesorio != null)
                    botonCambiarAccesorio.gameObject.SetActive(true);

                ActualizarTextoNarrativaConFade("¡La energía de la reliquia corona solemnemente al ave como Monarca del Vuelo!");
                yield return new WaitForSeconds(3.5f);

                progresoHistoria = 3;
                ActualizarBarraProgresoUI(3);
                ActualizarTextoNarrativaConFade("¡Reliquia obtenida!\n(¡Ahora puedes probar los sombreros con el botón de doble flecha!)\nBusca el marcador de la Isla final.");
            }
            else
            {
                ActualizarTextoNarrativaConFade("El Altar ya otorgó su reliquia a la corona del explorador.");
            }
        }
        // CASO 4: ISLA (dioramaIdx == 3)
        else if (dioramaIdx == 3)
        {
            if (progresoHistoria < 3)
            {
                ActualizarTextoNarrativaConFade("Divisas el Santuario del Origen, pero las puertas místicas exigen reunir todas las bendiciones previas.");
            }
            else if (progresoHistoria == 3)
            {
                if (luzIslaBrillo != null)
                {
                    luzIslaBrillo.enabled = true;
                    luzIslaBrillo.color = new Color(1f, 0.78f, 0.25f);
                    luzIslaBrillo.range = 20.0f;
                    StartCoroutine(EfectoBrilloPortal(15.0f));
                }

                progresoHistoria = 4;
                ActualizarBarraProgresoUI(4);

                ActualizarTextoNarrativaConFade("¡Has alcanzado la cumbre del Santuario del Origen!\n¡El gran portal celestial se abre en gloria!");
                yield return new WaitForSeconds(2.5f);

                MostrarModalVictoria();
            }
        }

        if (botonSiguiente != null && progresoHistoria < 4)
            botonSiguiente.interactable = true;

        enVuelo = false;
    }

    private void ActualizarBarraProgresoUI(int paso)
    {
        float ratio = Mathf.Clamp01((float)paso / 4f);

        if (sliderProgreso != null)
        {
            sliderProgreso.minValue = 0f;
            sliderProgreso.maxValue = 1f;
            sliderProgreso.value = ratio;
        }

        if (textoPorcentaje != null)
        {
            textoPorcentaje.text = $"{Mathf.RoundToInt(ratio * 100)}%";
        }

        if (textoMisionActiva != null && paso >= 0 && paso < pistasMision.Length)
        {
            textoMisionActiva.text = pistasMision[paso];
        }
    }

    private void MostrarModalVictoria()
    {
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }
    }

    private IEnumerator EfectoColor(Color objetivo)
    {
        Color inicial = robinRenderer.material.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            robinRenderer.material.color = Color.Lerp(inicial, objetivo, t);
            yield return null;
        }
        robinRenderer.material.color = objetivo;
    }

    private IEnumerator EfectoBrilloPortal(float intensidadFinal)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.2f;
            luzIslaBrillo.intensity = Mathf.Lerp(0f, intensidadFinal, t);
            yield return null;
        }
        luzIslaBrillo.intensity = intensidadFinal;
    }

    // ==============================================================
    // GESTIÓN ESTRICTA DE SOMBREROS
    // ==============================================================
    private void ApagarTodosLosAccesorios()
    {
        if (listaAccesorios == null) return;
        for (int i = 0; i < listaAccesorios.Count; i++)
        {
            if (listaAccesorios[i] != null)
                listaAccesorios[i].SetActive(false);
        }
    }

    private void ActivarAccesorioPorIndice(int idx)
    {
        if (listaAccesorios == null || listaAccesorios.Count == 0) return;

        ApagarTodosLosAccesorios();

        if (idx >= 0 && idx < listaAccesorios.Count && listaAccesorios[idx] != null)
        {
            listaAccesorios[idx].SetActive(true);
            indiceAccesorioActual = idx;
        }
    }

    public void BotonCiclarAccesorioPresionado()
    {
        if (!accesorioDesbloqueado || listaAccesorios == null || listaAccesorios.Count == 0) return;

        if (Time.time - ultimoTiempoClickAccesorio < 0.25f) return;
        ultimoTiempoClickAccesorio = Time.time;

        int siguienteIndice = (indiceAccesorioActual + 1) % listaAccesorios.Count;
        ActivarAccesorioPorIndice(siguienteIndice);

        if (textoNarrativa != null && listaAccesorios[siguienteIndice] != null)
        {
            textoNarrativa.text = $"Sombrero equipado: {listaAccesorios[siguienteIndice].name} ({siguienteIndice + 1}/{listaAccesorios.Count})";
        }
    }

    public void BotonReinicioPresionado()
    {
        MostrarPantallaInicio();
    }

    private void ActualizarTextoNarrativaConFade(string nuevoTexto)
    {
        if (panelTextoCanvasGroup != null)
            StartCoroutine(RutinaFade(nuevoTexto));
        else if (textoNarrativa != null)
            textoNarrativa.text = nuevoTexto;
    }

    private IEnumerator RutinaFade(string nuevoTexto)
    {
        for (float a = 1f; a >= 0f; a -= Time.deltaTime * 4f)
        {
            panelTextoCanvasGroup.alpha = a;
            yield return null;
        }
        panelTextoCanvasGroup.alpha = 0f;
        textoNarrativa.text = nuevoTexto;
        for (float a = 0f; a <= 1f; a += Time.deltaTime * 4f)
        {
            panelTextoCanvasGroup.alpha = a;
            yield return null;
        }
        panelTextoCanvasGroup.alpha = 1f;
    }
}