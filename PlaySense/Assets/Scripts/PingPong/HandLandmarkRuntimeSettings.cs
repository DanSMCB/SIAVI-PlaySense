namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public static class HandLandmarkRuntimeSettings
    {
        public static int NumHands = 1;
        public static float MinHandDetectionConfidence = 0.5f;
        public static float MinHandPresenceConfidence = 0.5f;
        public static float MinTrackingConfidence = 0.5f;

        public static Mediapipe.Tasks.Core.BaseOptions.Delegate Delegate =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU;
#else
            Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU;
#endif

        public static ImageReadMode ImageReadMode = ImageReadMode.CPUAsync;

        public static Mediapipe.Tasks.Vision.Core.RunningMode RunningMode =
            Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM;
    }
}