﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using SharpDX;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX.Model
#else
namespace HelixToolkit.UWP.Model
#endif
{
    using Shaders;
    using System.IO;
    using Utilities;

    public struct VolumeTextureParams
    {
        public byte[] VolumeTextures { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public global::SharpDX.DXGI.Format Format { get; }
        public VolumeTextureParams(byte[] data, int width, int height, int depth, global::SharpDX.DXGI.Format format)
        {
            VolumeTextures = data;
            Width = width;
            Height = height;
            Depth = depth;
            Format = format;
        }
    }

    public struct VolumeTextureGradientParams
    {
        public Half4[] VolumeTextures { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public global::SharpDX.DXGI.Format Format { get; }
        public VolumeTextureGradientParams(Half4[] data, int width, int height, int depth)
        {
            VolumeTextures = data;
            Width = width;
            Height = height;
            Depth = depth;
            Format = global::SharpDX.DXGI.Format.R16G16B16A16_Float;
        }
    }
    /// <summary>
    /// Abstract class for VolumeTextureMaterial
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class VolumeTextureMaterialCoreBase<T> : MaterialCore
    {
        private T volumeTexture;
        public T VolumeTexture
        {
            set { Set(ref volumeTexture, value); }
            get { return volumeTexture; }
        }

        private global::SharpDX.Direct3D11.SamplerStateDescription sampler = DefaultSamplers.LinearSamplerClampAni1;
        public global::SharpDX.Direct3D11.SamplerStateDescription Sampler
        {
            set { Set(ref sampler, value); }
            get { return sampler; }
        }

        private float sampleDistance = 1f;
        /// <summary>
        /// Gets or sets the step size, usually set to 1 / VolumeDepth.
        /// </summary>
        /// <value>
        /// The size of the step.
        /// </value>
        public float SampleDistance
        {
            set { Set(ref sampleDistance, value); }
            get { return sampleDistance; }
        }

        private int maxIterations = int.MaxValue;
        /// <summary>
        /// Gets or sets the iteration. Usually set to VolumeDepth.
        /// </summary>
        /// <value>
        /// The iteration.
        /// </value>
        public int MaxIterations
        {
            set { Set(ref maxIterations, value); }
            get { return maxIterations; }
        }

        private Color4 color = new Color4(1,1,1,1);
        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public Color4 Color
        {
            set { Set(ref color, value); }
            get { return color; }
        }

        private Color4[] gradientMap;
        public Color4[] GradientMap
        {
            set { Set(ref gradientMap, value); }
            get { return gradientMap; }
        }

        protected virtual string DefaultPassName { get; } = DefaultPassNames.Default;

        public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
        {
            return new VolumeMaterialVariable<T>(manager, technique, this, DefaultPassName)
            {
                OnCreateTexture = (material, effectsManager) => { return OnCreateTexture(effectsManager); }
            };
        }

        protected abstract ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager);
    }

    /// <summary>
    /// Default Volume Texture Material. Supports 3D DDS memory stream as <see cref="VolumeTextureMaterialCoreBase{T}.VolumeTexture"/>
    /// </summary>
    public sealed class VolumeTextureDDS3DMaterialCore : VolumeTextureMaterialCoreBase<Stream>
    {
        protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager)
        {
            return manager.MaterialTextureManager.Register(VolumeTexture);
        }
    }
    /// <summary>
    /// Used to use raw data as Volume 3D texture. 
    /// User must create their own data reader to read texture files as pixel byte[] and pass the necessary information as <see cref="VolumeTextureParams"/>
    /// <para>
    /// Pixel Byte[] is equal to Width * Height * Depth * BytesPerPixel.
    /// </para>
    /// </summary>
    public sealed class VolumeTextureRawDataMaterialCore : VolumeTextureMaterialCoreBase<VolumeTextureParams>
    {
        protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager)
        {
            if (VolumeTexture.VolumeTextures != null)
            {
                return ShaderResourceViewProxy.CreateViewFromPixelData(manager.Device, VolumeTexture.VolumeTextures,
                VolumeTexture.Width, VolumeTexture.Height, VolumeTexture.Depth, VolumeTexture.Format);
            }
            else
            {
                return null;
            }
        }

        public static VolumeTextureParams LoadRAWFile(string filename, int width, int height, int depth)
        {
            using (FileStream file = new FileStream(filename, FileMode.Open))
            {
                long length = file.Length;
                var bytePerPixel = length / (width * height * depth);
                byte[] buffer = new byte[width * height * depth * bytePerPixel];
                using (BinaryReader reader = new BinaryReader(file))
                {                   
                    reader.Read(buffer, 0, buffer.Length);
                }
                var format = global::SharpDX.DXGI.Format.Unknown;
                switch (bytePerPixel)
                {
                    case 1:
                        format = global::SharpDX.DXGI.Format.R8_UNorm;
                        break;
                    case 2:
                        format = global::SharpDX.DXGI.Format.R16_UNorm;
                        break;
                    case 4:
                        format = global::SharpDX.DXGI.Format.R32_Float;
                        break;
                }
                return new VolumeTextureParams(buffer, width, height, depth, format);               
            }              
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class VolumeTextureDiffuseMaterialCore : VolumeTextureMaterialCoreBase<VolumeTextureGradientParams>
    {
        protected override string DefaultPassName => DefaultPassNames.Diffuse;

        protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager)
        {
            if (VolumeTexture.VolumeTextures != null)
            {
                return ShaderResourceViewProxy.CreateViewFromPixelData(manager.Device, VolumeTexture.VolumeTextures,
                VolumeTexture.Width, VolumeTexture.Height, VolumeTexture.Depth, VolumeTexture.Format);
            }
            else
            {
                return null;
            }
        }
    }
}
