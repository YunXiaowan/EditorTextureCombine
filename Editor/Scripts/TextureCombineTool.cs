using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace TextureCombine
{
    public partial class TextureCombineTool : EditorWindow, ISerializationCallbackReceiver
    {
        private static TextureCombineTool _instance;
        private Vector2 _scrollPos;

        #region Data

        private Texture2D _roughnessOrSmoothnessTex = null;
        private Texture2D _metallicTex = null;
        private bool _isRoughness = false;
        private string _savePath;
        private string _saveName;
        private Object _folderObject;

        #endregion
        [MenuItem("Tool/TextureCombine")]
        public static void ShowWindow() => _instance = GetWindow<TextureCombineTool>("TextureCombine");

        private void OnEnable()
        {
            _instance = this;
            _savePath = Application.dataPath + "/CombineTextures";
        }

        private void OnGUI()
        {
            if (_instance == null)
            {
                Clear();
                return;
            }

            // Title
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("贴图合并工具", Style.TitleStyle);
                EditorGUILayout.Space();
            }

            var scroll =  new GUILayout.ScrollViewScope(_scrollPos, GUILayout.ExpandHeight(true));
            _scrollPos = scroll.scrollPosition;
            // main
            DrawTextureInput();
            // button
            DrawButtonAndState();
            scroll.Dispose();
        }

        private void DrawTextureInput()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        _roughnessOrSmoothnessTex = (Texture2D)EditorGUILayout.ObjectField("Roughness/Smoothness",_roughnessOrSmoothnessTex, typeof(Texture2D), false);
                        if (check.changed)
                        {
                            string rname = _roughnessOrSmoothnessTex.name;
                            _isRoughness = rname.Contains("_R") ? true : false;
                            int len = rname.IndexOf("_", StringComparison.Ordinal) == -1 ? rname.Length : rname.IndexOf("_", StringComparison.Ordinal);

                            _saveName = rname.Substring(0, len) + "PbrMask";
                        }
                    }

                }
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    _metallicTex = (Texture2D)EditorGUILayout.ObjectField("Metallic",_metallicTex, typeof(Texture2D), false);
                }

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        var path = string.Empty;
                        _folderObject = EditorGUILayout.ObjectField("savePath", _folderObject, typeof(DefaultAsset), false);
                        if (check.changed)
                        {
                            path = AssetDatabase.GetAssetPath(_folderObject);
                            if (!string.IsNullOrEmpty(path))
                            {
                                _savePath = path;
                            }
                        }
                    }

                    _saveName = EditorGUILayout.TextField("saveName：", _saveName);
                }
            }
        }

        private void DrawButtonAndState()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _isRoughness = EditorGUILayout.Toggle("IsRoughness?", _isRoughness);
                if (GUILayout.Button("点击合并"))
                {
                    Combine();
                }

                if (GUILayout.Button("清空当前"))
                {
                    Clear();
                }
                
            }
        }

        private void Combine()
        {
            if (_roughnessOrSmoothnessTex == null && _metallicTex == null)
            {
                return;
            }

            if (_roughnessOrSmoothnessTex && _metallicTex)
            {
                if (_roughnessOrSmoothnessTex.width * _roughnessOrSmoothnessTex.height !=
                    _metallicTex.width * _metallicTex.height)
                {
                    EditorGUILayout.HelpBox("纹理大小不一致!", MessageType.Error);
                    return;
                }
            }
            
            int width, height;
            if (_roughnessOrSmoothnessTex != null)
            {
                width = _roughnessOrSmoothnessTex.width;
                height = _roughnessOrSmoothnessTex.height;
            }
            else if (_metallicTex != null)
            {
                width = _metallicTex.width;
                height = _metallicTex.height;
            }

            Texture2D createdTex = new Texture2D(_roughnessOrSmoothnessTex.width, _roughnessOrSmoothnessTex.height);
            // 使用RT拷贝以忽略图像Read/Write检查
            Texture2D readR = null, readM = null;
            if (_roughnessOrSmoothnessTex)
            {
                readR = GetReadTexture(_roughnessOrSmoothnessTex,_isRoughness);
            }

            if (_metallicTex)
            {
                readM = GetReadTexture(_metallicTex);
            }

            Color colorR,colorM;
            for (int i = 0; i < createdTex.width; i++)
            {
                for (int j = 0; j < createdTex.height; j++)
                {
                    colorR = readR == null ? Color.black : readR.GetPixel(i, j);
                    colorM = readM == null ? Color.black : readM.GetPixel(i, j);
                    createdTex.SetPixel(i,j,new Color(colorM.r, 1, 1, colorR.r));
                }
            }

            readR = null;
            readM = null;
            if (!Directory.Exists(_savePath))
            {
                Directory.CreateDirectory(_savePath);
            }
            

            string texSavePath = $"{_savePath}/{_saveName}.png";
            byte[] dataBytes = createdTex.EncodeToPNG();
            FileStream fileStream = File.Open(texSavePath,FileMode.OpenOrCreate);
            fileStream.Write(dataBytes,0,dataBytes.Length);
            fileStream.Close();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
           
            // over
            Clear();
        }

        private static Texture2D GetReadTexture(Texture2D src, bool toSmoothness = false)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(src,tempRT);
            Texture2D outTex = new Texture2D(src.width, src.height);
            outTex.ReadPixels(new Rect(0,0,tempRT.width,tempRT.height),0,0);
            outTex.Apply();
            RenderTexture.ReleaseTemporary(tempRT);

            if (toSmoothness)
            {
                for (int i = 0; i < outTex.width; i++)
                {
                    Color getColor;
                    for (int j = 0; j < outTex.height; j++)
                    {
                        // One Minus to change roughness => smoothness
                        getColor = outTex.GetPixel(i, j);
                        outTex.SetPixel(i,j,new Color(1 - getColor.r, 1 - getColor.g, 1-getColor.b));
                    }
                }
            }

            return outTex;
        }

        private void Clear()
        {
            _roughnessOrSmoothnessTex = null;
            _metallicTex = null;
            _isRoughness = false;
            //_savePath = Application.dataPath + "/CombineTextures";
            GC.Collect();
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            if (_instance == null)
            {
                return;
            }
            _instance.Repaint();
        }
    }

}
