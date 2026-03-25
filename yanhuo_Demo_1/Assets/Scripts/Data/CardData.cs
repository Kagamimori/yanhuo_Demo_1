using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    [System.Serializable]
    public class CardData
    {
        public enum CardType { Sugar, Oil, Flour }
        public enum CardQuality { Real, Fake }

        public CardType type;
        public CardQuality quality;
        public string cardId; // 唯一标识，可用于区分同种卡

        public CardData(CardType type, CardQuality quality)
        {
            this.type = type;
            this.quality = quality;
            this.cardId = System.Guid.NewGuid().ToString();
        }
    }


