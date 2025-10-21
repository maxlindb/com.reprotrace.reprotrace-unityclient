using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;


namespace MPerf {
    public class FastUILineRenderer : UIPrimitiveBase_MPerf {
        
        public Vector2[] Points;

        //public Sprite sprite;
        public float thickness = 3f;

        static UnityEngine.Profiling.CustomSampler OnPopulateMeshSampler {
            get {
                if (m_OnPopulateMeshSampler == null)
                    m_OnPopulateMeshSampler = UnityEngine.Profiling.CustomSampler.Create("FastUILineRenderer.OnPopulateMesh custom sampler");

                return m_OnPopulateMeshSampler;
            }
        }
        static UnityEngine.Profiling.CustomSampler m_OnPopulateMeshSampler = null;



        override protected void Start() {
            base.Start();
            if(rectangleTemplate)rectangleTemplate.gameObject.SetActive(false);
        }

        //List<RectTransform> touchedsTemp = new List<RectTransform>();

        public override Texture mainTexture {
            get {
                if (sprite == null)
                    return null;

                return sprite.texture;
            }
        }

        /*public override Material materialForRendering {
            get {
                if(mat == null) {
                    mat = base.materialForRendering;
                }
                return mat;
            }
        }
        Material mat = null;*/

        /*public override Material GetModifiedMaterial(Material baseMaterial) {
            mat = baseMaterial;
            return baseMaterial;
        }*/

        List<UIVertex[]> vertBunches = new List<UIVertex[]>();

        protected override void OnPopulateMesh(VertexHelper vh) {        

            vh.Clear();
            if (doDrawWithRects) return;
            if (Points == null) return;
            if (Points.Length < 2) return;

            OnPopulateMeshSampler.Begin(this);


            if (vertBunches.Count != (Points.Length - 1)) {
                vertBunches = new List<UIVertex[]>(Points.Length - 1);
                for (int i = 0; i < Points.Length - 1; i++) {
                    vertBunches.Add(new UIVertex[4]);
                    //vertBunches[i] = 
                    for (int j = 0; j < 4; j++) {
                        vertBunches[i][j] = new UIVertex();
					    vertBunches [i] [j].color = color;
                        //vertBunches[i][j].color = color; //new Color32(255, 255, 255, 255);
                        //vertBunches[i][j].color = new Color32(255, 255, 255, 255);

                        if (j == 0) vertBunches[i][j].uv0 = Vector2.zero;
                        if (j == 1) vertBunches[i][j].uv0 = new Vector2(0, 1);
                        if (j == 2) vertBunches[i][j].uv0 = new Vector2(0.5f, 1);
                        if (j == 3) vertBunches[i][j].uv0 = new Vector2(0.5f, 0);
                    }
                }
            }

            for (int i = 0; i < Points.Length - 1; i++) {
                var fromPoint = Points[i];
                var toPoint = Points[i + 1];

                Vector2 diffVec = toPoint - fromPoint;
                //Vector2 sivuttainVector = Quaternion.Euler(0, 0, -90) * diffVec.normalized;
			    var norm = diffVec.normalized;
			    Vector2 sivuttainVector = new Vector2 (norm.y, norm.x);

                vertBunches[i][1].position = fromPoint + (sivuttainVector * thickness * 0.5f);
                vertBunches[i][0].position = fromPoint - (sivuttainVector * thickness * 0.5f);
                vertBunches[i][2].position = toPoint + (sivuttainVector * thickness * 0.5f);
                vertBunches[i][3].position = toPoint - (sivuttainVector * thickness * 0.5f);

                vh.AddUIVertexQuad(vertBunches[i]);
            }

            OnPopulateMeshSampler.End();
        }

        public RectTransform rectangleTemplate;
        List<RectTransform> standbyRectangles = new List<RectTransform>();
        List<RectTransform> rectanglesInuse = new List<RectTransform>();

        bool doDrawWithRects = false;

        public void Redraw() {

            if(doDrawWithRects) {
                DrawWithRects();
            }
            else {
                while (rectanglesInuse.Count != 0) {
                    DisposeRectangle(rectanglesInuse[0]);
                }
                SetAllDirty();
            }
        }

        private void DrawWithRects() {
            while (rectanglesInuse.Count != 0) {
                DisposeRectangle(rectanglesInuse[0]);
            }

            //touchedsTemp.Clear();

            Vector3 fromPoint;
            Vector3 toPoint;
            for (int i = 0; i < Points.Length - 1; i++) {
                fromPoint = Points[i];
                toPoint = Points[i + 1];

                var rect = GetRectangle();

                var diff = toPoint - fromPoint;
                var degs = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                rect.eulerAngles = new Vector3(0f, 0f, degs);

                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Vector2.Distance(fromPoint, toPoint));

                rect.localPosition = fromPoint;

                //touchedsTemp.Add(rect);
            }

            //for (int n = rectanglesInuse.Count; n > 0; n--) {
            //    if (!touchedsTemp.Contains(rectanglesInuse[n])) {
            //        DisposeRectangle(rectanglesInuse[n]);
            //    }
            //}
        }

        RectTransform GetRectangle() {
            if(standbyRectangles.Count == 0) {
                CreateStandbyRectangle();
            }
            var toReturn = standbyRectangles.First();        
            standbyRectangles.Remove(toReturn);
            rectanglesInuse.Add(toReturn);
            //toReturn.gameObject.SetActive(true);
            return toReturn;
        }

        void DisposeRectangle(RectTransform inRect) {
            //inRect.gameObject.SetActive(false);
            rectanglesInuse.Remove(inRect);
            standbyRectangles.Add(inRect);

            inRect.localPosition = Vector3.up * 100000;
        }

        void CreateStandbyRectangle() {
            var createdRectangle = Instantiate<RectTransform>(rectangleTemplate);
            createdRectangle.SetParent(rectangleTemplate.parent);
            //createdRectangle.gameObject.SetActive(false);
            standbyRectangles.Add(createdRectangle);

            createdRectangle.gameObject.SetActive(true);
        }
    }
}
