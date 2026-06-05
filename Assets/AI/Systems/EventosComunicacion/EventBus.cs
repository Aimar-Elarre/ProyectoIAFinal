
using System;
using System.Collections.Generic;
using UnityEngine;

public class DatosEvento
{
    public string tipo;           // nombre del evento
    public Vector3 posicion;      // posición del emisor o del evento
    public GameObject emisor;     // quién lo envió (puede ser null)
    public object datos;          // payload extra opcional

    public DatosEvento(string tipo, Vector3 posicion, GameObject emisor = null, object datos = null)
    {
        this.tipo     = tipo;
        this.posicion = posicion;
        this.emisor   = emisor;
        this.datos    = datos;
    }
}

public static class EventBus
{
    public const string EnemigoDetectado  = "EnemigoDetectado";
    public const string BajoFuego         = "BajoFuego";
    public const string AliadoCaido       = "AliadoCaido";
    public const string ObjetivoEliminado = "ObjetivoEliminado";
    public const string SolicitarAyuda    = "SolicitarAyuda";

    static readonly Dictionary<string, List<Action<DatosEvento>>> _suscriptores =
        new Dictionary<string, List<Action<DatosEvento>>>();

    public static void Suscribir(string tipo, Action<DatosEvento> callback)
    {
        if (!_suscriptores.ContainsKey(tipo))
            _suscriptores[tipo] = new List<Action<DatosEvento>>();
        _suscriptores[tipo].Add(callback);
    }

    public static void Desuscribir(string tipo, Action<DatosEvento> callback)
    {
        if (_suscriptores.TryGetValue(tipo, out var lista))
            lista.Remove(callback);
    }

    public static void Publicar(DatosEvento evento)
    {
        if (!_suscriptores.TryGetValue(evento.tipo, out var lista)) return;

        var copia = new List<Action<DatosEvento>>(lista);
        foreach (var cb in copia)
            cb?.Invoke(evento);
    }

    public static void LimpiarTodo() => _suscriptores.Clear();
}
