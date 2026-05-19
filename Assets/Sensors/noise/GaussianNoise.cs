using UnityEngine;

public class GaussianNoise : MonoBehaviour
{
    public float GenerateGaussianNoise(float mean, float stdDev)
    {
        float u1 = 1.0f - Random.value;
        float u2 = 1.0f - Random.value;

        float z = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + stdDev * z;
    }
}
