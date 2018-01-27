﻿using OpenNosCore.Core.Serializing;

namespace OpenNosCore.Packets
{
    [PacketHeader("c_map")]
    public class CMapPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0)]
        public byte Type { get; set; }

        [PacketIndex(1)]
        public short Id { get; set; }

        [PacketIndex(2)]
        public bool MapType { get; set; }

        #endregion
    }
}