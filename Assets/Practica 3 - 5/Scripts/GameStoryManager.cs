using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class GameStoryManager : MonoBehaviour
{
    [Header("UI & Narrativa")]
    [SerializeField] private TextMeshProUGUI textoNarrativa;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Button botonSiguiente;
    [SerializeField] private Button botonReinicio;
    [SerializeField] private Button botonCambiarAccesorio;

    [Header("Protagonista")]
    [SerializeField] private GameObject robinPajaro;
    [SerializeField] private Animator robinAnimator;
    [SerializeField] private Renderer robinRenderer;
    [SerializeField] private float rotacionYFinal = 90f;
    [SerializeField] private Color colorNormalPajaro = Color.white;
    [SerializeField] private Color colorManzanaPajaro = new Color(1f, 0.85f, 0.2f); // Dorado

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

    // Progreso: 0 = Manzana | 1 = Tigre | 2 = Altar | 3 = Isla | 4 = Fin
    private int progresoHistoria = 0;
    private bool enVuelo = false;
    private int targetFisicoActual = 99; // 99 = Nido Base (ImageTarget 4)

    // Relación aleatoria
    private int[] dioramaEnTarget = new int[4];
    private int[] targetDeDiorama = new int[4];

    // Respaldo nido
    private Transform padreBase;
    private Vector3 posBaseLocal;
    private Quaternion rotBaseLocal;
    private Vector3 escalaBaseLocal;

    // Observers para tracking estricto
    private ObserverBehaviour observerBase;
    private List<ObserverBehaviour> observersSecundarios = new List<ObserverBehaviour>();

    private readonly string[] pistasMision = new string[]
    {
        "Misión actual: Encuentra el Manzano Sagrado para que el ave reciba su bendición.",
        "Misión actual: Encuentra al Guardián Silente para obtener su protección.",
        "Misión actual: Encuentra el Altar Místico para reclamar la reliquia real.",
        "Misión final: Encuentra la Isla del Origen para despertar el gran portal celestial."
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
        // Se desactiva el botón de accesorios al inicio hasta llegar al Altar.
        if (botonCambiarAccesorio != null)
        {
            botonCambiarAccesorio.gameObject.SetActive(false);
        }

        BarajarYReiniciar();
    }

    public void BarajarYReiniciar()
    {
        StopAllCoroutines();
        enVuelo = false;
        progresoHistoria = 0;
        targetFisicoActual = 99;
        accesorioDesbloqueado = false;
        indiceAccesorioActual = -1;

        // 1. Restaurar al Robin al nido
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

        // 2. Restaurar color original
        if (robinRenderer != null)
            robinRenderer.material.color = colorNormalPajaro;

        // 3. Restaurar manzana y gema
        if (manzanaObjeto != null)
        {
            manzanaObjeto.SetActive(true);
            manzanaObjeto.transform.localScale = Vector3.one;
        }
        if (gemaObjeto != null) gemaObjeto.SetActive(true);

        // 4. Apagar todos los sombreros y ocultar botón
        ApagarTodosLosAccesorios();
        if (botonCambiarAccesorio != null)
            botonCambiarAccesorio.gameObject.SetActive(false);

        // 5. Apagar luz de isla
        if (luzIslaBrillo != null)
        {
            luzIslaBrillo.intensity = 0f;
            luzIslaBrillo.enabled = false;
        }

        // 6. Barajar los 4 dioramas
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
                dioramas[d].SetActive(false);
            }
        }

        // 7. Encender questions
        if (questionsFisicos != null)
        {
            foreach (var q in questionsFisicos)
                if (q != null) q.SetActive(true);
        }

        ActualizarTextoConFade("El viaje comienza.\n" + pistasMision[0] + "\n\nEnfoca un marcador y pulsa 'Siguiente'.");
        if (botonSiguiente != null) botonSiguiente.interactable = true;
    }

    public void BotonSiguientePresionado()
    {
        if (enVuelo) return;

        if (progresoHistoria >= 4)
        {
            ActualizarTextoConFade("¡La historia ha finalizado! Pulsa 'Reinicio' para barajar los marcadores de nuevo.");
            return;
        }

        int targetVisto = DetectarTargetEnCamara();

        if (targetVisto == -1)
        {
            ActualizarTextoConFade("⚠️ Enfoca claramente con la cámara el marcador hacia donde deseas enviar al ave.");
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

        // 2. Emparentar directo al nuevo ImageTarget y viajar a (0, 0, 0)
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

        // Caso: Regreso al nido base
        if (targetIdx == 99)
        {
            targetFisicoActual = 99;
            ActualizarTextoConFade("El ave ha regresado a descansar a su nido base.\n\n" + pistasMision[progresoHistoria]);
            yield return new WaitForSeconds(2.0f);
            if (botonSiguiente != null && progresoHistoria < 4) botonSiguiente.interactable = true;
            enVuelo = false;
            yield break;
        }

        // ==========================================================
        // APAGAR QUESTION Y ACTIVAR ESCENA
        // ==========================================================
        if (questionsFisicos != null && targetIdx < questionsFisicos.Count && questionsFisicos[targetIdx] != null)
            questionsFisicos[targetIdx].SetActive(false);

        if (dioramas[dioramaIdx] != null)
            dioramas[dioramaIdx].SetActive(true);

        targetFisicoActual = targetIdx;

        // ==========================================================
        // CASOS NARRATIVOS Y ACCIONES
        // ==========================================================
        // CASO 1: MANZANA (dioramaIdx == 0)
        if (dioramaIdx == 0)
        {
            if (progresoHistoria == 0)
            {
                ActualizarTextoConFade("¡El ave encuentra el Manzano Sagrado!\nObserva el fruto místico...");

                yield return new WaitForSeconds(1.8f);

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
                ActualizarTextoConFade("¡El ave ha comido la manzana!\nSus plumas se impregnan del poder místico y resplandecen en oro.");
                yield return new WaitForSeconds(3.0f);

                progresoHistoria = 1;
                ActualizarTextoConFade("Paso completado.\n" + pistasMision[1]);
            }
            else
            {
                ActualizarTextoConFade("Has vuelto al Manzano, pero el ave ya posee la bendición dorada.\n\n" + pistasMision[progresoHistoria]);
            }
        }
        // CASO 2: TIGRE (dioramaIdx == 1)
        else if (dioramaIdx == 1)
        {
            if (progresoHistoria == 0)
            {
                ActualizarTextoConFade("¡Descubriste al Guardián Silente!\nSin embargo, el felino no percibe el poder místico en el ave.\n\n" + pistasMision[0]);
            }
            else if (progresoHistoria == 1)
            {
                ActualizarTextoConFade("¡El Guardián contempla las plumas doradas!\nEl noble tigre inclina su mirada y le concede paso libre.");
                yield return new WaitForSeconds(3.0f);

                progresoHistoria = 2;
                ActualizarTextoConFade("Paso completado.\n" + pistasMision[2]);
            }
            else
            {
                ActualizarTextoConFade("El Guardián reposa tranquilo.\n\n" + pistasMision[progresoHistoria]);
            }
        }
        // CASO 3: ALTAR (dioramaIdx == 2)
        else if (dioramaIdx == 2)
        {
            if (progresoHistoria == 0)
            {
                ActualizarTextoConFade("Llegaste al Altar Ancestral, pero la reliquia está dormida.\nEl ave aún no tiene el sustento sagrado.\n\n" + pistasMision[0]);
            }
            else if (progresoHistoria == 1)
            {
                ActualizarTextoConFade("El Altar Ancestral vibra débilmente, pero rechaza la presencia del ave.\nNecesitas la bendición del Guardián primero.\n\n" + pistasMision[1]);
            }
            else if (progresoHistoria == 2)
            {
                accesorioDesbloqueado = true;
                ActivarAccesorioPorIndice(0); // 0: Crown

                if (gemaObjeto != null) gemaObjeto.SetActive(false);

                if (botonCambiarAccesorio != null)
                    botonCambiarAccesorio.gameObject.SetActive(true);

                ActualizarTextoConFade("¡El Altar Ancestral despierta!\nLa reliquia corona solemnemente al ave como explorador real.");
                yield return new WaitForSeconds(3.5f);

                progresoHistoria = 3;
                ActualizarTextoConFade("Paso completado. (¡Usa el botón inferior para probar sombreros!)\n\n" + pistasMision[3]);
            }
            else
            {
                ActualizarTextoConFade("El Altar ya otorgó su corona.\n\n" + pistasMision[progresoHistoria]);
            }
        }
        // CASO 4: ISLA (dioramaIdx == 3)
        else if (dioramaIdx == 3)
        {
            if (progresoHistoria < 3)
            {
                ActualizarTextoConFade("Divisas la Isla del Origen, pero el gran portal está inerte.\nDebes reunir las tres bendiciones previas.\n\n" + pistasMision[progresoHistoria]);
            }
            else if (progresoHistoria == 3)
            {
                if (luzIslaBrillo != null)
                {
                    luzIslaBrillo.enabled = true;
                    luzIslaBrillo.color = new Color(0.4f, 0.9f, 1f);
                    luzIslaBrillo.range = 8.0f;
                    StartCoroutine(EfectoBrilloPortal(7.0f));
                }

                ActualizarTextoConFade("¡Has alcanzado la cima de la Isla del Origen!\nCon el oro, la bendición y la corona, el gran portal resplandece en gloria.\n\n¡La aventura ha culminado con éxito total!");
                progresoHistoria = 4;
            }
        }

        if (botonSiguiente != null && progresoHistoria < 4)
            botonSiguiente.interactable = true;

        enVuelo = false;
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
            t += Time.deltaTime * 1.5f;
            luzIslaBrillo.intensity = Mathf.Lerp(0f, intensidadFinal, t);
            yield return null;
        }
        luzIslaBrillo.intensity = intensidadFinal;
    }

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

        // Candado de 0.25s para prevenir dobles clics en pantallas táctiles
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
        BarajarYReiniciar();
    }

    private void ActualizarTextoConFade(string nuevoTexto)
    {
        if (panelCanvasGroup != null)
            StartCoroutine(RutinaFade(nuevoTexto));
        else if (textoNarrativa != null)
            textoNarrativa.text = nuevoTexto;
    }

    private IEnumerator RutinaFade(string nuevoTexto)
    {
        for (float a = 1f; a >= 0f; a -= Time.deltaTime * 4f)
        {
            panelCanvasGroup.alpha = a;
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;
        textoNarrativa.text = nuevoTexto;
        for (float a = 0f; a <= 1f; a += Time.deltaTime * 4f)
        {
            panelCanvasGroup.alpha = a;
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;
    }
}