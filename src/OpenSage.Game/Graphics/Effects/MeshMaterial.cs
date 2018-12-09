﻿using System.Numerics;
using System.Runtime.InteropServices;
using OpenSage.Content;
using OpenSage.Graphics.Shaders;
using Veldrid;

namespace OpenSage.Graphics.Effects
{
    public abstract class MeshMaterial : EffectMaterial
    {
        protected MeshMaterial(ContentManager contentManager, Effect effect)
            : base(contentManager, effect)
        {
            
        }

        public void SetSkinningBuffer(DeviceBuffer skinningBuffer)
        {
            SetProperty("SkinningBuffer", skinningBuffer);
        }

        public void SetMeshConstants(DeviceBuffer meshConstants)
        {
            SetProperty("MeshConstants", meshConstants);
        }

        public void SetTeamColor(DeviceBuffer colorBuffer)
        {
            SetProperty("TeamColorBuffer", colorBuffer);
        }

        public override LightingType LightingType => LightingType.Object;

        [StructLayout(LayoutKind.Sequential)]
        public struct MeshConstants
        {
            public Bool32 HasHouseColor;
            public Bool32 SkinningEnabled;
            public uint NumBones;
#pragma warning disable CS0169
            private readonly float _padding;
#pragma warning restore CS0169
        }
    }
}
