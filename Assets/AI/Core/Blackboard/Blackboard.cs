// ============================================================
//  EJERCICIO: BLACKBOARD — Parte 1 (Obligatorio)
// ============================================================
//
//  La pizarra (Blackboard) es un almacén genérico de datos compartidos.
//  Cualquier componente puede escribir o leer en ella usando una clave
//  de tipo string, sin saber quién escribió el dato ni quién lo leerá.
//
//  En proyectos anteriores ya usabas datos compartidos "a mano":
//
//    // De Enemigo_BT.cs:
//    Vector3 _lastKnownPos;   // → ahora: BB.LastKnownPosition
//    bool    _hasLastKnownPos;// → ahora: BB.HasClue
//
//  La pizarra formaliza ese patrón y lo hace extensible.
//
// ============================================================
//  QUÉ DEBES IMPLEMENTAR
// ============================================================
//
//  Los cuatro métodos marcados con TODO:
//    · Set<T>   — guardar un valor bajo una clave
//    · Get<T>   — recuperar un valor (con fallback si no existe)
//    · Has      — comprobar si una clave existe
//    · Remove   — eliminar una entrada
//
//  El campo _data ya está declarado; es el único estado interno
//  que necesita la pizarra.
//
//  PISTAS:
//    · Dictionary<string, object> admite cualquier tipo como valor.
//    · Para recuperar un valor tipado desde un object, usa un cast: (T)value
//    · TryGetValue devuelve false si la clave no existe.
//
// ============================================================

using System.Collections.Generic;

public class Blackboard
{
    readonly Dictionary<string, object> _data = new();

    // TODO [PARTE 1a]: Implementa Set<T>.
    // Guarda (o sobreescribe) el valor bajo la clave dada.
    public void Set<T>(string key, T value)
    {
        _data[key] = value;
    }

    // TODO [PARTE 1b]: Implementa Get<T>.
    // Devuelve el valor asociado a la clave.
    // Si la clave no existe, devuelve defaultValue.
    public T Get<T>(string key, T defaultValue = default)
    {
        if (_data.TryGetValue(key, out object value) && value is T typedValue)
            return typedValue;

        return defaultValue;
    }

    // TODO [PARTE 1c]: Implementa Has.
    // Devuelve true si la clave existe en la pizarra.
    public bool Has(string key)
    {
        return _data.ContainsKey(key);
    }

    // TODO [PARTE 1d]: Implementa Remove.
    // Elimina la entrada con esa clave si existe; no hace nada si no existe.
    public void Remove(string key)
    {
        _data.Remove(key);
    }
}
