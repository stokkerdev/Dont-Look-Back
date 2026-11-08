using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Paciente del hospital. Va en una figura con Collider e Interactable; en el "On Interact"
// del Interactable se llama a PatientNPC.Talk. Un mismo script sirve para los dos tipos:
//  - Paciente real (isImpostor = false): dice la verdad y, al terminar, se apaga (se está muriendo).
//  - Impostor (isImpostor = true): el doppelgänger de la entidad. Da una pista falsa que lleva a la
//    PowerZone. Si el jugador lo alumbra con la linterna el tiempo suficiente, se delata y se va.
// Los subtítulos no bloquean al jugador: si se aleja más de hearDistance, el paciente espera.
public class PatientNPC : MonoBehaviour
{
    [Header("Diálogo")]
    public string displayName = "Paciente";
    [TextArea(2, 4)] public string[] lines;
    public float hearDistance = 9f;          // más lejos que esto no se oye y el paciente espera
    public string giveItem;                  // objeto que entrega al terminar (vacío = ninguno)
    public bool vanishWhenFinished = true;   // el paciente real se desvanece al terminar
    public float vanishDelay = 2f;
    public UnityEvent onFinished = new UnityEvent();

    [Header("Impostor (doppelgänger)")]
    public bool isImpostor;
    public Flashlight flashlight;            // si queda vacío se busca en la escena
    public GameObject tell;                  // señal visible al alumbrarlo (ojos rojos); empieza desactivada
    public float revealSeconds = 2.5f;       // segundos de luz seguidos para delatarlo
    public float revealRange = 10f;          // distancia máxima a la que la luz lo delata
    [Range(5, 40)] public float revealAngle = 15f;   // qué tan centrado debe estar en la mira
    public string revealedLine = "...Podías haberte quedado.";
    public UnityEvent onRevealed = new UnityEvent();

    public enum State { Idle, Speaking, Done, Gone }
    public State Current { get; private set; }
    public bool Revealed { get; private set; }

    float exposure;
    Transform player;
    Transform cam;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (Camera.main != null) cam = Camera.main.transform;
        if (isImpostor && flashlight == null) flashlight = FindFirstObjectByType<Flashlight>();
        if (tell != null) tell.SetActive(false);
    }

    // Se llama desde el "On Interact" del Interactable
    public void Talk()
    {
        if (Current != State.Idle || lines == null || lines.Length == 0) return;
        StartCoroutine(Speak());
    }

    // También lo usa la PowerZone para quitar al impostor de en medio
    public void Vanish()
    {
        if (Current == State.Gone) return;
        Current = State.Gone;
        StopAllCoroutines();
        Subtitles.Clear();
        gameObject.SetActive(false);
    }

    IEnumerator Speak()
    {
        Current = State.Speaking;
        foreach (string line in lines)
        {
            while (!PlayerNear()) yield return null;
            float time = LineTime(line);
            Subtitles.Show(displayName, line, time);
            yield return new WaitForSeconds(time + 0.4f);
        }

        if (!string.IsNullOrEmpty(giveItem)) Inventory.Add(giveItem);
        Current = State.Done;
        onFinished.Invoke();

        if (vanishWhenFinished)
        {
            yield return new WaitForSeconds(vanishDelay);
            Vanish();
        }
    }

    void Update()
    {
        if (Current == State.Gone) return;

        // Mira al jugador mientras habla; el impostor sigue mirándolo después, sin moverse
        bool faces = Current == State.Speaking || (isImpostor && Current == State.Done);
        if (faces && player != null)
        {
            Vector3 d = player.position - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 4f * Time.deltaTime);
        }

        if (isImpostor && !Revealed) UpdateExposure();
    }

    // Cuanto más tiempo lo alumbra la linterna, más se nota que algo anda mal
    void UpdateExposure()
    {
        exposure = IsLit() ? exposure + Time.deltaTime : Mathf.Max(0f, exposure - Time.deltaTime * 0.5f);
        if (tell != null) tell.SetActive(exposure > revealSeconds * 0.35f);
        if (exposure >= revealSeconds) Reveal();
    }

    bool IsLit()
    {
        if (flashlight == null || !flashlight.IsOn || cam == null) return false;

        Vector3 eye = transform.position + Vector3.up * 1.4f;
        Vector3 to = eye - cam.position;
        float dist = to.magnitude;
        if (dist > revealRange || Vector3.Angle(cam.forward, to) > revealAngle) return false;

        // Debe verse de verdad: sin paredes ni muebles de por medio
        if (Physics.Raycast(cam.position, to, out RaycastHit hit, dist + 0.5f, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(transform);
        return false;
    }

    void Reveal()
    {
        if (Revealed || Current == State.Gone) return;
        Revealed = true;
        StopAllCoroutines();
        if (tell != null) tell.SetActive(true);
        Subtitles.Show("???", revealedLine, 3f);
        onRevealed.Invoke();
        StartCoroutine(VanishAfter(1.6f));
    }

    IEnumerator VanishAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Vanish();
    }

    bool PlayerNear() => player == null || (player.position - transform.position).sqrMagnitude <= hearDistance * hearDistance;

    static float LineTime(string line) => Mathf.Clamp(1.5f + line.Length * 0.045f, 2f, 6f);
}
