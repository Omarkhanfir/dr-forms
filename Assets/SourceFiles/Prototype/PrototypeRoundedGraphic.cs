using UnityEngine;
using UnityEngine.UI;

namespace DrForms.Prototype
{
    // One tessellated shape owns fill, boundary and edge antialiasing. Corners
    // are measured in canvas units, never stretched from a tiny sliced texture.
    public sealed class PrototypeRoundedGraphic : MaskableGraphic
    {
        private float radius = 12, borderWidth = 1;
        private Color borderColor = new Color(.44f,.42f,.40f);
        public void Shape(float cornerRadius, float width, Color border)
        {
            radius=cornerRadius; borderWidth=width; borderColor=border; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect=GetPixelAdjustedRect();
            float r=Mathf.Clamp(radius,0,Mathf.Min(rect.width,rect.height)*.5f);
            float b=Mathf.Clamp(borderWidth,0,Mathf.Min(rect.width,rect.height)*.5f);
            float aa=1f/Mathf.Max(.01f,canvas!=null?canvas.scaleFactor:1);
            const int steps=12, count=4*(steps+1);
            mesh.AddVert(rect.center,color,Vector2.zero);
            for(int ring=0;ring<4;ring++)
            {
                float inset=ring<2?b:ring==2?0:-aa;
                var bounds=new Rect(rect.x+inset,rect.y+inset,rect.width-2*inset,rect.height-2*inset);
                float arc=Mathf.Max(0,r-inset);
                Color tint=ring==0?color:borderColor; if(ring==3)tint.a=0;
                for(int corner=0;corner<4;corner++)for(int step=0;step<=steps;step++)
                {
                    bool right=corner==0||corner==3, top=corner<2;
                    var center=new Vector2(right?bounds.xMax-arc:bounds.xMin+arc,top?bounds.yMax-arc:bounds.yMin+arc);
                    float angle=(corner*90+step*90f/steps)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*arc,tint,Vector2.zero);
                }
            }
            for(int i=0;i<count;i++)
            {
                int next=(i+1)%count;
                mesh.AddTriangle(0,1+i,1+next);
                for(int ring=1;ring<3;ring++)
                {
                    int inner=1+ring*count,outer=inner+count;
                    mesh.AddTriangle(inner+i,outer+i,outer+next);
                    mesh.AddTriangle(inner+i,outer+next,inner+next);
                }
            }
        }
    }
}
