using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Rendering.LWRP;

namespace UnityEditor.Rendering.LWRP.ShaderGUI
{
    internal class AdvancedDisolve_LitShader : BaseShaderGUI
    {
        // Properties
        private UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.LitProperties litProperties;
        
        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            litProperties = new UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.LitProperties(properties);


             //VacuumShaders
            VacuumShaders.AdvancedDissolve.MaterialProperties.Init(properties);
        }

        // material changed check
        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
            
            SetMaterialKeywords(material, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);


            //VacuumShaders
            VacuumShaders.AdvancedDissolve.MaterialProperties.MaterialChanged(material);
        }
        
        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            if (litProperties.workflowMode != null)
            {
                DoPopup(UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.Styles.workflowModeText, litProperties.workflowMode, Enum.GetNames(typeof(UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.WorkflowMode)));
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }
            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.Inputs(litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            if (litProperties.reflections != null && litProperties.highlights != null)
            {
                EditorGUI.BeginChangeCheck();
                {
                    materialEditor.ShaderProperty(litProperties.highlights, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.Styles.highlightsText);
                    materialEditor.ShaderProperty(litProperties.reflections, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.Styles.reflectionsText);
                    EditorGUI.BeginChangeCheck();
                }
            }

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            if (oldShader.name.Equals("Standard (Specular setup)"))
            {
                material.SetFloat("_WorkflowMode", (float)UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.WorkflowMode.Specular);
                Texture texture = material.GetTexture("_SpecGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }
            else
            {
                material.SetFloat("_WorkflowMode", (float)UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.WorkflowMode.Metallic);
                Texture texture = material.GetTexture("_MetallicGlossMap");
                if (texture != null)
                    material.SetTexture("_MetallicSpecGlossMap", texture);
            }

            MaterialChanged(material);
        }

        public override void DrawAdditionalFoldouts(Material material)
        {
            //VacuumShaders
            VacuumShaders.AdvancedDissolve.MaterialProperties.DrawDissolveOptions(materialEditor, true, false);
        }
    }
}
