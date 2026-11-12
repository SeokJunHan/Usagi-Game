namespace UsagiGame
{
    public static class GameRuleLogic
    {
        public static long PairKey(int firstId, int secondId)
        {
            int min = firstId < secondId ? firstId : secondId;
            int max = firstId < secondId ? secondId : firstId;
            return ((long)min << 32) | (uint)max;
        }

        public static bool CanMerge(int firstStage, int secondStage, int firstBornStep, int secondBornStep, int physicsStep) =>
            firstStage == secondStage && firstBornStep < physicsStep && secondBornStep < physicsStep;

        public static float UpdateOverflowTime(bool aboveDangerLine, float elapsed, float deltaTime) =>
            aboveDangerLine ? elapsed + deltaTime : 0f;
    }
}
