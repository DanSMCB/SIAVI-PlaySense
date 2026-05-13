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

    // Track state so we only try to end if recording started
    private bool microphoneActive = false;
    private Coroutine listeningCoroutine;

    void OnEnable()
    {
        bool voiceEnabled = PlayerPrefs.GetInt("VoiceMode", 0) == 1;
        Debug.Log($"[Speech] OnEnable — VoiceMode: {voiceEnabled}");

        if (voiceEnabled && !isListening)
            ToggleVoiceMode();
    }

    void OnDisable()
    {
        if (isListening)
        {
            isListening = false;
            SafeStopMicrophone();
            recordedClip = null;
            if (listeningCoroutine != null)
            {
                StopCoroutine(listeningCoroutine);
                listeningCoroutine = null;
            }
        }
    }

    public void ToggleVoiceMode()
    {
        isListening = !isListening;

        if (isListening)
        {
            if (speechToTextOutput != null) speechToTextOutput.enabled = true;

            // Garante que o microfone está limpo antes de começar
            SafeStopMicrophone();
            recordedClip = null;

            if (listeningCoroutine != null)
            {
                StopCoroutine(listeningCoroutine);
                listeningCoroutine = null;
            }

            listeningCoroutine = StartCoroutine(ListenContinuously());
            Debug.Log("[Speech] Voice mode ON");
        }
        else
        {
            SafeStopMicrophone();
            recordedClip = null;

            if (listeningCoroutine != null)
            {
                StopCoroutine(listeningCoroutine);
                listeningCoroutine = null;
            }

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
        listeningCoroutine = null;
    }

    IEnumerator WaitForSpeech()
    {
        if (speechToTextOutput != null) speechToTextOutput.text = "🎤 ...";

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Speech] Nenhum microfone encontrado.");
            yield break;
        }

        microphoneDevice = Microphone.devices[0];

        // Grava já em loop com buffer grande — não perde o início
        recordedClip = Microphone.Start(microphoneDevice, true, (int)maxRecordingSeconds, sampleRate);
        microphoneActive = true;

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
        // Clip must be valid and mic must be running
        if (Microphone.devices.Length == 0 || string.IsNullOrEmpty(microphoneDevice) || recordedClip == null)
        {
            Debug.LogError("[Speech] Microfone indisponível na gravação.");
            yield break;
        }
        if (speechToTextOutput != null) speechToTextOutput.text = "🎤 A ouvir...";
        Debug.Log("[Speech] A gravar fala...");

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

                int endPos = Microphone.GetPosition(microphoneDevice);
                SafeStopMicrophone();

                ExtractAndSend(startPos, endPos);
                yield break;
            }
        }
    }

    // New: Only end microphone if we have started it safely!
    void SafeStopMicrophone()
    {
        if (microphoneActive && Microphone.devices.Length > 0)
        {
            try
            {
                Microphone.End(microphoneDevice);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Speech] Failed to end microphone: " + ex.Message);
            }
        }
        microphoneActive = false;
    }

    void ExtractAndSend(int startPos, int endPos)
    {
        if (recordedClip == null) return;

        int clipLen = recordedClip.samples;
        int length = endPos > startPos
            ? endPos - startPos
            : (clipLen - startPos) + endPos;

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
        try
        {
            byte[] debugWav = WavUtility.FromAudioClip(trimmed);
            string path = Application.persistentDataPath + "/debug_audio.wav";
            System.IO.File.WriteAllBytes(path, debugWav);
            Debug.Log($"[Speech] WAV guardado em: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Speech] Fail to write debug wav: " + ex.Message);
        }

        StartCoroutine(SendAudioToSpeechToText(trimmed));
    }

    float GetCurrentVolume()
    {
        if (recordedClip == null || string.IsNullOrEmpty(microphoneDevice)) return 0f;

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

    public void StopRecordingAndTranscribe()
    {
        if (recordedClip == null || string.IsNullOrEmpty(microphoneDevice)) return;

        int position = Microphone.GetPosition(microphoneDevice);
        SafeStopMicrophone();

        if (position <= 0)
        {
            Debug.LogError("[Speech] Nenhum áudio captado!");
            return;
        }

        float[] samples = new float[position * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create("trimmed", position, recordedClip.channels, sampleRate, false);
        trimmedClip.SetData(samples, 0);

        // DEBUG — guarda o WAV para ouvires
        try
        {
            byte[] debugWav = WavUtility.FromAudioClip(trimmedClip);
            string path = Application.persistentDataPath + "/debug_audio.wav";
            System.IO.File.WriteAllBytes(path, debugWav);
            Debug.Log($"[Speech] WAV guardado em: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Speech] Fail to write debug wav: " + ex.Message);
        }

        StartCoroutine(SendAudioToSpeechToText(trimmedClip));
    }

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

    // Clean up microphone and coroutines if object is destroyed (scene change)
    void OnDestroy()
    {
        SafeStopMicrophone();
        if (listeningCoroutine != null)
        {
            StopCoroutine(listeningCoroutine);
            listeningCoroutine = null;
        }
    }

    [Serializable] public class SpeechToTextRequest { public RecognitionConfig config; public RecognitionAudio audio; }
    [Serializable] public class RecognitionConfig { public string encoding; public int sampleRateHertz; public string languageCode; }
    [Serializable] public class RecognitionAudio { public string content; }
    [Serializable] public class SpeechToTextResponse { public SpeechRecognitionResult[] results; }
    [Serializable] public class SpeechRecognitionResult { public SpeechRecognitionAlternative[] alternatives; }
    [Serializable] public class SpeechRecognitionAlternative { public string transcript; public float confidence; }
}