using System;
using BrawlLib.SSBBTypes;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe class RWARNode : RSAREntryNode
    {
        internal RWAR* Header { get { return (RWAR*)WorkingUncompressed.Address; } }
        public override ResourceType ResourceType { get { return ResourceType.Unknown; } }

        protected override bool OnInitialize()
        {
            base.OnInitialize();

            return Header->Table->_entryCount > 0;
        }

        protected override void OnPopulate()
        {
            RWARTableBlock* table = Header->Table;
            RWARDataBlock* d = Header->Data;

            for (int i = 0; i < table->_entryCount; i++)
                new RWAVNode().Initialize(this, d->GetEntry(table->Entries[i].waveFileRef), 0);
        }

        protected override int OnCalculateSize(bool force)
        {
            int size = (RWAR.Size + 12 + Children.Count * 12).Align(0x20) + RWARDataBlock.Size;
            foreach (RWAVNode n in Children)
                size += n.WorkingUncompressed.Length;
            return size.Align(0x20);
        }

        protected internal override void OnRebuild(VoidPtr address, int length, bool force)
        {
            RWAR* header = (RWAR*)address;
            header->_header._version = 0x100;
            header->_header._tag = RWAR.Tag;
            header->_header.Endian = Endian.Big;
            header->_header._length = length;
            header->_tableOffset = 0x20;

            RWARTableBlock* tabl = (RWARTableBlock*)(address + 0x20);
            tabl->_header._tag = RWARTableBlock.Tag;
            tabl->_header._length = (0x20 + 12 + Children.Count * 12).Align(0x20);
            tabl->_entryCount = (uint)Children.Count;

            RWARDataBlock* data = (RWARDataBlock*)(address + 0x20 + tabl->_header._length);
            data->_header._tag = RWARDataBlock.Tag;

            VoidPtr addr = (VoidPtr)data + 0x20;
            foreach (RWAVNode n in Children)
            {
                tabl->Entries[n.Index].waveFileRef = (uint)(addr - (VoidPtr)data);
                Memory.Move(addr, n.WorkingUncompressed.Address, (uint)n.WorkingUncompressed.Length);
                addr += (tabl->Entries[n.Index].waveFileSize = (uint)n.WorkingUncompressed.Length);
            }
            data->_header._length = (int)(addr - (VoidPtr)data);
        }

        internal static ResourceNode TryParse(DataSource source) { return ((RWAR*)source.Address)->_header._tag == RWAR.Tag ? new RWARNode() : null; }
    }
}
