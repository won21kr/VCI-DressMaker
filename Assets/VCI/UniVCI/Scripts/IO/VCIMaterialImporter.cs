﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VCIGLTF;

namespace VCI
{
    public class VCIMaterialImporter : MaterialImporter
    {
        private List<glTF_VCI_Material> m_materials;

        public VCIMaterialImporter(ImporterContext context, List<glTF_VCI_Material> materials) : base(
            new ShaderStore(context), context)
        {
            m_materials = materials;
        }

        private static string[] VRM_SHADER_NAMES =
        {
            "Standard",
            "VRM/MToon",
            "UniGLTF/UniUnlit",

            "VRM/UnlitTexture",
            "VRM/UnlitCutout",
            "VRM/UnlitTransparent",
            "VRM/UnlitTransparentZWrite",
        };

        public override Material CreateMaterial(glTF gltf, int i, glTFMaterial src)
        {
            if (i == 0 && m_materials.Count == 0) return base.CreateMaterial(gltf, i, src);

            var item = m_materials[i];
            var shaderName = item.shader;
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                //
                // no shader
                //
                if (VRM_SHADER_NAMES.Contains(shaderName))
                    Debug.LogErrorFormat(
                        "shader {0} not found. set Assets/VRM/Shaders/VRMShaders to Edit - project setting - Graphics - preloaded shaders",
                        shaderName);
                else
                    Debug.LogFormat("unknown shader {0}.", shaderName);
                return base.CreateMaterial(gltf, i, src);
            }

            //
            // restore VRM material
            //
            var material = new Material(shader);
            material.name = item.name;
            material.renderQueue = item.renderQueue;

            foreach (var kv in item.floatProperties) material.SetFloat(kv.Key, kv.Value);
            foreach (var kv in item.vectorProperties)
                if (item.textureProperties.ContainsKey(kv.Key))
                {
                    // texture offset & scale
                    material.SetTextureOffset(kv.Key, new Vector2(kv.Value[0], kv.Value[1]));
                    material.SetTextureScale(kv.Key, new Vector2(kv.Value[2], kv.Value[3]));
                }
                else
                {
                    // vector4
                    var v = new Vector4(kv.Value[0], kv.Value[1], kv.Value[2], kv.Value[3]);
                    material.SetVector(kv.Key, v);
                }

            foreach (var kv in item.textureProperties)
            {
                var texture = Context.GetTexture(kv.Value);
                if (texture != null)
                {
                    var converted = texture.ConvertTexture(kv.Key);
                    if (converted != null)
                        material.SetTexture(kv.Key, converted);
                    else
                        material.SetTexture(kv.Key, texture.Texture);
                }
            }

            foreach (var kv in item.keywordMap)
                if (kv.Value)
                    material.EnableKeyword(kv.Key);
                else
                    material.DisableKeyword(kv.Key);
            foreach (var kv in item.tagMap) material.SetOverrideTag(kv.Key, kv.Value);

            return material;
        }
    }
}