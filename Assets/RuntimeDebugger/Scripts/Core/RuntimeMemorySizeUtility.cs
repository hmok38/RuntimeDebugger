using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace RuntimeDebugger
{
    /// <summary>
    /// 运行时对象内存大小测量。
    /// 优先使用 Profiler 的原生内存统计：WebGL/微信小游戏上纹理像素数据在 JS 侧，
    /// Profiler.GetRuntimeMemorySizeLong 对这类纹理返回 0（RenderTexture 等原生创建的纹理不受影响），
    /// 此时按尺寸、格式与 mip 数估算，并通过 estimated 告知调用方该值是推算结果。
    /// </summary>
    internal static class RuntimeMemorySizeUtility
    {
        /// <summary>
        /// 获取对象占用的运行时内存大小（字节）。
        /// </summary>
        /// <param name="target">待测量的对象。</param>
        /// <param name="estimated">为 true 表示该值按纹理尺寸与格式估算，不是平台的统计值。</param>
        public static long GetRuntimeMemorySize(UnityEngine.Object target, out bool estimated)
        {
            long size = GetProfilerRuntimeMemorySize(target);
            if (size > 0L)
            {
                estimated = false;
                return size;
            }

            Texture texture = target as Texture;
            if (texture == null)
            {
                estimated = false;
                return 0L;
            }

            size = EstimateTextureSize(texture);
            estimated = size > 0L;
            return size;
        }

        private static long GetProfilerRuntimeMemorySize(UnityEngine.Object target)
        {
#if UNITY_5_6_OR_NEWER
            return Profiler.GetRuntimeMemorySizeLong(target);
#else
            return Profiler.GetRuntimeMemorySize(target);
#endif
        }

        private static long EstimateTextureSize(Texture texture)
        {
            RenderTexture renderTexture = texture as RenderTexture;
            if (renderTexture != null && !renderTexture.IsCreated())
            {
                return 0L;
            }

            int width = texture.width;
            int height = texture.height;
            if (width <= 0 || height <= 0)
            {
                return 0L;
            }

            int mipmapCount = Mathf.Max(1, texture.mipmapCount);
            long mipChainSize = 0L;
            for (int mipLevel = 0; mipLevel < mipmapCount; mipLevel++)
            {
                int mipWidth = Mathf.Max(1, width >> mipLevel);
                int mipHeight = Mathf.Max(1, height >> mipLevel);
                mipChainSize += GraphicsFormatUtility.ComputeMipmapSize(mipWidth, mipHeight, texture.graphicsFormat);
            }

            return mipChainSize * GetTextureLayerCount(texture);
        }

        private static long GetTextureLayerCount(Texture texture)
        {
            RenderTexture renderTexture = texture as RenderTexture;
            if (renderTexture != null)
            {
                return renderTexture.dimension == TextureDimension.Cube
                    ? renderTexture.volumeDepth * 6L
                    : renderTexture.volumeDepth;
            }

            CubemapArray cubemapArray = texture as CubemapArray;
            if (cubemapArray != null)
            {
                return cubemapArray.cubemapCount * 6L;
            }

            Texture2DArray texture2DArray = texture as Texture2DArray;
            if (texture2DArray != null)
            {
                return texture2DArray.depth;
            }

            Texture3D texture3D = texture as Texture3D;
            if (texture3D != null)
            {
                return texture3D.depth;
            }

            return texture.dimension == TextureDimension.Cube ? 6L : 1L;
        }
    }
}
