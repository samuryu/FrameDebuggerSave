﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using UTJ.FrameDebugSave;
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif


namespace UTJ.FrameDebugSave.UI
{

    public class FrameEventDebugSaveWindow : EditorWindow
    {
        private static readonly string[] shaderParamTypes = { "Texture", "Float", "Vector", "Matrix" };

        private FrameInfoCrawler.CaptureFlag captureFlag;
        private FameDebugSave frameDebugSave = new FameDebugSave();

        private VisualTreeAsset shaderParamTemplate;
        private VisualTreeAsset namedValueParamTemplate;
        private TextureLoader textureLoader;

#if !UNITY_2019_1_OR_NEWER && !UNITY_2019_OR_NEWER
        private VisualElement rootVisualElement
        {
            get
            {
                return this.GetRootVisualContainer();
            }
        }

        private float lastHeight = -1.0f;
        private void SetupScrollViewHeight()
        {
            if (lastHeight == this.position.height)
            {
                return;
            }

            var captureItems = this.rootVisualElement.Q<ScrollView>("CaptureItems");
            captureItems.style.height = this.position.height - 20;

            var detailScroll = this.rootVisualElement.Q<ScrollView>("DetailScroll");
            detailScroll.style.height = this.position.height - 200;

            var eventListView = this.rootVisualElement.Q<FrameEventListView>();
            if (eventListView != null)
            {
                eventListView.SetupScrollViewHeight(this.position.height);
            }
            lastHeight = this.position.height;
        }
        private void Update()
        {
            SetupScrollViewHeight();
        }
#endif


        [MenuItem("Tools/FrameDebugSave")]
        public static void Create()
        {
            EditorWindow.GetWindow<FrameEventDebugSaveWindow>();
        }
        private void OnEnable()
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            string windowLayoutPath = "Packages/com.utj.framedebugger2csv/Editor/UI/UXML/FrameEventsViewer.uxml";
            string shaderParamPath = "Packages/com.utj.framedebugger2csv/Editor/UI/UXML/ShaderParameterTemplate.uxml";
            string namedValuePath = "Packages/com.utj.framedebugger2csv/Editor/UI/UXML/NamedValueTemplate.uxml";
#else
            string windowLayoutPath = "Packages/com.utj.framedebugger2csv/Editor/UI/UXML2018/FrameEventsViewer.uxml";
            string shaderParamPath = "Packages/com.utj.framedebugger2csv/Editor/UI/UXML2018/ShaderParameterTemplate.uxml";
            string namedValuePath = "Packages/com.utj.framedebugger2csv/Editor/UI/UXML2018/NamedValueTemplate.uxml";
#endif


            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(windowLayoutPath);
            var visualElement = CloneTree(tree);

            this.rootVisualElement.Add(visualElement);
            this.textureLoader = new TextureLoader();
            this.shaderParamTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(shaderParamPath);

            this.namedValueParamTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(namedValuePath);

            this.RefreshCaptures();
        }

        private void OnFrameEventChange(FrameDebugDumpInfo.FrameEventInfo evtInfo)
        {
            var screenShotTex = textureLoader.LoadTexture(evtInfo.screenshot);
            this.rootVisualElement.Q<VisualElement>("ScreenShot").style.backgroundImage = screenShotTex;

            ChangeRenderTargetInfo(evtInfo);
            ChangeRenderInfo(evtInfo);
            ChangeShaderInfo(evtInfo);
            ChangeShaderParam(evtInfo);
        }


        private void ChangeRenderTargetInfo(FrameDebugDumpInfo.FrameEventInfo evtInfo)
        {
            var parentElement = this.rootVisualElement.Q<Foldout>("RenderTargetInfo");
            RemoveAllChildren(parentElement);
            var rtInfo = evtInfo.renderTarget;
            parentElement.Add(CreateNameValueElement("RT Name", rtInfo.rtName));
            parentElement.Add(CreateNameValueElement("Size", rtInfo.rtWidth + " X " + rtInfo.rtHeight));
            parentElement.Add(CreateNameValueElement("Count", rtInfo.rtCount.ToString()));
            parentElement.Add(CreateNameValueElement("Depth", (rtInfo.rtHasDepthTexture >0).ToString()));

        }

