using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Actions;

public sealed partial class ShadowlingEngageHatchActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ShadowlingEngageHatchDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class ShadowlingEnthrallActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ShadowlingEnthrallDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public int Step = 1;
}

public sealed partial class ShadowlingBlackRecuperationActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ShadowlingBlackRecuperationDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class ShadowlingBlindnessSmokeActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingCollectiveMindActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class ShadowlingCollectiveMindDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed partial class ShadowlingGlareActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingVeilActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingIcyVeinsActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingShadowWalkActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingPlaneShiftActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingSonicScreechActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingAscendanceActionEvent : InstantActionEvent
{
}

public sealed partial class ShadowlingAnnihilateActionEvent : EntityTargetActionEvent
{
}
