
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering; 

using Battlehub.Utils;
using Battlehub.RTCommon;
using System;
using System.Linq;
using Battlehub.Storage;

namespace Battlehub.RTEditor
{
    [BuiltInDescriptor]
    public class URPLitMaterialDescriptor : IMaterialDescriptor
    {
        public string ShaderName
        {
            get { return "Universal Render Pipeline/Lit"; } 
        }

        public object GetValue(object[] convertersOrMaterials, Func<object, object> getter)
        {
            if (convertersOrMaterials == null || convertersOrMaterials.Length == 0 || convertersOrMaterials[0] == null)
            {
                return null;
            }

            object val = getter(convertersOrMaterials[0]);
            for (int i = 1; i < convertersOrMaterials.Length; ++i)
            {
                object item = convertersOrMaterials[i];
                if (item == null) return null;
                object val2 = getter(item);
                if (!Equals(val, val2)) return null;
            }
            return val;
        }
        
        public object CreateConverter(MaterialEditor editor)
        {
            return editor.Materials
                .Where(m => m != null)
                .Select(m => (object)new URPLitMaterialValueConverter(m)) 
                .ToArray();
        }

        public void EraseAccessorTarget(object accessorRef, object target)
        {
            if (accessorRef is URPLitMaterialValueConverter conv)
            {
                conv.Material = target as Material;
            }
            else if (accessorRef is MaterialPropertyAccessor accessor)
            {
                accessor.Material = target as Material;
            }
        }

        private MaterialPropertyAccessor[] CreateAccessors(MaterialEditor editor, string propertyName)
        {
            return editor.Materials
                .Where(m => m != null)
                .Select(m => new MaterialPropertyAccessor(m, propertyName))
                .ToArray();
        }

        public MaterialPropertyDescriptor[] GetProperties(MaterialEditor editor, object converterObject)
        {
            ILocalization lc = IOC.Resolve<ILocalization>();
            PropertyEditorCallback valueChangedCallback = () => editor.BuildEditor(); 

            URPLitMaterialValueConverter[] converters = ((object[])converterObject)
                                                        .Cast<URPLitMaterialValueConverter>()
                                                        .ToArray();
            if(converters.Length == 0) return new MaterialPropertyDescriptor[0];


            List<MaterialPropertyDescriptor> properties = new List<MaterialPropertyDescriptor>();

            // --- Surface Options ---
            var workflowModeInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.WorkflowMode, "WorkflowMode");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_WorkflowMode", "Workflow"), RTShaderPropertyType.Float, workflowModeInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback,  EraseAccessorTarget));

            URPWorkflowMode? currentWorkflow = (URPWorkflowMode?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).WorkflowMode);

