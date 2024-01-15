using System;
using ServerCore;
using System.Collections.Generic;
using ServerCore;

public class PacketManager
{
    #region Singleton
    private static PacketManager _instance=new PacketManager();
    public static PacketManager Instance { get { return _instance; } }
    #endregion

    PacketManager()
    {
        Register();
    }

    private Dictionary<ushort, Func<PacketSession, ArraySegment<byte>,IPacket>> _makeFunc =
        new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>,IPacket>>();

    Dictionary<ushort, Action<PacketSession, IPacket>> _handler =
        new Dictionary<ushort, Action<PacketSession, IPacket>>();
    
    
    public void Register()
    {
      _makeFunc.Add((ushort)PacketID.C_LeaveGame,MakePacket<C_LeaveGame>);
        _handler.Add((ushort)PacketID.C_LeaveGame,PacketHandler.C_LeaveGameHandler);
      _makeFunc.Add((ushort)PacketID.C_Move,MakePacket<C_Move>);
        _handler.Add((ushort)PacketID.C_Move,PacketHandler.C_MoveHandler);

    }
    
    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
    {
        ushort count = 0;

        ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
        count += 2;

        Func<PacketSession, ArraySegment<byte>,IPacket> func = null;
        if(_makeFunc.TryGetValue(id,out func))
        {
            IPacket packet=func.Invoke(session,buffer);
            
            if(onRecvCallback!=null)
                onRecvCallback.Invoke(session,packet); //유니티 클라쪽에서는 메인쓰레드가 처리해야하기 때문에 이 부분을 사용해서 패킷큐에 넣어주고 메인쓰레드에서 처리하도록 함
            else
                HandlePacket(session,packet);
        }
    }
    
    T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {
        T pkt = new T();
        pkt.Read(buffer);
        return pkt;
    }

    public void HandlePacket(PacketSession session, IPacket packet)
    {
        Action<PacketSession, IPacket> action = null;
        if(_handler.TryGetValue(packet.Protocol,out action))
            action.Invoke(session,packet);
    }
}