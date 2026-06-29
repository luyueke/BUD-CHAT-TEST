using UnityEngine;

public static class AudioClipExtensions {
    public static int Loudness(this AudioClip clip) {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        // 累加所有样本的平方
        float sum = 0f;
        foreach (var sample in samples) {
            sum += sample * sample;
        }

        // 计算平均值并取平方根
        float mean = sum / samples.Length;
        float rmsValue = Mathf.Sqrt(mean);
        return Mathf.RoundToInt(20 * Mathf.Log10(rmsValue / 0.1f) - 20);
    }
}
