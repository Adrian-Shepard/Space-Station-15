using System;

namespace SS15.Atmospherics;

public sealed class GasMixture
{
    public float[] Moles = new float[3];
    public float Temperature = 293.15f; // 20°C
    public float Volume = 2.5f;         // м³

    public float TotalMoles
    {
        get
        {
            float sum = 0;
            foreach (float m in Moles)
                sum += m;
            return sum;
        }
    }

    public float Pressure =>
        TotalMoles * 8.314f * Temperature / Volume;

    public void SetMoles(GasType gas, float moles)
    {
        Moles[(int)gas] = moles;
    }

    public float GetMoles(GasType gas) => Moles[(int)gas];

    public void Clear()
    {
        Array.Clear(Moles, 0, Moles.Length);
    }

    public GasMixture Clone()
    {
        var clone = new GasMixture();
        Array.Copy(Moles, clone.Moles, Moles.Length);
        clone.Temperature = Temperature;
        clone.Volume = Volume;
        return clone;
    }
}