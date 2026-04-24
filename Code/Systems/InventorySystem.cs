using SS15.Code.Core;

namespace SS15.Code.Systems
{
    public static class InventorySystem
    {
        // ---------- существующие методы ----------
        public static bool TryPickupToActiveHand(GameState state, Entity item)
        {
            if (state.ActiveLeftHand && state.LeftHand == null)
            {
                state.LeftHand = item;
                return true;
            }
            if (!state.ActiveLeftHand && state.RightHand == null)
            {
                state.RightHand = item;
                return true;
            }
            if (state.Pocket1 == null)
            {
                state.Pocket1 = item;
                return true;
            }
            if (state.Pocket2 == null)
            {
                state.Pocket2 = item;
                return true;
            }
            return false;
        }

        public static void DropActiveHand(GameState state, int playerX, int playerY)
        {
            Entity? item = state.ActiveHandItem;
            if (item == null) return;

            if (state.ActiveLeftHand) state.LeftHand = null;
            else state.RightHand = null;

            item.SetComponent(new Position(playerX, playerY));
            state.WorldEntities.Add(item);
        }

        public static void SwitchHand(GameState state)
        {
            state.ActiveLeftHand = !state.ActiveLeftHand;
        }

        // ---------- новые методы ----------
        /// <summary> Поменять содержимое левой и правой руки местами </summary>
        public static void SwapHands(GameState state)
        {
            (state.LeftHand, state.RightHand) = (state.RightHand, state.LeftHand);
        }

        /// <summary> Переместить предмет из указанной руки в указанный карман (0 или 1).
        /// Если карман занят, предметы меняются местами. </summary>
        public static void MoveFromHandToPocket(GameState state, bool fromLeftHand, int pocketIndex)
        {
            Entity? handItem = fromLeftHand ? state.LeftHand : state.RightHand;
            if (handItem == null) return;

            Entity? pocketItem = pocketIndex == 0 ? state.Pocket1 : state.Pocket2;

            if (fromLeftHand) state.LeftHand = pocketItem;
            else state.RightHand = pocketItem;

            if (pocketIndex == 0) state.Pocket1 = handItem;
            else state.Pocket2 = handItem;
        }

        /// <summary> Взять предмет из кармана в активную руку.
        /// Если рука занята, не делает ничего (можно потом доработать). </summary>
        public static void MoveFromPocketToActiveHand(GameState state, int pocketIndex)
        {
            Entity? active = state.ActiveHandItem;
            if (active != null) return; // рука занята

            Entity? pocketItem = pocketIndex == 0 ? state.Pocket1 : state.Pocket2;
            if (pocketItem == null) return;

            if (state.ActiveLeftHand) state.LeftHand = pocketItem;
            else state.RightHand = pocketItem;

            if (pocketIndex == 0) state.Pocket1 = null;
            else state.Pocket2 = null;
        }

        /// <summary> Обменять предмет в активной руке с предметом в указанном кармане </summary>
        public static void SwapActiveHandWithPocket(GameState state, int pocketIndex)
        {
            Entity? active = state.ActiveHandItem;
            Entity? pocketItem = pocketIndex == 0 ? state.Pocket1 : state.Pocket2;

            if (state.ActiveLeftHand) state.LeftHand = pocketItem;
            else state.RightHand = pocketItem;

            if (pocketIndex == 0) state.Pocket1 = active;
            else state.Pocket2 = active;
        }
    }
}