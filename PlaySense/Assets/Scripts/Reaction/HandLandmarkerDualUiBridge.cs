using System;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using mptcc = Mediapipe.Tasks.Components.Containers;

public class HandLandmarkerDualUiBridge : MonoBehaviour
{
    [Header("Pointers")]
    [SerializeField] private HandUiPointerClicker _leftHandPointer;
    [SerializeField] private HandUiPointerClicker _rightHandPointer;

    [Header("Settings")]
    [SerializeField] private float _pinchDistanceThreshold = 0.05f;
    [Range(0f, 1f)][SerializeField] private float _smoothing = 0.2f;

    private bool _leftVisible;
    private bool _rightVisible;

    private readonly object _lock = new object();

    // Classe interna para guardar os dados
    private class HandData
    {
        public Vector3 indexTip;
        public Vector3 thumbTip;
        public Vector3 indexBase;
        public Vector3 pinkyBase;
        public Vector3 wrist;
        public bool hasSample;
    }

    private HandData _leftData = new HandData();
    private HandData _rightData = new HandData();

    private Vector2 _smoothedLeftPos;
    private Vector2 _smoothedRightPos;

    public void OnHandResult(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handedness == null)
            return;

        bool leftDetected = false;
        bool rightDetected = false;

        lock (_lock)
        {
            _leftData.hasSample = false;
            _rightData.hasSample = false;

            for (int i = 0; i < result.handLandmarks.Count; i++)
            {
                var lms = result.handLandmarks[i].landmarks;
                if (lms == null || lms.Count < 21) continue;

                string label = result.handedness[i].categories[0].categoryName.ToLower();
                bool isLeft = label.Contains("left");

                HandData target = isLeft ? _leftData : _rightData;

                var wrist = lms[0];
                var indexBase = lms[5];
                var pinkyBase = lms[17];

                target.indexTip = new Vector3(lms[8].x, lms[8].y, lms[8].z);
                target.thumbTip = new Vector3(lms[4].x, lms[4].y, lms[4].z);

                target.wrist = new Vector3(wrist.x, wrist.y, wrist.z);
                target.indexBase = new Vector3(indexBase.x, indexBase.y, indexBase.z);
                target.pinkyBase = new Vector3(pinkyBase.x, pinkyBase.y, pinkyBase.z);

                target.hasSample = true;

                if (isLeft) leftDetected = true;
                else rightDetected = true;
            }
        }

        _leftVisible = leftDetected;
        _rightVisible = rightDetected;
    }

    private void Update()
    {
        if (_leftHandPointer != null)
            _leftHandPointer.gameObject.SetActive(_leftVisible);

        if (_rightHandPointer != null)
            _rightHandPointer.gameObject.SetActive(_rightVisible);

        ProcessHand(_leftData, _leftHandPointer, ref _smoothedLeftPos);
        ProcessHand(_rightData, _rightHandPointer, ref _smoothedRightPos);
    }

    private void ProcessHand(HandData data, HandUiPointerClicker pointer, ref Vector2 smoothedPos)
    {
        // Se não houver amostra (mão não detectada), não fazemos nada
        if (pointer == null || !data.hasSample) return;

        Vector3 iTip, tTip, iBase, pBase, wrist;
        lock (_lock)
        {
            iTip = data.indexTip;
            tTip = data.thumbTip;
            iBase = data.indexBase;
            pBase = data.pinkyBase;
            wrist = data.wrist;
        }

        // Converter para coordenadas de ecrã (Inverter Y porque MediaPipe 0 é topo)
        float centerX =data.wrist.x * 0.5f + data.indexBase.x * 0.25f + data.pinkyBase.x * 0.25f;
        float centerY = data.wrist.y * 0.5f + data.indexBase.y * 0.25f + data.pinkyBase.y * 0.25f;
        Vector2 screenPos = new Vector2(centerX * Screen.width, (1f - centerY) * Screen.height);

        // Smoothing (Suavização)
        if (_smoothing > 0f)
        {
            float t = 1f - Mathf.Pow(1f - _smoothing, Time.deltaTime * 60f);
            smoothedPos = Vector2.Lerp(smoothedPos, screenPos, t);
        }
        else
        {
            smoothedPos = screenPos;
        }

        bool isPinching = Vector3.Distance(iTip, tTip) < _pinchDistanceThreshold;

        // Envia os dados para o cursor visual
        pointer.UpdatePointer(smoothedPos, isPinching);
    }
}