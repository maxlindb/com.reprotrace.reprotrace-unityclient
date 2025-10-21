using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.UI.Extensions;

namespace MPerf {
    public class FPSGraphDrawer : MonoBehaviour {

        public static FPSGraphDrawer current;

        public static bool frozen = false;


        //public UILineRenderer uiLine;
        public FastUILineRenderer uiLine;

        public RectTransform graphParent;
        public RectTransform graphArea;

        public float graphAreaWidth = 300f;
        public float graphAreaHeight = 100f;

        public Vector2 graphValueScale = new Vector2(5f, 0.1f);


        List<Vector2> timesAndDeltaTimes = new List<Vector2>();

        public int drawLastPointsCount;


        //float maxDelta = 0.1f; //100ms     

        void Awake() {
            current = this;
        }

        public void AddPoint(float inDeltaTime) {
            timesAndDeltaTimes.Add(new Vector2(Time.realtimeSinceStartup, inDeltaTime));
        }

        Vector2 lastSetSize = Vector2.zero;

        public void Refresh() {
            if (frozen) return;

            if (!Mathf.Approximately(lastSetSize.x,graphAreaWidth) || !Mathf.Approximately(lastSetSize.y, graphAreaHeight)) {
                graphParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, graphAreaWidth);
                graphParent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, graphAreaHeight);

                lastSetSize.x = graphAreaWidth;
                lastSetSize.y = graphAreaHeight;
            }


            if (timesAndDeltaTimes.Count < drawLastPointsCount) {
                for (int i = 0; i < drawLastPointsCount; i++) {
                    timesAndDeltaTimes.Add(Vector2.zero);
                }
            }

            if(uiLine.Points.Length != drawLastPointsCount) {
                uiLine.Points = new Vector2[drawLastPointsCount];
            }

            for (int i = 0; i < drawLastPointsCount; i++) {
                int indexInList = ((timesAndDeltaTimes.Count - 1) - drawLastPointsCount) + i;
                var valFromList = timesAndDeltaTimes[indexInList];
                valFromList.x = ((float)i / (float)drawLastPointsCount) * graphAreaWidth;
                var point = TransformTimeAndDeltaToRelevantSpace(valFromList);
                uiLine.Points[i] = point;            
            }
            //uiLine.ForceRefresh();
            uiLine.Redraw();
        }
    
        private Vector2 TransformTimeAndDeltaToRelevantSpace(Vector2 inTimeAndDeltaTime) {
            var relPos = new Vector2();
            relPos = Vector2.Scale(inTimeAndDeltaTime, graphValueScale);

            return relPos;
            //return (Vector2)graphArea.position + relPos;
        }
    }
}
