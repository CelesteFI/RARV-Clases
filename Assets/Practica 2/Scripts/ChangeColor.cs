using UnityEngine;
using System.Collections;
using System.Collections.Generic;   

public class ChangeColor : MonoBehaviour
{
    public GameObject[] model; // The object whose color will be changed
    private int ultimoElemento = -1; // To keep track of the last selected element
    private int ultimoColor = -1; // To keep track of the last selected color index

    private Color color;

    // Paleta de colores vivos 
    private Color[] paletaColores = new Color[]
    {
        // --- CÁLIDOS ---
        new Color(1f, 0.05f, 0.05f),   // Rojo 
        new Color(1f, 0.35f, 0.0f),    // Naranja 
        new Color(1f, 0.55f, 0.0f),    // Ámbar 
        new Color(1f, 0.85f, 0.0f),    // Amarillo oro
        new Color(1f, 1f, 0.15f),      // Amarillo neón
        new Color(1f, 0.2f, 0.45f),    // Fresa

        // --- VERDES ---
        new Color(0.15f, 1f, 0.15f),   // Verde neón
        new Color(0.45f, 1f, 0.05f),   // Verde lima
        new Color(0.0f, 0.95f, 0.55f),  // Verde esmeralda claro
        new Color(0.1f, 0.8f, 0.3f),   // Verde selva 

        // --- AZULES ---
        new Color(0.0f, 0.95f, 1f),    // Cian
        new Color(0.2f, 0.7f, 1f),     // Azul cielo
        new Color(0.1f, 0.35f, 1f),    // Azul cobalto
        new Color(0.0f, 0.8f, 0.8f),   // Turquesa 
        // --- MORADOS Y ROSAS ---
        new Color(1f, 0.05f, 0.7f),    // Rosa fucsia
        new Color(1f, 0.4f, 0.85f),    // Rosa chicle
        new Color(0.7f, 0.15f, 1f),    // Púrpura eléctrico
        new Color(0.5f, 0.05f, 1f),    // Violeta intenso
        new Color(0.85f, 0.55f, 1f),   // Lavanda

        // --- PASTELES Y METÁLICOS ---
        new Color(1f, 0.75f, 0.8f),    // Rosa pastel 
        new Color(0.6f, 1f, 0.9f),     // Aguamarina pastel
        new Color(1f, 0.9f, 0.6f),     // Melocotón
        new Color(0.75f, 0.9f, 1f),    // Azul glacial

    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
          
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChanegeColor_Reset()
    {
        // Reset the color of all models to white
        for (int i = 0; i < model.Length; i++)
        {
            if (model[i] != null)
            {
                model[i].GetComponent<Renderer>().material.color = Color.white;
            }
        }
        // Reset the last selected element and color index
        ultimoElemento = -1;
        ultimoColor = -1;
    }

    public void ChangeColor_Boton()
    {
        // 1. Elegir un elemento DIFERENTE al anterior
        int indiceModelo;
        do
        {
            indiceModelo = Random.Range(0, model.Length);
        } while (indiceModelo == ultimoElemento);

        ultimoElemento = indiceModelo;

        // 2. Generar un COLOR DIFERENTE 
        int indiceColor;
        do
        {
            indiceColor = Random.Range(0, paletaColores.Length);
        } while (indiceColor == ultimoColor);

        ultimoColor = indiceColor;

        color = paletaColores[indiceColor];

        // 3. Aplicar el color al material del elemento elegido
        model[indiceModelo].GetComponent<Renderer>().material.color = color;
    }
}
