using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    public static class SpacedRepetitionConstants
    {
        // New card scheduling
        public const int AGAIN_NEW_CARD_MINUTES = 1;
        public const int HARD_NEW_CARD_MINUTES = 10;
        public const int GOOD_NEW_CARD_DAYS = 1;
        public const int EASY_NEW_CARD_DAYS = 4;

        // Review card scheduling
        public const int AGAIN_REVIEW_MINUTES = 10;
        public const double HARD_INTERVAL_MULTIPLIER = 1.2;

        // Ease factors
        public const double DEFAULT_EASE_FACTOR = 2.5;
        public const double MIN_EASE_FACTOR = 1.3;
        public const double EASY_EASE_BONUS = 0.15;
        public const double HARD_EASE_PENALTY = 0.15;
        public const double AGAIN_EASE_PENALTY = 0.2;

        // Initial intervals for new cards
        public const int GOOD_INITIAL_INTERVAL = 1;
        public const int EASY_INITIAL_INTERVAL = 4;
        public const double EASY_INITIAL_EASE_FACTOR = 2.6;
    }
}
