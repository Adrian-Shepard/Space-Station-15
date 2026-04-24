namespace SS15.Code.__DEFINES
{
    public static class ATMOS_DEFINES
    {
        public const float T20C = 293.15f;       // 20°C в Кельвинах
        public const float T0C = 273.15f;

        // Количество молей на тайл, чтобы при 20°C получить 101.325 кПа в объёме 1 м³
        public const float MOLES_CELLSTANDARD = 41.57f;

        // Стандартные доли (21% O₂, 79% N₂)
        public const float O2_STANDARD = 0.21f * MOLES_CELLSTANDARD;   // ~8.73 моль
        public const float N2_STANDARD = 0.79f * MOLES_CELLSTANDARD;   // ~32.84 моль

        public const float R_IDEAL_GAS = 8.31446f;
    }
}