        private void ChangeRenderInfo(FrameDebugDumpInfo.FrameEventInfo evtInfo)
        {
            var parentElement = this.rootVisualElement.Q<Foldout>("RenderingInfo");
            RemoveAllChildren(parentElement);
            var renderInfo = evtInfo.rendering;


            parentElement.Add(CreateNameValueElement("BreakReson", renderInfo.batchBreakCauseStr));
            parentElement.Add(CreateNameValueElement("Vertex", renderInfo.vertexCount.ToString()));
            parentElement.Add(CreateNameValueElement("Index", renderInfo.indexCount.ToString()));
            parentElement.Add(CreateNameValueElement("DrawCount", renderInfo.drawCallCount.ToString()));
            parentElement.Add(CreateNameValueElement("GameObject", renderInfo.gameobject));
            parentElement.Add(CreateNameValueElement("ComponentId", renderInfo.componentInstanceID.ToString()));
            parentElement.Add(CreateNameValueElement("MeshInstanceId", renderInfo.meshInstanceID.ToString()));
            parentElement.Add(CreateNameValueElement("InstanceCount", renderInfo.instanceCount.ToString()));
            parentElement.Add(CreateNameValueElement("MeshSubset", renderInfo.meshSubset.ToString()));


        }
        private void ChangeShaderInfo(FrameDebugDumpInfo.FrameEventInfo evtInfo)
        {
            var parentElement = this.rootVisualElement.Q<Foldout>("ShaderInfo");
            var shaderInfo = evtInfo.shaderInfo;
            RemoveAllChildren(parentElement);

            parentElement.Add(CreateNameValueElement("ShaderName", shaderInfo.shaderName));
            parentElement.Add(CreateNameValueElement("SubShader", shaderInfo.subShaderIndex.ToString()));
            parentElement.Add(CreateNameValueElement("Keyword", shaderInfo.shaderKeywords));
            parentElement.Add(CreateNameValueElement("Pass", shaderInfo.passName + "(" + shaderInfo.shaderPassIndex + ")"));
            parentElement.Add(CreateNameValueElement("LightMode", shaderInfo.passLightMode));
        }

        private VisualElement CreateNameValueElement(string name,string val)
        {
            var tree = CloneTree(namedValueParamTemplate);
            tree.Q<Label>("name").text = name;
            tree.Q<Label>("val").text = val;
            return tree;
        }

        private void ChangeShaderParam(FrameDebugDumpInfo.FrameEventInfo evtInfo)
        {
            var shaderParams = evtInfo.shaderInfo.shaderParams;
            var parentElement = this.rootVisualElement.Q<Foldout>("ShaderParameters");
            RemoveAllChildren(parentElement);

            if (shaderParams.textures != null)
            {
                foreach (var textureParam in shaderParams.textures)
                {
                    var elem = CreateShaderParamVE(textureParam);
                    parentElement.Add(elem);
                }
            }
            if (shaderParams.floats != null)
            {
                foreach (var floatParam in shaderParams.floats)
                {
                    var elem = CreateShaderParamVE(floatParam);
                    parentElement.Add(elem);
                }
            }
            if (shaderParams.vectors != null)
            {
                foreach (var vectorParam in shaderParams.vectors)
                {
                    var elem = CreateShaderParamVE(vectorParam);
                    parentElement.Add(elem);
                }
            }
            if (shaderParams.matricies != null)
            {
                foreach (var matrixParam in shaderParams.matricies)
                {
                    var elem = CreateShaderParamVE(matrixParam);
                    parentElement.Add(elem);
                }
            }

        }

        private VisualElement CreateShaderParamVE(FrameDebugDumpInfo.TextureParamInfo textureParam)
        {
            var element = CloneTree(this.shaderParamTemplate);
            var valElem = InitShaderParamValueElement(element, "Texture",textureParam.name);
            var tex = textureLoader.LoadTexture(textureParam.saved);
            var texBody = valElem.Q<VisualElement>("texbody");
            var info = valElem.Q<Label>("val");
            if (tex != null)
            {
                texBody.style.backgroundImage = tex;
            }
            else
            {
                texBody.parent.Remove(texBody);
            }
            info.text = textureParam.textureName + "\n" +
                textureParam.originWidth + " X " + textureParam.originHeight + "\n" +
                textureParam.originFormat + "\n" +
                "mipcount:" + textureParam.originMipCount;
            return element;
        }

