using System;
using SS15.Code.__DEFINES;

namespace SS15.Code.Modules.Atmospherics
{
    public struct GasMixture
    {
        public float MolesO2;
        public float MolesN2;
        public float Temperature; // Кельвины

        public float TotalMoles => MolesO2 + MolesN2;

        // Давление в Па (P = nRT / V, V = 1 м³)
        public float Pressure => TotalMoles * ATMOS_DEFINES.R_IDEAL_GAS * Temperature;

        public float HeatCapacity => MolesO2 * AtmosConstants.SpecificHeatO2 + MolesN2 * AtmosConstants.SpecificHeatN2;

        // Заполнить воздухом для дыхания
        public void SetToBreathable()
        {
            MolesO2 = ATMOS_DEFINES.O2_STANDARD;
            MolesN2 = ATMOS_DEFINES.N2_STANDARD;
            Temperature = ATMOS_DEFINES.T20C;
        }

        // Цвет для отрисовки (чем больше кислорода – голубее, азота – зеленее)
        public (byte r, byte g, byte b) GetVisualColor()
        {
            float density = TotalMoles / ATMOS_DEFINES.MOLES_CELLSTANDARD;
            density = Math.Clamp(density, 0.2f, 3.0f);
            float o2Ratio = TotalMoles > 0 ? MolesO2 / TotalMoles : 0.21f;

            byte r = (byte)Math.Clamp(60 + o2Ratio * 30 * density, 0, 255);
            byte g = (byte)Math.Clamp(60 + (1 - o2Ratio) * 30 * density, 0, 255);
            byte b = (byte)Math.Clamp(60 + o2Ratio * 50 * density, 0, 255);
            return (r, g, b);
        }
    }
}