using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class ReactionGoogleSpeechManager : MonoBehaviour
{
    bool isListening = false;

    [Header("UI")]
    public TMP_Text speechToTextOutput;

    [Header("Google Cloud")]
    [TextArea]
    public string bearerToken;

    public string speechToTextUrl = "https://speech.googleapis.com/v1/speech:recognize";

    private AudioClip recordedClip;
    private string microphoneDevice;
    private const int sampleRate = 16000;
    private const int maxRecordingSeconds = 2;

    // ─────────────────────────────────────────────────────────────
    // TOGGLE — ligar ao Toggle da UI
    // ─────────────────────────────────────────────────────────────

    public void ToggleVoiceMode()
    {
        isListening = !isListening;

        if (isListening)
        {
            if (speechToTextOutput != null) speechToTextOutput.enabled = true;
            StartRecording();
            Debug.Log("[Speech] Voice mode ON");
        }
        else
        {
            Microphone.End(null);
            StopAllCoroutines();
            if (speechToTextOutput != null) speechToTextOutput.enabled = false;
            Debug.Log("[Speech] Voice mode OFF");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // GRAVAÇÃO
    // ─────────────────────────────────────────────────────────────

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[Speech] Nenhum microfone encontrado.");
            return;
        }

        microphoneDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
        StartCoroutine(AutoStopRecording());
        Debug.Log("[Speech] A gravar...");
    }

    IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(maxRecordingSeconds);
        StopRecordingAndTranscribe();
    }

    public void StopRecordingAndTranscribe()
    {
        if (string.IsNullOrWhiteSpace(microphoneDevice)) return;

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);

        if (position <= 0)
        {
            Debug.LogError("[Speech] Não foi captado áudio.");
            return;
        }

        float[] samples = new float[position * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create("trimmed", position, recordedClip.channels, recordedClip.frequency, false);
        trimmedClip.SetData(samples, 0);

        StartCoroutine(SendAudioToSpeechToText(trimmedClip));
    }

    // ─────────────────────────────────────────────────────────────
    // ENVIO PARA GOOGLE API
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

        using UnityWebRequest request = new UnityWebRequest(speechToTextUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Speech] Erro: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            if (isListening) StartRecording();
            yield break;
        }

        string responseJson = request.downloadHandler.text;
        Debug.Log("[Speech] Resposta: " + responseJson);

        SpeechToTextResponse response = JsonUtility.FromJson<SpeechToTextResponse>(responseJson);

        if (response?.results != null && response.results.Length > 0)
        {
            var sb = new StringBuilder();
            foreach (var result in response.results)
                if (result.alternatives?.Length > 0)
                    sb.Append(result.alternatives[0].transcript).Append(" ");

            string recognized = sb.ToString().Trim().ToLower();

            if (speechToTextOutput != null)
                speechToTextOutput.text = recognized;

            HandleVoiceCommand(recognized);
        }
        else
        {
            if (speechToTextOutput != null)
                speechToTextOutput.text = "...";
        }

        // Loop contínuo enquanto o toggle estiver ativo
        if (isListening) StartRecording();
    }

    // ─────────────────────────────────────────────────────────────
    // INTERPRETAÇÃO — número 1 a 9
    // ─────────────────────────────────────────────────────────────

    void HandleVoiceCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // Tenta diretamente como número
        if (int.TryParse(text.Trim(), out int number))
        {
            ExecuteButton(number);
            return;
        }

        // Fallback: palavras pt-PT → números
        switch (text.Trim())
        {
            case "um": ExecuteButton(1); break;
            case "dois": ExecuteButton(2); break;
            case "três": case "tres": ExecuteButton(3); break;
            case "quatro": ExecuteButton(4); break;
            case "cinco": ExecuteButton(5); break;
            case "seis": ExecuteButton(6); break;
            case "sete": ExecuteButton(7); break;
            case "oito": ExecuteButton(8); break;
            case "nove": ExecuteButton(9); break;
            default:
                Debug.Log($"[Speech] Não reconhecido: \"{text}\"");
                break;
        }
    }

    void ExecuteButton(int number)
    {
        int index = number - 1; // 0-based

        int maxButtons = ReactionGameManager.Instance.CurrentLevel == 1
            ? ReactionGameManager.Instance.level1ButtonCount
            : ReactionGameManager.Instance.level2ButtonCount;

        if (index < 0 || index >= maxButtons)
        {
            Debug.LogWarning($"[Speech] Número {number} inválido para este nível (max: {maxButtons})");
            if (speechToTextOutput != null)
                speechToTextOutput.text = $"{number} inválido";
            return;
        }

        if (!ReactionGameManager.Instance.RoundActive) return;

        Debug.Log($"[Speech] Botão {number} → índice {index}");
        if (speechToTextOutput != null)
            speechToTextOutput.text = $"output {number}";

        bool wasColored = ReactionGameManager.Instance.ColoredIndicesThisRound.Contains(index);
        ReactionUIManager.Instance?.OnButtonTapped(index, wasColored);
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