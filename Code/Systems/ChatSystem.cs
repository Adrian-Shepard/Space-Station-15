using System.Collections.Generic;
using SS15.Code.Core;   // <-- добавили!

namespace SS15.Code.Systems
{
    public static class ChatSystem
    {
        private const int MaxMessages = 50;

        public static void AddMessage(GameState state, string sender, string text)
        {
            state.ChatMessages.Add((sender, text));
            if (state.ChatMessages.Count > MaxMessages)
                state.ChatMessages.RemoveAt(0);
        }

        public static void SendPlayerMessage(GameState state, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            AddMessage(state, "Astronaut", text);
        }
    }
}