            var surfaceTypeInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.SurfaceType, "SurfaceType");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_SurfaceType", "Surface Type"), RTShaderPropertyType.Float, surfaceTypeInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback,  EraseAccessorTarget));

            URPSurfaceType? currentSurfaceType = (URPSurfaceType?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).SurfaceType);

           
            if (currentSurfaceType == URPSurfaceType.Transparent)
            {
                var blendModeInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Blend, "Blend");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_BlendMode", "Blending Mode"), RTShaderPropertyType.Float, blendModeInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None,  valueChangedCallback, EraseAccessorTarget));
            }
            var cullModeInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Cull, "Cull");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_RenderFace", "Render Face"), RTShaderPropertyType.Float, cullModeInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
            var alphaClipInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.AlphaClip, "AlphaClip");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_AlphaClip", "Alpha Clipping"), RTShaderPropertyType.Float, alphaClipInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback,  EraseAccessorTarget));

            bool? isAlphaClipEnabled = (bool?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).AlphaClip);
            if (isAlphaClipEnabled == true)
            {
                var cutoffInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Cutoff, "Cutoff");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_AlphaCutoff", "Alpha Cutoff"), RTShaderPropertyType.Range, cutoffInfo, new RTShaderInfo.RangeLimits(0.5f, 0.0f, 1.0f), TextureDimension.None, null, EraseAccessorTarget));
            }
                    
            // --- Surface Inputs ---
            var baseMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.BaseMap, "BaseMap");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_BaseMap", "Base Map"), RTShaderPropertyType.TexEnv, baseMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, callback: valueChangedCallback, eraseTargetCallback: EraseAccessorTarget));
            
            var baseColorInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.BaseColor, "BaseColor");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_BaseColor", "Base Color"), RTShaderPropertyType.Color, baseColorInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None,null, EraseAccessorTarget));

            if(currentWorkflow == URPWorkflowMode.Metallic)
            {
                var metallicMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.MetallicGlossMap, "MetallicGlossMap");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_MetallicMap", "Metallic Map (R:Met, A:Smooth)"), RTShaderPropertyType.TexEnv, metallicMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, callback: valueChangedCallback, eraseTargetCallback: EraseAccessorTarget));

                var metallicInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Metallic, "Metallic");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_Metallic", "Metallic"), RTShaderPropertyType.Range, metallicInfo, new RTShaderInfo.RangeLimits(0f, 0f, 1f), TextureDimension.None, null, eraseTargetCallback: EraseAccessorTarget));
            }
            else // Specular Workflow
            {
                var specGlossMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.SpecGlossMap, "SpecGlossMap");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_SpecularMap", "Specular Map (RGB:Spec, A:Smooth)"), RTShaderPropertyType.TexEnv, specGlossMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, callback: valueChangedCallback, eraseTargetCallback: EraseAccessorTarget));
                
                var specColorInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.SpecColor, "SpecColor");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_SpecularColor", "Specular Color"), RTShaderPropertyType.Color, specColorInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
            }

            var smoothnessInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Smoothness, "Smoothness");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_Smoothness", "Smoothness"), RTShaderPropertyType.Range, smoothnessInfo, new RTShaderInfo.RangeLimits(0.5f, 0f, 1f), TextureDimension.None, null, EraseAccessorTarget));

            var normalMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.BumpMap, "BumpMap");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_NormalMap", "Normal Map"), RTShaderPropertyType.TexEnv, normalMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, callback: valueChangedCallback, eraseTargetCallback: EraseAccessorTarget));
            
            bool? hasNormalMap = (bool?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).BumpMap != null);
            if(hasNormalMap == true)
            {
                var normalScaleInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.BumpScale, "BumpScale");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_NormalScale", "Normal Scale"), RTShaderPropertyType.Float, normalScaleInfo, new RTShaderInfo.RangeLimits(1f, 0f, 2f), TextureDimension.None, null, EraseAccessorTarget)); // URP default is 1, range can vary
            }

            var heightMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.ParallaxMap, "ParallaxMap");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_HeightMap", "Height Map"), RTShaderPropertyType.TexEnv, heightMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));

            bool? hasHeightMap = (bool?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).ParallaxMap != null);
            if (hasHeightMap == true)
            {
                var heightScaleInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Parallax, "Parallax"); // Parallax is height scale
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_HeightScale", "Height Scale"), RTShaderPropertyType.Range, heightScaleInfo, new RTShaderInfo.RangeLimits(0.02f, 0.005f, 0.08f), TextureDimension.None, null, EraseAccessorTarget));
            }

            var occlusionMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.OcclusionMap, "OcclusionMap");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_OcclusionMap", "Occlusion Map"), RTShaderPropertyType.TexEnv, occlusionMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));

            bool? hasOcclusionMap = (bool?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).OcclusionMap != null);
            if (hasOcclusionMap == true)
            {
                var occlusionStrengthInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.OcclusionStrength, "OcclusionStrength");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_OcclusionStrength", "Occlusion Strength"), RTShaderPropertyType.Range, occlusionStrengthInfo, new RTShaderInfo.RangeLimits(1.0f, 0f, 1f), TextureDimension.None, null, EraseAccessorTarget));
            }

            var emissionInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.Emission, "Emission");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_Emission", "Emission"), RTShaderPropertyType.Float, emissionInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback, EraseAccessorTarget));
            bool? isEmissionEnabled = (bool?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).Emission);
            if (isEmissionEnabled == true)
            {
                var emissionMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.EmissionMap, "EmissionMap");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_EmissionMap", "Emission Map"), RTShaderPropertyType.TexEnv, emissionMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, valueChangedCallback, EraseAccessorTarget));
                var emissionColorInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.EmissionColor, "EmissionColor");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_EmissionColor", "Emission Color"), RTShaderPropertyType.Color, emissionColorInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
           }
                                
            // --- Tiling & Offset for Base Map ---
            var baseMapTilingInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.BaseMapTiling, "BaseMapTiling");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_BaseTiling", "Base Map Tiling"), RTShaderPropertyType.Vector, baseMapTilingInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
            
            var baseMapOffsetInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.BaseMapOffset, "BaseMapOffset");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_BaseOffset", "Base Map Offset"), RTShaderPropertyType.Vector, baseMapOffsetInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));


            // --- Detail Inputs ---
            var detailMaskInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.DetailMask, "DetailMask");
             properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_DetailMask", "Detail Mask"), RTShaderPropertyType.TexEnv, detailMaskInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D,valueChangedCallback, EraseAccessorTarget));

            var detailAlbedoMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.DetailAlbedoMap, "DetailAlbedoMap");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_DetailAlbedo", "Detail Albedo x2"), RTShaderPropertyType.TexEnv, detailAlbedoMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D, valueChangedCallback,  EraseAccessorTarget));
            
            var detailNormalMapInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.DetailNormalMap, "DetailNormalMap");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_DetailNormal", "Detail Normal Map"), RTShaderPropertyType.TexEnv, detailNormalMapInfo, new RTShaderInfo.RangeLimits(), TextureDimension.Tex2D,  valueChangedCallback, EraseAccessorTarget));

            bool? hasDetailMap = (bool?)GetValue(converters, c => ((URPLitMaterialValueConverter)c).DetailAlbedoMap != null || ((URPLitMaterialValueConverter)c).DetailNormalMap != null);
            if(hasDetailMap == true)
            {
                // Detail Normal Scale
                 var detailNormalScaleInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.DetailNormalMapScale, "DetailNormalMapScale");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_DetailNormalScale", "Detail Normal Scale"), RTShaderPropertyType.Float, detailNormalScaleInfo, new RTShaderInfo.RangeLimits(1f, 0f, 2f), TextureDimension.None, null, EraseAccessorTarget));
                
                // Detail Tiling & Offset
                var detailMapTilingInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.DetailMapTiling, "DetailMapTiling");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_DetailTiling", "Detail Tiling"), RTShaderPropertyType.Vector, detailMapTilingInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
                
                var detailMapOffsetInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.DetailMapOffset, "DetailMapOffset");
                properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLit_DetailOffset", "Detail Offset"), RTShaderPropertyType.Vector, detailMapOffsetInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, null, EraseAccessorTarget));
            }

            // --- Advanced Options can be added here (e.g. Receive Shadows, Clear Coat etc.)
            // For Clear Coat, you'd need to add properties like _ClearCoatMap, _ClearCoatMask, _ClearCoat, _ClearCoatSmoothness
            // and manage keywords _CLEARCOAT and _CLEARCOATMAP.
            // Advanced Options
            PropertyInfo environmentReflectionsInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.EnvironmentReflections, "EnvironmentReflections");
            PropertyInfo specularHighlightsInfo = Strong.PropertyInfo((URPLitMaterialValueConverter x) => x.SpecularHighlights, "SpecularHighlights");
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLitMaterial_SpecularHighlights", "Specular Highlights"), RTShaderPropertyType.Float, specularHighlightsInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback, EraseAccessorTarget));
            properties.Add(new MaterialPropertyDescriptor(editor.Materials, converters, lc.GetString("ID_RTEditor_CD_URPLitMaterial_EnvironmentReflections", "Environment Reflections"), RTShaderPropertyType.Float, environmentReflectionsInfo, new RTShaderInfo.RangeLimits(), TextureDimension.None, valueChangedCallback, EraseAccessorTarget));
            return properties.ToArray();
        }
    }
   
    public enum URPSurfaceType
    {
        Opaque = 0,
        Transparent = 1
    }

    public enum URPBlendMode
    {
        Alpha = 0,      // 
        Premultiply = 1, //
        Additive = 2,   // 
        Multiply = 3    // 
    }

    public enum URPWorkflowMode
    {
        Specular = 0,
        Metallic = 1,
    }

    public enum URPCullMode
    {
        Front = 2,  // Default
        Back = 1,
        Both = 0
    }

    public class URPLitMaterialValueConverter
    {
        public Material Material { get; set; }

        public URPLitMaterialValueConverter(Material material)
        {
            this.Material = material;
        }

        // Helper to manage keywords
        private void SetKeyword(string keyword, bool enabled)
        {
            if (enabled) Material.EnableKeyword(keyword);
            else Material.DisableKeyword(keyword);
           // Debug.Log(">>>>>" + Material.IsKeywordEnabled(keyword));
        }

        public URPSurfaceType SurfaceType
        {
            get { return (URPSurfaceType)Material.GetFloat("_Surface"); }
            set
            {
                if (SurfaceType != value)
                {
                    Material.SetFloat("_Surface", (float)value);
                    SetKeyword("_SURFACE_TYPE_TRANSPARENT", value == URPSurfaceType.Transparent);
                    
                    if (value == URPSurfaceType.Opaque)
                    {
                        Material.SetOverrideTag("RenderType", "Opaque");
                        Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        Material.SetInt("_ZWrite", 1);
                        SetKeyword("_ALPHAPREMULTIPLY_ON", false);
                        SetKeyword("_ALPHAMODULATE_ON", false); // For multiply
                        SetKeyword("_ALPHA_BLEND_ON", false); // For alpha blend (used by some custom URP shaders)
                        Material.renderQueue = (int)RenderQueue.Geometry;
                    }
                    else // Transparent
                    {
                        // Default to Alpha blend when switching to transparent
                        Blend = URPBlendMode.Alpha; // This will trigger its own setup
                        Material.SetOverrideTag("RenderType", "Transparent");
                        Material.SetInt("_ZWrite", 0);
                        Material.renderQueue = (int)RenderQueue.Transparent;
                    }
                }
            }
        }

        public URPBlendMode Blend
        {
            get { return (URPBlendMode)Material.GetFloat("_Blend"); }
            set
            {
                if (Blend != value || SurfaceType == URPSurfaceType.Transparent) 
                {
                    Material.SetFloat("_Blend", (float)value);
                    SetKeyword("_ALPHAPREMULTIPLY_ON", value == URPBlendMode.Premultiply);
                    SetKeyword("_ALPHAMODULATE_ON", value == URPBlendMode.Multiply);
                    SetKeyword("_ALPHA_BLEND_ON", value == URPBlendMode.Alpha); // Some shaders might use this

                    switch (value)
                    {
                        case URPBlendMode.Alpha:
                            Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case URPBlendMode.Premultiply:
                            Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case URPBlendMode.Additive:
                            Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            break;
                        case URPBlendMode.Multiply:
                            Material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                            Material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                            break;
                    }
                }
            }
        }

        public URPWorkflowMode WorkflowMode
        {
            get { return (URPWorkflowMode)Material.GetFloat("_WorkflowMode"); }
            set
            {
                Material.SetFloat("_WorkflowMode", (float)value);
                SetKeyword("_SPECGLOSSMAP", value == URPWorkflowMode.Specular);
                
            }
        }


        public bool AlphaClip
        {
            get { return Material.IsKeywordEnabled("_ALPHATEST_ON"); }
            set
            {
                Material.SetFloat("_AlphaClip", value ? 1.0f : 0.0f);
                SetKeyword("_ALPHATEST_ON", value);
            }
        }

        public float Cutoff
        {
            get { return Material.GetFloat("_Cutoff"); }
            set { Material.SetFloat("_Cutoff", value); }
        }

        public URPCullMode Cull
        {
            get { return (URPCullMode)Material.GetFloat("_Cull"); }
            set { Material.SetFloat("_Cull", (float)value); }
        }

        public Texture BaseMap
        {
            get { return Material.GetTexture("_BaseMap"); }
            set { Material.SetTexture("_BaseMap", value);  }
        }

        public Color BaseColor
        {
            get { return Material.GetColor("_BaseColor"); }
            set { Material.SetColor("_BaseColor", value); }
        }

        public Texture MetallicGlossMap
        {
            get { return Material.GetTexture("_MetallicGlossMap"); }
            set
            {
                Material.SetTexture("_MetallicGlossMap", value);
                SetKeyword("_METALLICGLOSSMAP", value != null);
            }
        }

        public float Metallic
        {
            get { return Material.GetFloat("_Metallic"); }
            set { Material.SetFloat("_Metallic", value); }
        }

        public float Smoothness
        {
            get { return Material.GetFloat("_Smoothness"); }
            set { Material.SetFloat("_Smoothness", value); }
        }

        public Texture SpecGlossMap
        {
            get { return Material.GetTexture("_SpecGlossMap"); }
            set
            {
                Material.SetTexture("_SpecGlossMap", value);
                // Keyword _SPECGLOSSMAP is also tied to _WorkflowMode
                SetKeyword("_SPECGLOSSMAP", value != null && WorkflowMode == URPWorkflowMode.Specular);
            }
        }

        public Color SpecColor // Specular Color
        {
            get { return Material.GetColor("_SpecColor"); }
            set { Material.SetColor("_SpecColor", value); }
        }


        public Texture BumpMap // Normal Map
        {
            get { return Material.GetTexture("_BumpMap"); }
            set
            {
                Material.SetTexture("_BumpMap", value);
                SetKeyword("_NORMALMAP", value != null);
            }
        }

        public float BumpScale // Normal Scale
        {
            get { return Material.GetFloat("_BumpScale"); }
            set { Material.SetFloat("_BumpScale", value); }
        }

        public Texture ParallaxMap // Height Map
        {
            get { return Material.GetTexture("_ParallaxMap"); }
            set
            {
                Material.SetTexture("_ParallaxMap", value);
                SetKeyword("_PARALLAXMAP", value != null);
            }
        }

        public float Parallax // Height Scale
        {
            get { return Material.GetFloat("_Parallax"); }
            set { Material.SetFloat("_Parallax", value); }
        }

        public Texture OcclusionMap
        {
            get { return Material.GetTexture("_OcclusionMap"); }
            set
            {
                Material.SetTexture("_OcclusionMap", value);
                SetKeyword("_OCCLUSIONMAP", value != null);
            }
        }

        public float OcclusionStrength
        {
            get { return Material.GetFloat("_OcclusionStrength"); }
            set { Material.SetFloat("_OcclusionStrength", value); }
        }

        public bool Emission
        {
            get { return Material.IsKeywordEnabled("_EMISSION"); }
            set
            {                          
                SetKeyword("_EMISSION", value);
            }
        }
        private bool IsEmissionColorOff(Color color)
        {
            // Check if RGB components are all zero.
            // For HDR, (0,0,0,1) is black, (0,0,0,0) is also effectively black/off.
            // Using maxColorComponent is a robust way for HDR.
            return color.maxColorComponent == 0;
        }
        public Texture EmissionMap
        {
            get { return Material.GetTexture("_EmissionMap"); }
            set
            {
                Material.SetTexture("_EmissionMap", value);
               // SetKeyword("_EMISSION", value != null || (Material.GetColor("_EmissionColor") != Color.black && Material.GetColor("_EmissionColor") != new Color(0, 0, 0, 0))); // also check color
               // Color currentColor = Material.GetColor("_EmissionColor");
               // SetKeyword("_EMISSION", value != null || !IsEmissionColorOff(currentColor));
            }
        }

        public Color EmissionColor
        {
            get { return Material.GetColor("_EmissionColor"); }
            set
            {
                Material.SetColor("_EmissionColor", value);
               // Debug.Log("+++++++++" + value);
                // SetKeyword("_EMISSION", Material.GetTexture("_EmissionMap") != null || (value != Color.black && value != new Color(0, 0, 0, 0)));
              //  SetKeyword("_EMISSION", Material.GetTexture("_EmissionMap") != null || !IsEmissionColorOff(value));
            }
        }

        // Detail Maps
        public Texture DetailMask
        {
            get { return Material.GetTexture("_DetailMask"); }
            set { Material.SetTexture("_DetailMask", value); SetKeyword("_DETAIL_MASK", value != null); }
        }

        public Texture DetailAlbedoMap
        {
            get { return Material.GetTexture("_DetailAlbedoMap"); }
            set { Material.SetTexture("_DetailAlbedoMap", value); SetKeyword("_DETAIL_ALBEDO_MAP", value != null); UpdateDetailKeywords(); }
        }

        public Texture DetailNormalMap
        {
            get { return Material.GetTexture("_DetailNormalMap"); }
            set { Material.SetTexture("_DetailNormalMap", value); SetKeyword("_DETAIL_NORMAL_MAP", value != null); UpdateDetailKeywords(); }
        }

        public float DetailNormalMapScale
        {
            get { return Material.GetFloat("_DetailNormalMapScale"); }
            set { Material.SetFloat("_DetailNormalMapScale", value); }
        }

        

        private void UpdateDetailKeywords()
        {
            bool hasDetailMap = Material.GetTexture("_DetailAlbedoMap") || Material.GetTexture("_DetailNormalMap");
            // URP's Lit shader uses shader_feature_local _DETAIL_MULX2 _DETAIL_SCALED
          
        }


        // Tiling and Offset for Base Map
        public Vector2 BaseMapTiling
        {
            get { return Material.GetTextureScale("_BaseMap"); }
            set { Material.SetTextureScale("_BaseMap", value); }
        }

        public Vector2 BaseMapOffset
        {
            get { return Material.GetTextureOffset("_BaseMap"); }
            set { Material.SetTextureOffset("_BaseMap", value); }
        }

        // Tiling and Offset for Detail Maps
        public Vector2 DetailMapTiling
        {
            get { return Material.GetTextureScale("_DetailAlbedoMap"); } // URP detail maps share UVs
            set { Material.SetTextureScale("_DetailAlbedoMap", value); Material.SetTextureScale("_DetailNormalMap", value); }
        }

        public Vector2 DetailMapOffset
        {
            get { return Material.GetTextureOffset("_DetailAlbedoMap"); }
            set { Material.SetTextureOffset("_DetailAlbedoMap", value); Material.SetTextureOffset("_DetailNormalMap", value); }
        }

        // Add other properties as needed (e.g., Clear Coat, Advanced options)
        public bool EnvironmentReflections
        {
            get { return Material.GetFloat("_EnvironmentReflections")==1.0f; }
            set
            {
                Material.SetFloat("_EnvironmentReflections", value?1.0f:0.0f);
               // URPLitMaterialUtils.SetMaterialKeywords(Material);
            }
        }
        public bool SpecularHighlights
        {
            get { return Material.GetFloat("_SpecularHighlights") == 1.0f; }
            set
            {
                Material.SetFloat("_SpecularHighlights", value ? 1.0f : 0.0f);
               // URPLitMaterialUtils.SetMaterialKeywords(Material);
            }
        }
    }
}
