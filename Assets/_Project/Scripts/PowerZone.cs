using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Zona donde la entidad es más fuerte. Va en un objeto con Collider marcado "Is Trigger".
// Mientras el jugador está dentro, las luces de "lights" fallan; al salir se restauran.
// Los eventos permiten conectar lo demás sin tocar este script (por ejemplo, hacer aparecer
// a la entidad con ApproachingEntity.Begin al entrar, y quitarla con Stop al salir).
public class PowerZone : MonoBehaviour
{
    public Light[] lights;                            // luces de la zona (las que ya parpadean se dejan como están)
    [Range(0, 1)] public float minFactor = 0.05f;     // cuánto baja la luz al fallar
    public float blackoutChance = 0.02f;              // probabilidad por fotograma de un apagón corto
    public UnityEvent onPlayerEnter = new UnityEvent();
    public UnityEvent onPlayerExit = new UnityEvent();

    readonly List<FlickerLight> added = new List<FlickerLight>();
    readonly Dictionary<Light, float> originals = new Dictionary<Light, float>();
    bool inside;

    void OnTriggerEnter(Collider other)
    {
        if (inside || !other.CompareTag("Player")) return;
        inside = true;
        Corrupt();
        onPlayerEnter.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (!inside || !other.CompareTag("Player")) return;
        inside = false;
        Restore();
        onPlayerExit.Invoke();
    }

    void Corrupt()
    {
        foreach (Light l in lights)
        {
            if (l == null || l.GetComponent<FlickerLight>() != null) continue;
            originals[l] = l.intensity;
            FlickerLight f = l.gameObject.AddComponent<FlickerLight>();
            f.minFactor = minFactor;
            f.blackoutChance = blackoutChance;
            f.speed = 14f;
            added.Add(f);
        }
    }

    void Restore()
    {
        foreach (FlickerLight f in added)
        {
            if (f == null) continue;
            f.enabled = false;
            Light l = f.GetComponent<Light>();
            if (originals.ContainsKey(l)) l.intensity = originals[l];
            Destroy(f);
        }
        added.Clear();
        originals.Clear();
    }

    void OnDisable()
    {
        if (inside) { inside = false; Restore(); }
    }
}
