using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;

namespace Content.Shared._OpenSpace.Genetics
{
    [Serializable, NetSerializable]
    public enum GeneticsConsoleUiKey : byte
    {
        Key
    }

    public static class SharedGeneticsConsole
    {
        public const string BeakerSlotId = "beaker_slot";
        public const string SyringeSlotId = "syringe_slot";
    }

    [Serializable, NetSerializable]
    public enum GeneticsBufferCopyMode : byte
    {
        UiOnly,
        UiAndSe,
        SeOnly
    }

    [Serializable, NetSerializable]
    public enum GeneticsBufferTransferTarget : byte
    {
        Subject,
        Syringe
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleTransferBufferState
    {
        public readonly string SubjectName;
        public readonly bool HasUi;
        public readonly bool HasSe;

        public GeneticsConsoleTransferBufferState(string subjectName, bool hasUi, bool hasSe)
        {
            SubjectName = subjectName;
            HasUi = hasUi;
            HasSe = hasSe;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string OccupantName;
        public readonly bool HasScanner;
        public readonly bool ScannerLocked;
        public readonly bool HasSubject;
        public readonly bool IsOccupantAlive;
        public readonly Dictionary<int, string> UiDna;
        public readonly Dictionary<int, string> SeDna;
        public readonly float CurrentHealth;
        public readonly float MaxHealth;
        public readonly int Instability;
        public readonly int RadiationExposure;
        public readonly int SelectedUiBlock; 
        public readonly int SelectedSeBlock; 
        public readonly bool HasBeaker;
        public readonly bool HasSyringe;
        public readonly string BeakerLabel;
        public readonly int BeakerCurrentVolume;
        public readonly int BeakerMaxVolume;
        public readonly List<GeneticsConsoleTransferBufferState> TransferBuffers;

        public GeneticsConsoleBoundUserInterfaceState(string occupantName, bool hasScanner, bool scannerLocked, bool hasSubject, bool isOccupantAlive, float currentHealth, float maxHealth, Dictionary<int, string> uiDna, Dictionary<int, string> seDna, int instability, int radiationExposure, int selectedUiBlock, int selectedSeBlock, bool hasBeaker = false, bool hasSyringe = false, string beakerLabel = "", int beakerCurrentVolume = 0, int beakerMaxVolume = 0, List<GeneticsConsoleTransferBufferState>? transferBuffers = null)
        {
            OccupantName = occupantName;
            HasScanner = hasScanner;
            ScannerLocked = scannerLocked;
            HasSubject = hasSubject;
            IsOccupantAlive = isOccupantAlive;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            UiDna = uiDna;
            SeDna = seDna;
            Instability = instability;
            RadiationExposure = radiationExposure;
            SelectedUiBlock = selectedUiBlock;
            SelectedSeBlock = selectedSeBlock;
            HasBeaker = hasBeaker;
            HasSyringe = hasSyringe;
            BeakerLabel = beakerLabel;
            BeakerCurrentVolume = beakerCurrentVolume;
            BeakerMaxVolume = beakerMaxVolume;
            TransferBuffers = transferBuffers ?? new List<GeneticsConsoleTransferBufferState>();
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleModifyDnaMessage : BoundUserInterfaceMessage
    {
        public readonly int BlockIndex;
        public readonly int SubIndex;

        public GeneticsConsoleModifyDnaMessage(int blockIndex, int subIndex)
        {
            BlockIndex = blockIndex;
            SubIndex = subIndex;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleSelectBlockMessage : BoundUserInterfaceMessage
    {
        public readonly int BlockIndex;
        public readonly bool IsUiBlock;

        public GeneticsConsoleSelectBlockMessage(int blockIndex, bool isUiBlock)
        {
            BlockIndex = blockIndex;
            IsUiBlock = isUiBlock;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleIrradiateBlockMessage : BoundUserInterfaceMessage
    {
        public readonly int BlockIndex;
        public readonly bool IsUiBlock;

        public GeneticsConsoleIrradiateBlockMessage(int blockIndex, bool isUiBlock)
        {
            BlockIndex = blockIndex;
            IsUiBlock = isUiBlock;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleInjectMessage : BoundUserInterfaceMessage
    {
        public readonly int Amount;

        public GeneticsConsoleInjectMessage(int amount)
        {
            Amount = amount;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleEjectBeakerMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleToggleScannerLockMessage : BoundUserInterfaceMessage
    {
        public readonly bool Locked;

        public GeneticsConsoleToggleScannerLockMessage(bool locked)
        {
            Locked = locked;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleStoreBufferMessage : BoundUserInterfaceMessage
    {
        public readonly int BufferIndex;
        public readonly GeneticsBufferCopyMode Mode;

        public GeneticsConsoleStoreBufferMessage(int bufferIndex, GeneticsBufferCopyMode mode)
        {
            BufferIndex = bufferIndex;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleTransferBufferMessage : BoundUserInterfaceMessage
    {
        public readonly int BufferIndex;
        public readonly GeneticsBufferTransferTarget Target;

        public GeneticsConsoleTransferBufferMessage(int bufferIndex, GeneticsBufferTransferTarget target)
        {
            BufferIndex = bufferIndex;
            Target = target;
        }
    }

    [Serializable, NetSerializable]
    public sealed class GeneticsConsoleModifyGeneMessage : BoundUserInterfaceMessage
    {
        public readonly int BlockIndex;
        public readonly int SubIndex; 
        public readonly string NewValue;
        public readonly bool IsUiBlock;

        public GeneticsConsoleModifyGeneMessage(int blockIndex, int subIndex, string newValue, bool isUiBlock)
        {
            BlockIndex = blockIndex;
            SubIndex = subIndex;
            NewValue = newValue;
            IsUiBlock = isUiBlock;
        }
    }
}

