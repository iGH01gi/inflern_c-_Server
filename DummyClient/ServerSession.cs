﻿using ServerCore;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DummyClient;

public abstract class Packet
{
    public ushort size;
    public ushort packetId;

    public abstract ArraySegment<byte> Write();
    public abstract void Read(ArraySegment<byte> s);
}

class PlayerInfoReq : Packet //플레이어 정보를 알고싶어서 서버로 보내는 패킷 (request)
{
    public long playerId;
    public string name;

    public PlayerInfoReq()
    {
        this.packetId = (ushort)PacketID.PlayerInfoReq;
    }

    public override ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);

        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);
        

        //[][][][][][][][][]
        //success와 and연산을해서 한번이라도 false가 떴으면 전체 결과가 false로 나옴
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count,s.Length-count),this.packetId);
        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count,s.Length-count), this.playerId);
        count += sizeof(long);
        

        //string
        ushort nameLen=(ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count+sizeof(ushort));
        success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
        count += sizeof(ushort);
        count += nameLen;
        
        
        success &= BitConverter.TryWriteBytes(s,count); //사이즈는 마지막에 계산한걸 맨 앞에다가
        
        if (success == false)
            return null;

        return SendBufferHelper.Close(count);
    }

    public override void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        
        count += sizeof(ushort);
        count += sizeof(ushort);
        this.playerId = BitConverter.ToInt64(s.Slice(count,s.Length-count));
        count += sizeof(long);
        
        //string
        ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
        count += sizeof(ushort);
        this.name=Encoding.Unicode.GetString(s.Slice(count, nameLen));
        
    }
}


public enum PacketID
{
    PlayerInfoReq = 1,
    PlayerInfoOk = 2,
}

class ServerSession : Session
{
    public override void OnConnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnConnected: {endPoint}");

        PlayerInfoReq packet = new PlayerInfoReq() { playerId = 1001, name="ABCD" };

        //보낸다
        ArraySegment<byte> s =packet.Write();
        if(s!=null)
            Send(s);
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        Console.WriteLine($"OnDisconnected: {endPoint}");
    }

    public override int OnRecv(ArraySegment<byte> buffer)
    {
        string recvData = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
        Console.WriteLine($"[From Server] {recvData}");
        return buffer.Count;
    }

    public override void OnSend(int numOfBytes)
    {
        Console.WriteLine($"Transferred bytes: {numOfBytes}");
    }
}