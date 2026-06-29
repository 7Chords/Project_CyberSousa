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
        SELECTED,
        NOT_SELECTED,
    }

    public enum EConditionType
    {
        NONE,
        FAVOR_GREATER,
        FAVOR_LESS,
        DIALOGUE_SELECTED,
        DIALOGUE_NOT_SELECTED,
        CUSTOMER_DELIVERED_TO_FLOOR,
        CUSTOMER_NOT_DELIVERED_TO_FLOOR,
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
        COMBO_TARGET_FLOOR,
        FORBID_CUSTOMER_TAG_TARGET_FLOOR,
        REQUIRE_TRANSFER_TARGET_FLOOR,
    }

    public enum ECustomerType
    {
        SPECIAL,
        RANDOM,
    }

    public enum EGameEndingType
    {
        BAD,
        ENDING_1,
        ENDING_2,
    }
}
