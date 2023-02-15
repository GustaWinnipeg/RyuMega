﻿using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System.Collections.Generic;
using System.Numerics;

namespace Ryujinx.Graphics.Vulkan
{
    static class PipelineLayoutFactory
    {
        private const ShaderStageFlags SupportBufferStages =
            ShaderStageFlags.VertexBit |
            ShaderStageFlags.FragmentBit |
            ShaderStageFlags.ComputeBit;

        private const ShaderStageFlags AllStages =
            ShaderStageFlags.VertexBit |
            ShaderStageFlags.TessellationControlBit |
            ShaderStageFlags.TessellationEvaluationBit |
            ShaderStageFlags.GeometryBit |
            ShaderStageFlags.FragmentBit |
            ShaderStageFlags.ComputeBit;

        public static unsafe DescriptorSetLayout[] Create(VulkanRenderer gd, Device device, uint stages, bool usePd, out PipelineLayout layout)
        {
            int stagesCount = BitOperations.PopCount(stages);

            int uCount = Constants.MaxUniformBuffersPerStage * stagesCount + 1;
            int tCount = Constants.MaxTexturesPerStage * 2 * stagesCount;
            int iCount = Constants.MaxImagesPerStage * 2 * stagesCount;

            DescriptorSetLayoutBinding* uLayoutBindings = stackalloc DescriptorSetLayoutBinding[uCount];
            DescriptorSetLayoutBinding* sLayoutBindings = stackalloc DescriptorSetLayoutBinding[stagesCount];
            DescriptorSetLayoutBinding* tLayoutBindings = stackalloc DescriptorSetLayoutBinding[tCount];
            DescriptorSetLayoutBinding* iLayoutBindings = stackalloc DescriptorSetLayoutBinding[iCount];

            uLayoutBindings[0] = new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = SupportBufferStages
            };

            DescriptorSetLayoutBinding* btLayoutBindings = stackalloc DescriptorSetLayoutBinding[2];

            btLayoutBindings[0] = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = AllStages
            };

            // TODO: Query maximum count supported by the device and use that instead.
            btLayoutBindings[1] = new DescriptorSetLayoutBinding()
            {
                Binding = 1,
                DescriptorType = DescriptorType.SampledImage,
                DescriptorCount = 32768,
                StageFlags = AllStages
            };

