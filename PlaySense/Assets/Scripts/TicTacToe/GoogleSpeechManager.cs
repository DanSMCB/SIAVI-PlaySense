using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class GoogleSpeechManager : MonoBehaviour
{
    bool isListening = false;

    public TicTacToe ticTacToe;

    [Header("UI")]
    public TMP_Text speechToTextOutput;
    

    public string apiKey = "";
    private string speechToTextUrl => $"https://speech.googleapis.com/v1/speech:recognize?key={apiKey}";

    private AudioClip recordedClip;
    private string microphoneDevice;
    private const int sampleRate = 16000;
    private const int maxRecordingSeconds = 2;

    [SerializeField] private UnityEngine.UI.Toggle voiceToggle;
    public GameObject[] boardNumbers;

    private void Start()
    {
        for(int i=0; i<boardNumbers.Length; i++) boardNumbers[i].SetActive(true);

        if (PlayerPrefs.GetInt("VoiceMode", 0) == 1)
        {
            voiceToggle.isOn = true;
            foreach (var num in boardNumbers)
            {
                num.SetActive(true);
            }
            speechToTextOutput.enabled = true;
            StartRecording();
        }
        else {
            foreach (var num in boardNumbers)
            {
                num.SetActive(false);
            }
        }
    }

    public void ResetBoardNumbers() {
        for (int i = 0; i < boardNumbers.Length; i++) boardNumbers[i].SetActive(true);
    }

    public void ToggleVoiceMode()
    {
        isListening = !isListening;
        PlayerPrefs.SetInt("VoiceMode", isListening ? 1 : 0);
        PlayerPrefs.Save();

        if (isListening)
        {
            foreach (var num in boardNumbers)
            {
                num.SetActive(true);
            }
            speechToTextOutput.enabled = true;
            StartRecording();
        }
        else
        {
            foreach (var num in boardNumbers)
            {
                num.SetActive(false);
            }
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
            if (ExecuteMove(number)) {
                boardNumbers[number-1].SetActive(false);
            }
            return;
        }

        // fallback: palavras → números
        switch (text)
        {
            case "um":
                if (ExecuteMove(1))
                    boardNumbers[0].SetActive(false);
                break;
            case "dois":
                if (ExecuteMove(2))
                    boardNumbers[1].SetActive(false);
                break;
            case "três":
            case "tres":
                if (ExecuteMove(3))
                    boardNumbers[2].SetActive(false);
                break;
            case "quatro":
                if (ExecuteMove(4))
                    boardNumbers[3].SetActive(false);
                break;
            case "cinco":
                if (ExecuteMove(5))
                    boardNumbers[4].SetActive(false);
                break;
            case "seis":
                if (ExecuteMove(6))
                    boardNumbers[5].SetActive(false);
                break;
            case "sete":
                if (ExecuteMove(7))
                    boardNumbers[6].SetActive(false);
                break;
            case "oito":
                if (ExecuteMove(8))
                    boardNumbers[7].SetActive(false);
                break;
            case "nove":
                if (ExecuteMove(9))
                    boardNumbers[8].SetActive(false);
                break;
            default:
                Debug.Log("Comando não reconhecido: " + text);
                break;
        }
    }

    bool ExecuteMove(int number)
    {
        int index = number - 1;

        if (index < 0 || index > 8)
        {
            Debug.Log("Número inválido");
            return false;
        }

        ticTacToe.Play(index);
        return true;
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