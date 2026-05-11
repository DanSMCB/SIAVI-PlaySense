using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MainMenuGoogleSpeechManager : MonoBehaviour
{
    bool isListening = false;

    [Header("UI")]
    public TMP_Text speechToTextOutput;

    [Header("Google Cloud")]
    [TextArea]
    public string bearerToken;
    public string speechToTextUrl = "https://speech.googleapis.com/v1/speech:recognize";

    [Header("VAD Settings")]
    public float silenceThreshold = 0.02f;
    public float silenceDuration = 0.8f;
    public float maxRecordingSeconds = 5f;

    private AudioClip recordedClip;
    private string microphoneDevice;
    private const int sampleRate = 44100;

    void Awake()
    {
        bool voiceEnabled = PlayerPrefs.GetInt("VoiceMode", 0) == 1;
        Debug.Log($"[Speech] VoiceMode ao iniciar: {voiceEnabled}");
        if (voiceEnabled) ToggleVoiceMode();
    }

    public void ToggleVoiceMode()
    {
        isListening = !isListening;

        if (isListening)
        {
            if (speechToTextOutput != null) speechToTextOutput.enabled = true;
            StartCoroutine(ListenContinuously());
            Debug.Log("[Speech] Voice mode ON");
        }
        else
        {
            Microphone.End(microphoneDevice);
            StopAllCoroutines();
            if (speechToTextOutput != null) speechToTextOutput.enabled = false;
            Debug.Log("[Speech] Voice mode OFF");
        }
    }

    IEnumerator ListenContinuously()
    {
        while (isListening)
        {
            yield return StartCoroutine(WaitForSpeech());
            if (!isListening) yield break;

            yield return StartCoroutine(RecordUntilSilence());

            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator WaitForSpeech()
    {
        if (speechToTextOutput != null) speechToTextOutput.text = "🎤 ...";

        if (Microphone.devices.Length == 0) yield break;

        microphoneDevice = Microphone.devices[0];

        // Grava já em loop com buffer grande — não perde o início
        recordedClip = Microphone.Start(microphoneDevice, true, (int)maxRecordingSeconds, sampleRate);

        while (isListening)
        {
            yield return new WaitForSeconds(0.1f);
            if (GetCurrentVolume() > silenceThreshold)
            {
                Debug.Log("[Speech] Voz detetada!");
                yield break;
            }
        }
    }

    IEnumerator RecordUntilSilence()
    {
        // NÃO paras o microfone — continuas a gravar no mesmo clip
        if (speechToTextOutput != null) speechToTextOutput.text = "🎤 A ouvir...";
        Debug.Log("[Speech] A gravar fala...");

        // Marca onde começou a fala (com 0.3s de buffer antes)
        int startPos = Mathf.Max(0, Microphone.GetPosition(microphoneDevice) - (int)(sampleRate * 0.3f));

        float silenceTimer = 0f;
        float recordingTimer = 0f;

        while (isListening)
        {
            yield return new WaitForSeconds(0.1f);
            recordingTimer += 0.1f;

            float vol = GetCurrentVolume();
            if (vol < silenceThreshold)
                silenceTimer += 0.1f;
            else
                silenceTimer = 0f;

            if (silenceTimer >= silenceDuration || recordingTimer >= maxRecordingSeconds)
            {
                Debug.Log($"[Speech] Fim de fala — silêncio: {silenceTimer:F1}s | total: {recordingTimer:F1}s");

                // Para o microfone e extrai só a parte com fala
                int endPos = Microphone.GetPosition(microphoneDevice);
                Microphone.End(microphoneDevice);

                ExtractAndSend(startPos, endPos);
                yield break;
            }
        }
    }

    void ExtractAndSend(int startPos, int endPos)
    {
        int clipLen = recordedClip.samples;
        int length = endPos > startPos
            ? endPos - startPos
            : (clipLen - startPos) + endPos; // wrap-around do buffer circular

        if (length <= 0) return;

        float[] samples = new float[length * recordedClip.channels];

        // Copia os samples respeitando o wrap-around
        if (endPos > startPos)
        {
            recordedClip.GetData(samples, startPos);
        }
        else
        {
            // Buffer fez wrap — copia em duas partes
            float[] part1 = new float[(clipLen - startPos) * recordedClip.channels];
            float[] part2 = new float[endPos * recordedClip.channels];
            recordedClip.GetData(part1, startPos);
            recordedClip.GetData(part2, 0);
            part1.CopyTo(samples, 0);
            part2.CopyTo(samples, part1.Length);
        }

        AudioClip trimmed = AudioClip.Create("trimmed", length, recordedClip.channels, sampleRate, false);
        trimmed.SetData(samples, 0);

        // DEBUG — guarda o WAV para ouvires
        byte[] debugWav = WavUtility.FromAudioClip(trimmed);
        string path = Application.persistentDataPath + "/debug_audio.wav";
        System.IO.File.WriteAllBytes(path, debugWav);
        Debug.Log($"[Speech] WAV guardado em: {path}");

        StartCoroutine(SendAudioToSpeechToText(trimmed));
    }

    float GetCurrentVolume()
    {
        if (recordedClip == null) return 0f;

        int pos = Microphone.GetPosition(microphoneDevice);
        if (pos <= 0) return 0f;

        int sampleCount = sampleRate / 10;
        int startPos = Mathf.Max(0, pos - sampleCount);
        int length = pos - startPos;
        if (length <= 0) return 0f;

        float[] samples = new float[length];
        recordedClip.GetData(samples, startPos);

        float max = 0f;
        foreach (var s in samples)
        {
            float abs = Mathf.Abs(s);
            if (abs > max) max = abs;
        }
        return max;
    }

    // ─────────────────────────────────────────────────────────────
    // TRANSCRIÇÃO
    // ─────────────────────────────────────────────────────────────

    public void StopRecordingAndTranscribe()
    {
        if (recordedClip == null) return;

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);

        if (position <= 0)
        {
            Debug.LogError("[Speech] Nenhum áudio captado!");
            return;
        }

        float[] samples = new float[position * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        // Amplificação
        float maxVolume = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > maxVolume) maxVolume = abs;
        }

        //if (maxVolume > 0 && maxVolume < 0.5f)
        //{
        //    float multiplier = 0.9f / maxVolume;
        //    for (int i = 0; i < samples.Length; i++) samples[i] *= multiplier;
        //    Debug.Log($"[Speech] Amplificado {multiplier:F1}x");
        //}

        AudioClip trimmedClip = AudioClip.Create("trimmed", position, recordedClip.channels, sampleRate, false);
        trimmedClip.SetData(samples, 0);

        // DEBUG — guarda o WAV para ouvires
        byte[] debugWav = WavUtility.FromAudioClip(trimmedClip);
        string path = Application.persistentDataPath + "/debug_audio.wav";
        System.IO.File.WriteAllBytes(path, debugWav);
        Debug.Log($"[Speech] WAV guardado em: {path}");

        StartCoroutine(SendAudioToSpeechToText(trimmedClip));
    }

    // ─────────────────────────────────────────────────────────────
    // ENVIO PARA API
    // ─────────────────────────────────────────────────────────────

    IEnumerator SendAudioToSpeechToText(AudioClip clip)
    {
        byte[] wavBytes = WavUtility.FromAudioClip(clip);
        string base64Audio = Convert.ToBase64String(wavBytes);

        SpeechToTextRequest requestBody = new SpeechToTextRequest
        {
            config = new RecognitionConfig
            {
                encoding = "LINEAR16",
                sampleRateHertz = sampleRate,
                languageCode = "pt-PT"
            },
            audio = new RecognitionAudio { content = base64Audio }
        };

        string json = JsonUtility.ToJson(requestBody);
        string finalUrl = $"{speechToTextUrl}?key={bearerToken}";

        using UnityWebRequest request = new UnityWebRequest(finalUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        Debug.Log($"[Speech] HTTP Status: {request.responseCode}");
        Debug.Log($"[Speech] Resposta completa: {request.downloadHandler.text}");

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Speech] Erro: " + request.error);
            yield break;
        }

        string responseJson = request.downloadHandler.text;
        Debug.Log("[Speech] Resposta: " + responseJson);

        SpeechToTextResponse response = JsonUtility.FromJson<SpeechToTextResponse>(responseJson);
        if (response?.results != null && response.results.Length > 0)
        {
            string recognizedText = response.results[0].alternatives[0].transcript.ToLower();
            if (speechToTextOutput != null) speechToTextOutput.text = recognizedText;
            Debug.Log($"[Speech] Reconhecido: {recognizedText}");
            HandleVoiceCommand(recognizedText);
        }
        else
        {
            if (speechToTextOutput != null) speechToTextOutput.text = "...";
        }
    }

    // ─────────────────────────────────────────────────────────────
    // INTERPRETAÇÃO — navegação do menu principal
    // ─────────────────────────────────────────────────────────────

    void HandleVoiceCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        string processedText = text.ToLower()
            .Replace("um", "1").Replace("dois", "2")
            .Replace("três", "3").Replace("tres", "3")
            .Replace("quatro", "4");

        int number = -1;
        foreach (char c in processedText)
        {
            if (char.IsDigit(c)) { number = (int)char.GetNumericValue(c); break; }
        }

        if (number == -1) return;

        Debug.Log($"[Speech] Comando: {number}");

        switch (number)
        {
            case 1: SceneManager.LoadScene("TicTacToe"); break;
            case 2: SceneManager.LoadScene("PingPong"); break;
            case 3: SceneManager.LoadScene("Reaction"); break;
            case 4: Application.Quit(); break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // CLASSES SERIALIZÁVEIS
    // ─────────────────────────────────────────────────────────────

    [Serializable] public class SpeechToTextRequest { public RecognitionConfig config; public RecognitionAudio audio; }
    [Serializable] public class RecognitionConfig { public string encoding; public int sampleRateHertz; public string languageCode; }
    [Serializable] public class RecognitionAudio { public string content; }
    [Serializable] public class SpeechToTextResponse { public SpeechRecognitionResult[] results; }
    [Serializable] public class SpeechRecognitionResult { public SpeechRecognitionAlternative[] alternatives; }
    [Serializable] public class SpeechRecognitionAlternative { public string transcript; public float confidence; }
}