using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion


	public Action<PacketSession, IMessage, ushort> _unityHandler;

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();
	Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new Dictionary<ushort, Action<PacketSession, IMessage>>();
		
	public void Register()
	{		
		_onRecv.Add((ushort)MsgId.CMove, MakePacket<CMove>);
		_handler.Add((ushort)MsgId.CMove, PacketHandler.CMoveHandler);		
		_onRecv.Add((ushort)MsgId.CSkill, MakePacket<CSkill>);
		_handler.Add((ushort)MsgId.CSkill, PacketHandler.CSkillHandler);		
		_onRecv.Add((ushort)MsgId.CLogin, MakePacket<CLogin>);
		_handler.Add((ushort)MsgId.CLogin, PacketHandler.CLoginHandler);		
		_onRecv.Add((ushort)MsgId.CCreatePlayer, MakePacket<CCreatePlayer>);
		_handler.Add((ushort)MsgId.CCreatePlayer, PacketHandler.CCreatePlayerHandler);		
		_onRecv.Add((ushort)MsgId.CEnterGame, MakePacket<CEnterGame>);
		_handler.Add((ushort)MsgId.CEnterGame, PacketHandler.CEnterGameHandler);		
		_onRecv.Add((ushort)MsgId.CEquipItem, MakePacket<CEquipItem>);
		_handler.Add((ushort)MsgId.CEquipItem, PacketHandler.CEquipItemHandler);		
		_onRecv.Add((ushort)MsgId.CPong, MakePacket<CPong>);
		_handler.Add((ushort)MsgId.CPong, PacketHandler.CPongHandler);
	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		Action<PacketSession, ArraySegment<byte>, ushort> action = null;
		if (_onRecv.TryGetValue(id, out action))
			action.Invoke(session, buffer, id);
	}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		
        if (_unityHandler != null)
		{
            _unityHandler.Invoke(session, pkt, id);
        }
		else
		{
            Action<PacketSession, IMessage> action = null;
            if (_handler.TryGetValue(id, out action))
                action.Invoke(session, pkt);
        }
        
	}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{
		Action<PacketSession, IMessage> action = null;
		if (_handler.TryGetValue(id, out action))
			return action;
		return null;
	}
}