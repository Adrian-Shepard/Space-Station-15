using SS15.Code.Core;
using System;

namespace SS15.Code.Modules.Atmospherics
{
    public class LINDA
    {
        private Map map;
        // Коэффициент обмена: какая доля разницы выравнивается за тик
       private const float EqualizationRate = 0.5f;      // было 0.15
        private const float HeatEqualizationRate = 0.5f;   // было 0.1

        public LINDA(Map map) => this.map = map;

        public void Process()
        {
            int w = map.Width, h = map.Height;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (map.Tiles[x, y].IsWall) continue;

                    // Обмен только с правым и нижним соседом (каждое ребро один раз)
                    Exchange(x, y, x + 1, y);
                    Exchange(x, y, x, y + 1);
                }
            }
        }

        private void Exchange(int x1, int y1, int x2, int y2)
        {
            if (x2 < 0 || x2 >= map.Width || y2 < 0 || y2 >= map.Height) return;
            if (map.Tiles[x2, y2].IsWall) return;

            GasMixture air1 = map.Tiles[x1, y1].Air;
            GasMixture air2 = map.Tiles[x2, y2].Air;

            float total1 = air1.TotalMoles;
            float total2 = air2.TotalMoles;

            // Выравнивание общего количества молей (диффузия)
            float avgTotal = (total1 + total2) * 0.5f;
            float newTotal1 = total1 + (avgTotal - total1) * EqualizationRate;
            float newTotal2 = total2 + (avgTotal - total2) * EqualizationRate;

            // Переносим доли газов пропорционально исходным составам
            if (total1 > 0 && total2 > 0)
            {
                // Смесь остаётся с теми же пропорциями O2/N2
                float ratioO2_1 = air1.MolesO2 / total1;
                float ratioN2_1 = air1.MolesN2 / total1;
                float ratioO2_2 = air2.MolesO2 / total2;
                float ratioN2_2 = air2.MolesN2 / total2;

                map.Tiles[x1, y1].Air.MolesO2 = newTotal1 * ratioO2_1;
                map.Tiles[x1, y1].Air.MolesN2 = newTotal1 * ratioN2_1;
                map.Tiles[x2, y2].Air.MolesO2 = newTotal2 * ratioO2_2;
                map.Tiles[x2, y2].Air.MolesN2 = newTotal2 * ratioN2_2;
            }
            else
            {
                // Одна из клеток почти пуста — просто перераспределяем
                map.Tiles[x1, y1].Air.MolesO2 = newTotal1 * (total1 > 0 ? air1.MolesO2 / total1 : 0.21f);
                map.Tiles[x1, y1].Air.MolesN2 = newTotal1 * (total1 > 0 ? air1.MolesN2 / total1 : 0.79f);
                map.Tiles[x2, y2].Air.MolesO2 = newTotal2 * (total2 > 0 ? air2.MolesO2 / total2 : 0.21f);
                map.Tiles[x2, y2].Air.MolesN2 = newTotal2 * (total2 > 0 ? air2.MolesN2 / total2 : 0.79f);
            }

            // Выравнивание температуры (теплообмен)
            float avgTemp = (air1.Temperature + air2.Temperature) * 0.5f;
            map.Tiles[x1, y1].Air.Temperature += (avgTemp - air1.Temperature) * HeatEqualizationRate;
            map.Tiles[x2, y2].Air.Temperature += (avgTemp - air2.Temperature) * HeatEqualizationRate;
        }
    }
}