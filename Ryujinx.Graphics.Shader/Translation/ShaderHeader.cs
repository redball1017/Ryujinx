using Ryujinx.Graphics.Shader.Decoders;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Shader.Translation
{
    enum PixelImap
    {
        Unused = 0,
        Constant = 1,
        Perspective = 2,
        ScreenLinear = 3
    }

    struct ImapPixelType
    {
        public PixelImap X { get; }
        public PixelImap Y { get; }
        public PixelImap Z { get; }
        public PixelImap W { get; }

        public ImapPixelType(PixelImap x, PixelImap y, PixelImap z, PixelImap w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public PixelImap GetFirstUsedType()
        {
            if (X != PixelImap.Unused) return X;
            if (Y != PixelImap.Unused) return Y;
            if (Z != PixelImap.Unused) return Z;
            return W;
        }
    }

    struct OmapTarget
    {
        public bool Red   { get; }
        public bool Green { get; }
        public bool Blue  { get; }
        public bool Alpha { get; }

        public bool Enabled => Red || Green || Blue || Alpha;

        public OmapTarget(bool red, bool green, bool blue, bool alpha)
        {
            Red   = red;
            Green = green;
            Blue  = blue;
            Alpha = alpha;
        }

        public bool ComponentEnabled(int component)
        {
            switch (component)
            {
                case 0: return Red;
                case 1: return Green;
                case 2: return Blue;
                case 3: return Alpha;
            }

            throw new ArgumentOutOfRangeException(nameof(component));
        }
    }

    class ShaderHeader
    {
        public int SphType { get; }
        public int Version { get; }

        public ShaderStage Stage { get; }

        public bool MrtEnable { get; }

        public bool KillsPixels { get; }

        public bool DoesGlobalStore { get; }

        public int SassVersion { get; }

        public bool GpPassthrough { get; }

        public bool DoesLoadOrStore { get; }
        public bool DoesFp64        { get; }

        public int StreamOutMask { get; }

        public int ShaderLocalMemoryLowSize { get; }

        public int PerPatchAttributeCount { get; }

        public int ShaderLocalMemoryHighSize { get; }

        public int ThreadsPerInputPrimitive { get; }

        public int ShaderLocalMemoryCrsSize { get; }

        public OutputTopology OutputTopology { get; }

        public int MaxOutputVertexCount { get; }

        public int StoreReqStart { get; }
        public int StoreReqEnd   { get; }

        public ImapPixelType[] ImapTypes { get; }

        public OmapTarget[] OmapTargets    { get; }
        public bool         OmapSampleMask { get; }
        public bool         OmapDepth      { get; }

        public ShaderHeader(IGpuAccessor gpuAccessor, ulong address)
        {
            ReadOnlySpan<int> header = MemoryMarshal.Cast<ulong, int>(gpuAccessor.GetCode(address, 0x50));

            int commonWord0 = header[0];
            int commonWord1 = header[1];
            int commonWord2 = header[2];
            int commonWord3 = header[3];
            int commonWord4 = header[4];

            SphType = commonWord0.Extract(0, 5);
            Version = commonWord0.Extract(5, 5);

            Stage = (ShaderStage)commonWord0.Extract(10, 4);

            // Invalid.
            if (Stage == ShaderStage.Compute)
            {
                Stage = ShaderStage.Vertex;
            }

            MrtEnable = commonWord0.Extract(14);

            KillsPixels = commonWord0.Extract(15);

            DoesGlobalStore = commonWord0.Extract(16);

            SassVersion = commonWord0.Extract(17, 4);

            GpPassthrough = commonWord0.Extract(24);

            DoesLoadOrStore = commonWord0.Extract(26);
            DoesFp64        = commonWord0.Extract(27);

            StreamOutMask = commonWord0.Extract(28, 4);

            ShaderLocalMemoryLowSize = commonWord1.Extract(0, 24);

            PerPatchAttributeCount = commonWord1.Extract(24, 8);

            ShaderLocalMemoryHighSize = commonWord2.Extract(0, 24);

            ThreadsPerInputPrimitive = commonWord2.Extract(24, 8);

            ShaderLocalMemoryCrsSize = commonWord3.Extract(0, 24);

            OutputTopology = (OutputTopology)commonWord3.Extract(24, 4);

            MaxOutputVertexCount = commonWord4.Extract(0, 12);

            StoreReqStart = commonWord4.Extract(12, 8);
            StoreReqEnd   = commonWord4.Extract(24, 8);

            ImapTypes = new ImapPixelType[32];

            for (int i = 0; i < 32; i++)
            {
                byte imap = (byte)(header[6 + (i >> 2)] >> ((i & 3) * 8));

                ImapTypes[i] = new ImapPixelType(
                    (PixelImap)((imap >> 0) & 3),
                    (PixelImap)((imap >> 2) & 3),
                    (PixelImap)((imap >> 4) & 3),
                    (PixelImap)((imap >> 6) & 3));
            }

            int type2OmapTarget = header[18];
            int type2Omap       = header[19];

            OmapTargets = new OmapTarget[8];

            for (int offset = 0; offset < OmapTargets.Length * 4; offset += 4)
            {
                OmapTargets[offset >> 2] = new OmapTarget(
                    type2OmapTarget.Extract(offset + 0),
                    type2OmapTarget.Extract(offset + 1),
                    type2OmapTarget.Extract(offset + 2),
                    type2OmapTarget.Extract(offset + 3));
            }

            OmapSampleMask = type2Omap.Extract(0);
            OmapDepth      = type2Omap.Extract(1);
        }
    }
}