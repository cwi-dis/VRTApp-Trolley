using System.IO;
using UnityEngine;

namespace VRT.Pilots.Trolley
{
    public static class WavUtility
    {
        /// <summary>
        /// Saves an AudioClip as a 16-bit PCM WAV file.
        /// sampleCount: number of samples to write (use Microphone.GetPosition() to trim silence).
        /// </summary>
        public static void Save(string path, AudioClip clip, int sampleCount)
        {
            sampleCount = Mathf.Clamp(sampleCount, 0, clip.samples);
            int channels = clip.channels;
            int sampleRate = clip.frequency;
            int dataSize = sampleCount * channels * 2;  // 16-bit = 2 bytes per sample

            float[] data = new float[sampleCount * channels];
            clip.GetData(data, 0);

            using var fs = new FileStream(path, FileMode.Create);
            using var bw = new BinaryWriter(fs);

            // RIFF header
            bw.Write(new[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + dataSize);
            bw.Write(new[] { 'W', 'A', 'V', 'E' });

            // fmt chunk
            bw.Write(new[] { 'f', 'm', 't', ' ' });
            bw.Write(16);
            bw.Write((short)1);                                        // PCM
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(sampleRate * channels * 2);                       // byte rate
            bw.Write((short)(channels * 2));                           // block align
            bw.Write((short)16);                                       // bits per sample

            // data chunk
            bw.Write(new[] { 'd', 'a', 't', 'a' });
            bw.Write(dataSize);
            foreach (float f in data)
                bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(f * 32767f), -32768, 32767));
        }
    }
}
