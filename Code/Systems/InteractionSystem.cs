using SS15.Code.Core;
using System;
using System.Linq;

namespace SS15.Code.Systems
{
    public static class InteractionSystem
    {
        public static bool TryPickupAt(GameState state, int worldX, int worldY)
        {
            if (Math.Abs(worldX - state.PlayerX) > 1 || Math.Abs(worldY - state.PlayerY) > 1)
                return false;

            var entity = state.WorldEntities.FirstOrDefault(e =>
            {
                var p = e.GetComponent<Position>();
                return p.X == worldX && p.Y == worldY && e.HasComponent<Item>() && e.GetComponent<Item>().CanPickup;
            });

            if (entity != null)
            {
                state.WorldEntities.Remove(entity);
                return InventorySystem.TryPickupToActiveHand(state, entity);
            }
            return false;
        }

        public static void UseActiveItem(GameState state, int worldX, int worldY)
        {
            Entity? active = state.ActiveHandItem;
            if (active == null) return;

            var itemComp = active.GetComponent<Item>();
            if (!itemComp.CanUseOnTile) return;

            var tile = state.Map.Tiles[worldX, worldY];
            if (itemComp.Name == "Crowbar" && tile.IsWall)
            {
                state.Map.Tiles[worldX, worldY].IsWall = false;
                state.Map.Tiles[worldX, worldY].Air.SetToBreathable();
            }
            else if (itemComp.Name == "Fire Extinguisher")
            {
                ref var air = ref state.Map.Tiles[worldX, worldY].Air;
                air.MolesO2 = Math.Max(0, air.MolesO2 - 5f);
                air.MolesN2 = Math.Max(0, air.MolesN2 - 5f);
                air.Temperature = Math.Max(100f, air.Temperature - 200f);
            }
        }
    }
}