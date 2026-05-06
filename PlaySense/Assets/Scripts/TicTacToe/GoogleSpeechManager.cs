using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleSpeechManager : MonoBehaviour
{
    bool isListening = false;

    public TicTacToe ticTacToe;

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
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("Nenhum microfone encontrado.");
            return;
        }

        microphoneDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
        StartCoroutine(AutoStopRecording());

        Debug.Log("Gravação iniciada...");
    }

    IEnumerator AutoStopRecording()
    {
        yield return new WaitForSeconds(2f);
        StopRecordingAndTranscribe();
    }

    public void StopRecordingAndTranscribe()
    {
        if (string.IsNullOrWhiteSpace(microphoneDevice))
        {
            Debug.LogWarning("Nenhuma gravação ativa.");
            return;
        }

        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);

        if (position <= 0)
        {
            Debug.LogError("Não foi captado áudio.");
            return;
        }

        float[] samples = new float[position * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        AudioClip trimmedClip = AudioClip.Create(
            "trimmed_recording",
            position,
            recordedClip.channels,
            recordedClip.frequency,
            false
        );
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

        using UnityWebRequest request = new UnityWebRequest(speechToTextUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Erro no Speech-to-Text: " + request.error);
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }

        string responseJson = request.downloadHandler.text;
        Debug.Log("Resposta STT: " + responseJson);

        SpeechToTextResponse response = JsonUtility.FromJson<SpeechToTextResponse>(responseJson);

        if (response != null && response.results != null && response.results.Length > 0)
        {
            StringBuilder fullText = new StringBuilder();

            foreach (var result in response.results)
            {
                if (result.alternatives != null && result.alternatives.Length > 0)
                {
                    fullText.Append(result.alternatives[0].transcript);
                    fullText.Append(" ");
                }
            }

            string recognizedText = fullText.ToString().Trim().ToLower();
            speechToTextOutput.text = recognizedText;

            HandleVoiceCommand(recognizedText);

            if (isListening)
            {
                StartRecording();
            }
        }
        else
        {
            speechToTextOutput.text = "Not identified.";

            if (isListening)
            {
                StartRecording();
            }
        }
    }

    void HandleVoiceCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        if (int.TryParse(text, out int number))
        {
            ExecuteMove(number);
            return;
        }

        // fallback: palavras → números
        switch (text)
        {
            case "um": ExecuteMove(1); break;
            case "dois": ExecuteMove(2); break;
            case "três":
            case "tres": ExecuteMove(3); break;
            case "quatro": ExecuteMove(4); break;
            case "cinco": ExecuteMove(5); break;
            case "seis": ExecuteMove(6); break;
            case "sete": ExecuteMove(7); break;
            case "oito": ExecuteMove(8); break;
            case "nove": ExecuteMove(9); break;
            default:
                Debug.Log("Comando não reconhecido: " + text);
                break;
        }
    }

    void ExecuteMove(int number)
    {
        int index = number - 1;

        if (index < 0 || index > 8)
        {
            Debug.Log("Número inválido");
            return;
        }

        ticTacToe.Play(index);
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