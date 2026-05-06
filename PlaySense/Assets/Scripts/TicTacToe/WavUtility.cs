using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();

        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int samples = clip.samples;

        float[] floatSamples = new float[samples * channels];
        clip.GetData(floatSamples, 0);

        short[] intData = new short[floatSamples.Length];
        byte[] bytesData = new byte[floatSamples.Length * 2];

        const float rescaleFactor = 32767f;

        for (int i = 0; i < floatSamples.Length; i++)
        {
            intData[i] = (short)(floatSamples[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        WriteWavHeader(stream, clip, bytesData.Length);
        stream.Write(bytesData, 0, bytesData.Length);

        return stream.ToArray();
    }

    private static void WriteWavHeader(Stream stream, AudioClip clip, int dataLength)
    {
        int channels = clip.channels;
        int sampleRate = clip.frequency;
        int byteRate = sampleRate * channels * 2;

        stream.Position = 0;

        stream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(36 + dataLength), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)1), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
        stream.Write(BitConverter.GetBytes((ushort)(channels * 2)), 0, 2);
        stream.Write(BitConverter.GetBytes((ushort)16), 0, 2);
        stream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(dataLength), 0, 4);
    }
}