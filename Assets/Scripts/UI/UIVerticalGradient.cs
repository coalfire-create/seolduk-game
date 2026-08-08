using UnityEngine;
using UnityEngine.UI;

namespace Persuasion.UI
{
    /// <summary>
    /// 버텍스 컬러로 세로 그라데이션을 그린다.
    /// Unity UI에는 기본 그라데이션 셰이더가 없으므로 Graphic을 상속해 직접 메시를 만든다.
    /// 초상화 위에 깔아 하단 UI 가독성을 높이는 용도.
    /// </summary>
    [AddComponentMenu("Persuasion/UI Vertical Gradient")]
    public class UIVerticalGradient : Graphic
    {
        public Color topColor    = new Color(0f, 0f, 0f, 0f);
        public Color bottomColor = new Color(0f, 0f, 0f, 0.88f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vert(vh, r.xMin, r.yMin, bottomColor);
            Vert(vh, r.xMax, r.yMin, bottomColor);
            Vert(vh, r.xMax, r.yMax, topColor);
            Vert(vh, r.xMin, r.yMax, topColor);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }

        private static void Vert(VertexHelper vh, float x, float y, Color c)
        {
            var v = UIVertex.simpleVert;
            v.position = new Vector3(x, y, 0f);
            v.color = c;
            vh.AddVert(v);
        }
    }
}
