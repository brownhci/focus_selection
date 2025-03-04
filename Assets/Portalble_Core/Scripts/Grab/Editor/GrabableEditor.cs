﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Portalble.Functions.Grab {
    [CustomEditor(typeof(Portalble.Functions.Grab.Grabable))]
    [CanEditMultipleObjects]
    public class GrabableEditor : Editor {

        private const string outlineMaterialPath = "Assets/Jiaju/Prefabs/Outline/BackFaceOutlines.mat";
        // st means structure
        SerializedProperty iInitialLock;
        // b means bool
        SerializedProperty bUseOutlineMaterialProp;
        // c means color
        SerializedProperty cSelectedColorProp;
        SerializedProperty cGrabbedColorProp;
        // m means material
        SerializedProperty mSelectedMaterialProp;
        SerializedProperty mGrabbedMaterialProp;

        SerializedProperty fThrowPowerProp;

        int tab = 0;

        void OnEnable() {
            iInitialLock = serializedObject.FindProperty("m_initialLock");
            bUseOutlineMaterialProp = serializedObject.FindProperty("m_useOutlineMaterial");
            cSelectedColorProp = serializedObject.FindProperty("m_selectedOutlineColor");
            cGrabbedColorProp = serializedObject.FindProperty("m_grabbedOutlineColor");
            mSelectedMaterialProp = serializedObject.FindProperty("m_selectedMaterial");
            mGrabbedMaterialProp = serializedObject.FindProperty("m_grabbedMaterial");
            fThrowPowerProp = serializedObject.FindProperty("m_throwPower");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            SerializedProperty prop = serializedObject.GetIterator();

            // Set color
            tab = GUILayout.Toolbar(tab, new string[] { "Style", "Dynamic" });
            switch (tab) {
                case 0:
                    EditorGUILayout.PropertyField(bUseOutlineMaterialProp, new GUIContent("DefaultMaterial"));
                    if (bUseOutlineMaterialProp.hasMultipleDifferentValues) {
                        EditorGUILayout.HelpBox("You selected multiple grabable objects with different DefaultMaterial setting",
                            MessageType.Warning);
                    }
                    else {
                        if (bUseOutlineMaterialProp.boolValue) {
                            mSelectedMaterialProp.objectReferenceValue = 
                                AssetDatabase.LoadAssetAtPath(outlineMaterialPath, typeof(Material)) as Material;
                            EditorGUILayout.PropertyField(cSelectedColorProp, new GUIContent("Color when selected"));
                            EditorGUILayout.PropertyField(cGrabbedColorProp, new GUIContent("Color when grabbed"));
                        }
                        else {
                            EditorGUILayout.PropertyField(mSelectedMaterialProp, new GUIContent("Material when selected"));
                            EditorGUILayout.PropertyField(mGrabbedMaterialProp, new GUIContent("Material when grabbed"));
                        }
                    }
                    break;
                case 1:
                    // TODO: add locks
                    GrabableConfig config = new GrabableConfig(iInitialLock.intValue);
                    EditorGUILayout.LabelField("Position lock:");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUIUtility.labelWidth = 1f;
                    bool px, py, pz;
                    px = EditorGUILayout.ToggleLeft("X", config.isLocked(GrabableConfig.POS_LOCK_X));
                    py = EditorGUILayout.ToggleLeft("Y", config.isLocked(GrabableConfig.POS_LOCK_Y));
                    pz = EditorGUILayout.ToggleLeft("Z", config.isLocked(GrabableConfig.POS_LOCK_Z));
                    EditorGUILayout.EndHorizontal();
                    config.setLock(GrabableConfig.POS_LOCK_X, px);
                    config.setLock(GrabableConfig.POS_LOCK_Y, py);
                    config.setLock(GrabableConfig.POS_LOCK_Z, pz);

                    iInitialLock.intValue = config.GetRawLockData();

                    EditorGUILayout.LabelField("Throw power:");
                    EditorGUILayout.PropertyField(fThrowPowerProp);
                    
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
