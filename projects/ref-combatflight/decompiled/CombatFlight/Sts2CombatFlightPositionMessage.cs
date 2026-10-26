using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

namespace CombatFlight;

public struct Sts2CombatFlightPositionMessage : INetMessage, IPacketSerializable
{
	public RunLocation Location;

	public Vector2 Offset;

	public float RotationOffset;

	public uint Sequence;

	public bool IsFlying;

	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => (NetTransferMode)1;

	public LogLevel LogLevel => (LogLevel)0;

	public bool ShouldBuffer => true;

	public void Serialize(PacketWriter writer)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		writer.Write<RunLocation>(Location);
		writer.WriteVector2(Offset, (QuantizeParams?)null, (QuantizeParams?)null);
		writer.WriteFloat(RotationOffset, (QuantizeParams?)null);
		writer.WriteUInt(Sequence, 32);
		writer.WriteBool(IsFlying);
	}

	public void Deserialize(PacketReader reader)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		Location = reader.Read<RunLocation>();
		Offset = reader.ReadVector2((QuantizeParams?)null, (QuantizeParams?)null);
		RotationOffset = reader.ReadFloat((QuantizeParams?)null);
		Sequence = reader.ReadUInt(32);
		IsFlying = reader.ReadBool();
	}
}
