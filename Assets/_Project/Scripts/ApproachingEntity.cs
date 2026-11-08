using System.Collections;
using UnityEngine;

// La entidad (el doppelgänger) mostrando su verdadera forma: aparece al fondo de una zona
// y camina lenta y directamente hacia el jugador. Si lo alcanza lo golpea (jumpscare + daño)
// y desaparece. Se puede evitar saliendo de la zona: Stop() la retira.
// Este script va en un objeto SIEMPRE ACTIVO; "figure" es la figura visual (hija, empieza desactivada).
// Se conecta a una PowerZone: onPlayerEnter -> Begin, onPlayerExit -> Stop.
public class ApproachingEntity : MonoBehaviour
{
    public GameObject figure;            // temporal (TEMP_Entity) hasta tener el modelo real
    public Transform spawnPoint;         // dónde aparece
    public Jumpscare jumpscare;          // opcional: susto al alcanzarte
    public float appearDelay = 10f;      // segundos desde que Begin() se llama hasta que aparece
    public float speed = 1.4f;           // el jugador camina a 2.6, así que se le puede ganar
    public float attackDistance = 1.2f;
    public AudioSource appearSound;      // opcional

    Coroutine routine;
    Transform player;

    // Si ya está en marcha no hace nada
    public void Begin()
    {
        if (routine != null) return;
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) return;
        player = p.transform;
        routine = StartCoroutine(Run());
    }

    public void Stop()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (figure != null) figure.SetActive(false);
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(appearDelay);

        figure.transform.position = spawnPoint.position;
        figure.SetActive(true);
        if (appearSound != null) appearSound.Play();

        while (true)
        {
            Vector3 to = player.position - figure.transform.position;
            to.y = 0;
            if (to.magnitude <= attackDistance) break;

            figure.transform.rotation = Quaternion.LookRotation(to);
            figure.transform.position += to.normalized * speed * Time.deltaTime;
            yield return null;
        }

        Strike();
    }

    void Strike()
    {
        figure.SetActive(false);
        routine = null;

        PlayerHealth health = PlayerHealth.Instance;
        if (health == null || health.IsDead) return;
        if (jumpscare != null) jumpscare.Play(health.IsLethalHit);
        health.TakeHit();
    }

    void OnDisable()
    {
        routine = null;
    }
}
