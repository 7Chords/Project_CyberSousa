namespace GameCore
{

    public enum EDialogueType
    {
        STANDARD,
        SELECT,
    }

    public enum EDialogueFlagType
    {
        NONE,
        BEGIN,
        END,
    }
    public enum ECompareOperator
    {
        GREATER,
        LESS,
    }
    public enum EElevatorOperator
    {
        GOTO,
        REFUSE,
        DEFAULT,
    }

    public enum ERuleEffectType
    {
        FORBID_TARGET_FLOOR,
        REDIRECT_TARGET_FLOOR,
        FORBID_REFUSE,
    }
}