            DescriptorSetLayoutBinding bsLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.Sampler,
                DescriptorCount = 32768,
                StageFlags = AllStages
            };

            DescriptorSetLayoutBinding biLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.StorageImage,
                DescriptorCount = 32768,
                StageFlags = AllStages
            };

            DescriptorSetLayoutBinding bbtLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformTexelBuffer,
                DescriptorCount = 32768,
                StageFlags = AllStages
            };

            DescriptorSetLayoutBinding bbiLayoutBinding = new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorType = DescriptorType.StorageTexelBuffer,
                DescriptorCount = 32768,
                StageFlags = AllStages
            };

            int iter = 0;

            while (stages != 0)
            {
                int stage = BitOperations.TrailingZeroCount(stages);
                stages &= ~(1u << stage);

                var stageFlags = stage switch
                {
                    1 => ShaderStageFlags.FragmentBit,
                    2 => ShaderStageFlags.GeometryBit,
                    3 => ShaderStageFlags.TessellationControlBit,
                    4 => ShaderStageFlags.TessellationEvaluationBit,
                    _ => ShaderStageFlags.VertexBit | ShaderStageFlags.ComputeBit
                };

                void Set(DescriptorSetLayoutBinding* bindings, int maxPerStage, DescriptorType type, int start, int skip)
                {
                    int totalPerStage = maxPerStage * skip;

                    for (int i = 0; i < maxPerStage; i++)
                    {
                        bindings[start + iter * totalPerStage + i] = new DescriptorSetLayoutBinding
                        {
                            Binding = (uint)(start + stage * totalPerStage + i),
                            DescriptorType = type,
                            DescriptorCount = 1,
                            StageFlags = stageFlags
                        };
                    }
                }

                void SetStorage(DescriptorSetLayoutBinding* bindings, int maxPerStage, int start = 0)
                {
                    bindings[start + iter] = new DescriptorSetLayoutBinding
                    {
                        Binding = (uint)(start + stage * maxPerStage),
                        DescriptorType = DescriptorType.StorageBuffer,
                        DescriptorCount = (uint)maxPerStage,
                        StageFlags = stageFlags
                    };
                }

                Set(uLayoutBindings, Constants.MaxUniformBuffersPerStage, DescriptorType.UniformBuffer, 1, 1);
                SetStorage(sLayoutBindings, Constants.MaxStorageBuffersPerStage);
                Set(tLayoutBindings, Constants.MaxTexturesPerStage, DescriptorType.CombinedImageSampler, 0, 2);
                Set(tLayoutBindings, Constants.MaxTexturesPerStage, DescriptorType.UniformTexelBuffer, Constants.MaxTexturesPerStage, 2);
                Set(iLayoutBindings, Constants.MaxImagesPerStage, DescriptorType.StorageImage, 0, 2);
                Set(iLayoutBindings, Constants.MaxImagesPerStage, DescriptorType.StorageTexelBuffer, Constants.MaxImagesPerStage, 2);

                iter++;
            }

            DescriptorSetLayout[] layouts = new DescriptorSetLayout[PipelineBase.DescriptorSetLayoutsBindless];

            var uDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = uLayoutBindings,
                BindingCount = (uint)uCount,
                Flags = usePd ? DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr : 0
            };

            var sDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = sLayoutBindings,
                BindingCount = (uint)stagesCount
            };

            var tDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = tLayoutBindings,
                BindingCount = (uint)tCount
            };

            var iDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = iLayoutBindings,
                BindingCount = (uint)iCount
            };

            var btBindingsFlags = stackalloc DescriptorBindingFlags[] { 0, DescriptorBindingFlags.UpdateAfterBindBit };

            var btDescriptorSetLayoutFlagsCreateInfo = new DescriptorSetLayoutBindingFlagsCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                PBindingFlags = btBindingsFlags,
                BindingCount = 2
            };

            var btDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PNext = &btDescriptorSetLayoutFlagsCreateInfo,
                Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
                PBindings = btLayoutBindings,
                BindingCount = 2
            };

            var bsBindingFlag = DescriptorBindingFlags.UpdateAfterBindBit;

            var bsDescriptorSetLayoutFlagsCreateInfo = new DescriptorSetLayoutBindingFlagsCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
                PBindingFlags = &bsBindingFlag,
                BindingCount = 1
            };

            var bsDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PNext = &bsDescriptorSetLayoutFlagsCreateInfo,
                Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
                PBindings = &bsLayoutBinding,
                BindingCount = 1
            };

            var biDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PNext = &bsDescriptorSetLayoutFlagsCreateInfo,
                Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
                PBindings = &biLayoutBinding,
                BindingCount = 1
            };

            var bbtDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PNext = &bsDescriptorSetLayoutFlagsCreateInfo,
                Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
                PBindings = &bbtLayoutBinding,
                BindingCount = 1
            };

            var bbiDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PNext = &bsDescriptorSetLayoutFlagsCreateInfo,
                Flags = DescriptorSetLayoutCreateFlags.UpdateAfterBindPoolBit,
                PBindings = &bbiLayoutBinding,
                BindingCount = 1
            };

            gd.Api.CreateDescriptorSetLayout(device, uDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.UniformSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, sDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.StorageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, tDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.TextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, iDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.ImageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, btDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.BindlessTexturesSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bsDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.BindlessSamplersSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, biDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.BindlessImagesSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bbtDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.BindlessBufferTextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, bbiDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.BindlessBufferImageSetIndex]).ThrowOnError();

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = PipelineBase.DescriptorSetLayoutsBindless
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return layouts;
        }

        public static unsafe DescriptorSetLayout[] CreateMinimal(VulkanRenderer gd, Device device, ShaderSource[] shaders, out PipelineLayout layout)
        {
            int stagesCount = shaders.Length;

            int uCount = 0;
            int sCount = 0;
            int tCount = 0;
            int iCount = 0;

            foreach (var shader in shaders)
            {
                uCount += shader.Bindings.UniformBufferBindings.Count;
                sCount += shader.Bindings.StorageBufferBindings.Count;
                tCount += shader.Bindings.TextureBindings.Count;
                iCount += shader.Bindings.ImageBindings.Count;
            }

            DescriptorSetLayoutBinding* uLayoutBindings = stackalloc DescriptorSetLayoutBinding[uCount];
            DescriptorSetLayoutBinding* sLayoutBindings = stackalloc DescriptorSetLayoutBinding[sCount];
            DescriptorSetLayoutBinding* tLayoutBindings = stackalloc DescriptorSetLayoutBinding[tCount];
            DescriptorSetLayoutBinding* iLayoutBindings = stackalloc DescriptorSetLayoutBinding[iCount];

            int uIndex = 0;
            int sIndex = 0;
            int tIndex = 0;
            int iIndex = 0;

            foreach (var shader in shaders)
            {
                var stageFlags = shader.Stage.Convert();

                void Set(DescriptorSetLayoutBinding* bindings, DescriptorType type, ref int start, IEnumerable<int> bds)
                {
                    foreach (var b in bds)
                    {
                        bindings[start++] = new DescriptorSetLayoutBinding
                        {
                            Binding = (uint)b,
                            DescriptorType = type,
                            DescriptorCount = 1,
                            StageFlags = stageFlags
                        };
                    }
                }

                // TODO: Support buffer textures and images here.
                // This is only used for the helper shaders on the backend, and we don't use buffer textures on them
                // so far, so it's not really necessary right now.
                Set(uLayoutBindings, DescriptorType.UniformBuffer, ref uIndex, shader.Bindings.UniformBufferBindings);
                Set(sLayoutBindings, DescriptorType.StorageBuffer, ref sIndex, shader.Bindings.StorageBufferBindings);
                Set(tLayoutBindings, DescriptorType.CombinedImageSampler, ref tIndex, shader.Bindings.TextureBindings);
                Set(iLayoutBindings, DescriptorType.StorageImage, ref iIndex, shader.Bindings.ImageBindings);
            }

            DescriptorSetLayout[] layouts = new DescriptorSetLayout[PipelineBase.DescriptorSetLayouts];

            var uDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = uLayoutBindings,
                BindingCount = (uint)uCount
            };

            var sDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = sLayoutBindings,
                BindingCount = (uint)sCount
            };

            var tDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = tLayoutBindings,
                BindingCount = (uint)tCount
            };

            var iDescriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                PBindings = iLayoutBindings,
                BindingCount = (uint)iCount
            };

            gd.Api.CreateDescriptorSetLayout(device, uDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.UniformSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, sDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.StorageSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, tDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.TextureSetIndex]).ThrowOnError();
            gd.Api.CreateDescriptorSetLayout(device, iDescriptorSetLayoutCreateInfo, null, out layouts[PipelineBase.ImageSetIndex]).ThrowOnError();

            fixed (DescriptorSetLayout* pLayouts = layouts)
            {
                var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    PSetLayouts = pLayouts,
                    SetLayoutCount = PipelineBase.DescriptorSetLayouts
                };

                gd.Api.CreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, out layout).ThrowOnError();
            }

            return layouts;
        }
    }
}
