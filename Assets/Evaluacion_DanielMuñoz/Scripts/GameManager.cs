using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int _Cantidadgemas;
    private int _CantidadMonedas;
    private int _CantidadJoyas;
    private int _totalPuntos;
    private float _elapsedTime; // Variable para acumular el tiempo total

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Acumula el tiempo total en segundos
        _elapsedTime += Time.deltaTime;
    }

    // Propiedades de cantidad de cada tipo de objeto recolectado
    public int Cantidadgemas
    {
        get => _Cantidadgemas;
        set
        {
            _Cantidadgemas = value;
            _totalPuntos += 10; // Valor por gema
        }
    }

    public int CantidadMonedas
    {
        get => _CantidadMonedas;
        set
        {
            _CantidadMonedas = value;
            _totalPuntos += 2; // Valor por moneda
        }
    }

    public int CantidadJoyas
    {
        get => _CantidadJoyas;
        set
        {
            _CantidadJoyas = value;
            _totalPuntos += 5; // Valor por joya
        }
    }

    // Propiedad para obtener el tiempo total acumulado
    public float ElapsedTime => _elapsedTime;

    // Propiedad para obtener los puntos totales acumulados
    public int TotalPuntos => _totalPuntos;

    // Método para agregar un objeto recolectado y actualizar el puntaje
    public void AddItem(string itemType)
    {
        switch (itemType)
        {
            case "Moneda":
                CantidadMonedas++;
                break;
            case "Joya":
                CantidadJoyas++;
                break;
            case "Gema":
                Cantidadgemas++;
                break;
        }
    }
}