        private VisualElement CreateShaderParamVE(FrameDebugDumpInfo.FloatParamInfo floatParam)
        {
            var element = CloneTree(this.shaderParamTemplate);
            var valElem = InitShaderParamValueElement(element, "Float", floatParam.name);
            valElem.Q<Label>("val").text = floatParam.val.ToString();
            return element;
        }
        private VisualElement CreateShaderParamVE(FrameDebugDumpInfo.VectorParamInfo vectorParam)
        {
            var element = CloneTree(this.shaderParamTemplate);
            var valElem = InitShaderParamValueElement(element, "Vector", vectorParam.name);
            for( int i = 0; i < 4; ++i)
            {
                valElem.Q<Label>("val-"+i).text = vectorParam.val[i].ToString();
            }
            return element;
        }
        private VisualElement CreateShaderParamVE(FrameDebugDumpInfo.MatrixParamInfo matrixParam)
        {
            var element = CloneTree(this.shaderParamTemplate);
            var valElem = InitShaderParamValueElement(element, "Matrix", matrixParam.name);
            for (int i = 0; i < 16; ++i)
            {
                valElem.Q<Label>("val-" + i).text = matrixParam.val[i].ToString();
            }
            return element;
        }

        private VisualElement InitShaderParamValueElement(VisualElement elem,string typeStr,string name)
        {
            VisualElement retNode = null;
            elem.Q<Label>("name").text = name;
            foreach( var type in shaderParamTypes)
            {
                var currentVE = elem.Q<VisualElement>(type);
                if ( type == typeStr)
                {
                    retNode = currentVE;
                }
                else
                {
                    currentVE.parent.Remove(currentVE);
                }
            }
            return retNode;
        }


        private void RemoveAllChildren(VisualElement elem)
        {
            for( int i = elem.childCount -1; i>=0 ; --i)
            {
                elem.RemoveAt(i);
            }
        }

        private void LoadCaptureFile(string file)
        {
            string detail = file;
            this.textureLoader.SetDirectory(System.IO.Path.GetDirectoryName(file));

            var t = UTJ.FrameDebugSave.FrameDebugDumpInfo.LoadFromFile(detail);
            var eventListView = this.rootVisualElement.Q<FrameEventListView>();
            eventListView.OnChangedFrame = this.OnFrameEventChange;
            eventListView.SetItems(t.events);
        }

        private void RefreshCaptures()
        {
            var captures = GetCaptures();
            var scrollView = this.rootVisualElement.Q<ScrollView>("CaptureItems");
            scrollView.Clear();
            scrollView.Add(new IMGUIContainer(OnGUINewCapture));

            foreach (var capture in captures) {
                var btn = new Button();
                btn.text = capture;
                btn.clickable.clicked += ()=>{
                    LoadCaptureFile(FrameInfoCrawler.RootSaveDir + "/" + capture + "/detail.json");
                };
                scrollView.Add(btn);
             }
        }

        private void OnGUINewCapture()
        {
            captureFlag = (FrameInfoCrawler.CaptureFlag)EditorGUILayout.EnumFlagsField( captureFlag);
            if (GUILayout.Button("new Capture"))
            {
                frameDebugSave.Execute(this.captureFlag,this.RefreshCaptures);
            }
            GUILayout.Label("");

        }

        private List<string> GetCaptures()
        {
            List<string> captures = new List<string>();
            if( !System.IO.Directory.Exists(FrameInfoCrawler.RootSaveDir))
            {
                return captures;
            }
            var dirs = System.IO.Directory.GetDirectories(FrameInfoCrawler.RootSaveDir);
            foreach (var dir in dirs){
                captures.Add(dir.Replace(FrameInfoCrawler.RootSaveDir, ""));
            }
            return captures;
        }

        private static VisualElement CloneTree(VisualTreeAsset asset)
        {
#if UNITY_2019_1_OR_NEWER || UNITY_2019_OR_NEWER
            return asset.CloneTree();
#else
            return asset.CloneTree(null);

#endif
        }
    }

}
