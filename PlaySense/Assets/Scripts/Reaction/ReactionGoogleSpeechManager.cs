using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
    private const int sampleRate = 44100;//16000;
    private const int maxRecordingSeconds = 2;

    public void ToggleVoiceMode()
    {
        isListening = !isListening;

        if (isListening)
        {
            speechToTextOutput.enabled = true;
            StartRecording();
        }
        else
        {
            Microphone.End(null);
            StopAllCoroutines();
            Debug.Log("Voice mode desligado");
            speechToTextOutput.enabled = false;
        }
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0) return;

        if (recordedClip != null)
        {
            Destroy(recordedClip);
        }

        microphoneDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(microphoneDevice, false, 10, sampleRate);
        StartCoroutine(WaitAndStop());
    }

    IEnumerator WaitAndStop()
    {
        // Esperamos os 2 segundos de fala
        yield return new WaitForSeconds(maxRecordingSeconds);

        // Damos um pequeno fôlego de 0.1s para o buffer processar
        yield return new WaitForEndOfFrame();

        StopRecordingAndTranscribe();
    }

    IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(2f);
        StopRecordingAndTranscribe();
    }

    public void StopRecordingAndTranscribe()
    {
        if (recordedClip == null) return;

        int position = Microphone.GetPosition(microphoneDevice);

        // LOG DE DEBUG IMPORTANTE
        Debug.Log($"Posição final do buffer: {position}");

        Microphone.End(microphoneDevice);

        if (position <= 0)
        {
            Debug.LogError("O microfone não captou nada. Tentando reiniciar...");
            if (isListening) Invoke("StartRecording", 0.5f);
            return;
        }

        // Criar os samples com base na posição real capturada
        float[] samples = new float[position * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        // temp
        float maxVolume = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float absVal = Mathf.Abs(samples[i]);
            if (absVal > maxVolume) maxVolume = absVal;
        }

        // Se o som for muito baixo, vamos amplificá-lo manualmente
        if (maxVolume > 0 && maxVolume < 0.5f)
        {
            float multiplier = 0.7f / maxVolume; // Alvo de 70% do volume máximo
            for (int i = 0; i < samples.Length; i++) samples[i] *= multiplier;
            Debug.Log($"Áudio amplificado em {multiplier}x");
        }


        AudioClip trimmedClip = AudioClip.Create("trimmed", position, recordedClip.channels, sampleRate, false);
        trimmedClip.SetData(samples, 0);

        StartCoroutine(SendAudioToSpeechToText(trimmedClip));
    }

    private IEnumerator SendAudioToSpeechToText(AudioClip clip)
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
            audio = new RecognitionAudio
            {
                content = base64Audio
            }
        };

        string json = JsonUtility.ToJson(requestBody);

        string finalUrl = $"{speechToTextUrl}?key={bearerToken}";

        using UnityWebRequest request = new UnityWebRequest(finalUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (isListening)
        {
            StartRecording();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Erro no Speech-to-Text: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        // ... resto do código igual (processamento da resposta)
        string responseJson = request.downloadHandler.text;
        Debug.Log("Resposta STT: " + responseJson);

        // (Continua com o JsonUtility.FromJson...)
        SpeechToTextResponse response = JsonUtility.FromJson<SpeechToTextResponse>(responseJson);
        if (response != null && response.results != null && response.results.Length > 0)
        {
            string recognizedText = response.results[0].alternatives[0].transcript.ToLower();
            speechToTextOutput.text = recognizedText;
            HandleVoiceCommand(recognizedText); // Ativa os botões!
        }
    }

    void HandleVoiceCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // 1. Limpeza e Conversão de texto para número
        string processedText = text.ToLower()
            .Replace("um", "1").Replace("dois", "2").Replace("três", "3").Replace("tres", "3")
            .Replace("quatro", "4").Replace("cinco", "5").Replace("seis", "6")
            .Replace("sete", "7").Replace("oito", "8").Replace("nove", "9");

        int number = -1;
        foreach (char c in processedText)
        {
            if (char.IsDigit(c))
            {
                number = (int)char.GetNumericValue(c);
                break;
            }
        }

        if (number == -1) return;

        // 2. Lógica de Contexto baseada no ReactionUIManager
        var ui = ReactionUIManager.Instance;
        var gm = ReactionGameManager.Instance;

        // CONTEXTO: MENU PRINCIPAL
        if (ui.mainMenuScreen.activeSelf)
        {
            switch (number)
            {
                case 1: ui.btnLevel1.onClick.Invoke(); break;
                case 2: ui.btnLevel2.onClick.Invoke(); break;
                case 3: ui.btnQuit.onClick.Invoke(); break;
            }
        }
        // CONTEXTO: ECRÃ DE ESTATÍSTICAS (FIM DE JOGO)
        else if (ui.statsScreen.activeSelf)
        {
            switch (number)
            {
                case 1: ui.btnPlayAgain.onClick.Invoke(); break;
                case 2:
                    if (gm.HasNextLevel()) ui.btnNextLevel.onClick.Invoke();
                    else ui.btnMainMenu.onClick.Invoke(); // Se não houver próximo, volta ao menu
                    break;
                case 3: ui.btnMainMenu.onClick.Invoke(); break;
            }
        }
        // CONTEXTO: DENTRO DO JOGO (Nível 1 ou 2)
        else if (ui.level1Screen.activeSelf || ui.level2Screen.activeSelf)
        {
            // Se o round estiver a decorrer, o número clica no botão
            if (gm.RoundActive)
            {
                ExecuteButton(number);
            }
        }
    }

    void ExecuteButton(int number)
    {
        int index = number - 1; // "1" vira índice 0
        var gm = ReactionGameManager.Instance;

        int maxButtons = gm.CurrentLevel == 1 ? gm.level1ButtonCount : gm.level2ButtonCount;

        if (index < 0 || index >= maxButtons) return;
        if (!gm.RoundActive) return;

        // Verifica se este índice era um dos coloridos este round
        bool wasColored = gm.ColoredIndicesThisRound.Contains(index);

        // Chama a animação e o registo do hit no UI Manager
        ReactionUIManager.Instance.OnButtonTapped(index, wasColored);
    }

    [Serializable]
    public class SpeechToTextRequest
    {
        public RecognitionConfig config;
        public RecognitionAudio audio;
    }

    [Serializable]
    public class RecognitionConfig
    {
        public string encoding;
        public int sampleRateHertz;
        public string languageCode;
    }

    [Serializable]
    public class RecognitionAudio
    {
        public string content;
    }

    [Serializable]
    public class SpeechToTextResponse
    {
        public SpeechRecognitionResult[] results;
    }

    [Serializable]
    public class SpeechRecognitionResult
    {
        public SpeechRecognitionAlternative[] alternatives;
    }

    [Serializable]
    public class SpeechRecognitionAlternative
    {
        public string transcript;
        public float confidence;
    }
}