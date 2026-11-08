using UnityEngine;

// Subtítulos en pantalla (abajo, centrados). Se usan con Subtitles.Show("Nombre", "texto", segundos).
// Se crea solo la primera vez que se necesita; no hay que ponerlo en la escena.
// A diferencia de DocumentReader no abre ningún panel: el jugador sigue moviéndose mientras lee.
public class Subtitles : MonoBehaviour
{
    static Subtitles instance;

    string speaker, line;
    float timer;

    public static void Show(string speaker, string line, float seconds)
    {
        if (instance == null) instance = new GameObject("Subtitles").AddComponent<Subtitles>();
        instance.speaker = speaker;
        instance.line = line;
        instance.timer = seconds;
    }

    public static void Clear()
    {
        if (instance != null) instance.timer = 0f;
    }

    void Update() => timer -= Time.deltaTime;

    void OnGUI()
    {
        if (timer <= 0f) return;

        float alpha = Mathf.Clamp01(timer / 0.4f);   // se desvanece al final
        string text = speaker + ": " + line;
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true, alignment = TextAnchor.MiddleCenter };
        Rect r = new Rect(Screen.width * 0.2f, Screen.height * 0.8f, Screen.width * 0.6f, 90);

        style.normal.textColor = new Color(0f, 0f, 0f, alpha);   // sombra para que se lea sobre fondos claros
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = new Color(0.92f, 0.92f, 0.85f, alpha);
        GUI.Label(r, text, style);
    }
}
