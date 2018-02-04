using System;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class SimulationCamera : MonoBehaviour {
    [SerializeField, Range(1, 127)]
    int seg;

    public int SegX { get { return seg; } }
    public int SegY { get { return seg; } }

    public Texture PositionTexture { get { return rtResultTexture; } }

    public Mesh ClothMesh { get { return clothMesh; } }

    [SerializeField]
    float scaleWidth;
    public float ScaleWidth { get { return scaleWidth; } }

    [SerializeField]
    float scaleHeight;
    public float ScaleHeight { get { return scaleHeight; } }

    [SerializeField]
    SphereCollider sphere;

    [SerializeField]
    float gravity;

    [SerializeField]
    float deltaT;

    [SerializeField]
    float resistance;

    [SerializeField]
    float springConstraint;

    [SerializeField, Range(0, 32)]
    int iterations;

    [SerializeField]
    Material simulationMaterial;

    [SerializeField]
    SimulationCloth cloth;

    Texture2D initialPositionTexture;

    Mesh clothMesh;
    RenderTexture rtResultTexture;
    RenderTexture rtPositionTexture;
    RenderTexture rtPrevPositionTexture;

    Vector3 simulationWorldOffset;
    Vector3 simulationWorldScale;
    Matrix4x4 packMatrix;
    Matrix4x4 unpackMatrix;

    void UpdateClothMesh(Mesh mesh)
    {
        float widthHalf = ScaleWidth * 0.5f;
        float heightHalf = ScaleHeight * 0.5f;

        int w = SegX + 1;
        int h = SegY + 1;

        float segW = ScaleWidth / SegX;
        float segH = ScaleHeight / SegY;

        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();

        for (var iy = 0; iy < h; iy++)
        {
            var y = iy * segH - heightHalf;
            for (var ix = 0; ix < w; ix++)
            {
                var x = ix * segW - widthHalf;
                vertices.Add(new Vector3(x, -y, 0));

                var ux = (ix + 0.5f) / w;
                var uy = 1.0f - (iy + 0.5f) / h;
                uvs.Add(new Vector2(ux, uy));
            }
        }

        var indices = new int[w * h * 6];
        for (var iy = 0; iy < SegY; iy++)
        {
            for (var ix = 0; ix < SegX; ix++)
            {
                var i = (iy * SegX + ix) * 6;

                int a = ix + w * iy;
                int b = ix + w * (iy + 1);
                int c = (ix + 1) + w * (iy + 1);
                int d = (ix + 1) + w * iy;
                indices[i + 0] = a;
                indices[i + 1] = d;
                indices[i + 2] = b;

                indices[i + 3] = b;
                indices[i + 4] = d;
                indices[i + 5] = c;
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
    }

    Mesh CreateClothMesh()
    {
        var mesh = new Mesh();
        UpdateClothMesh(mesh);
        return mesh;
    }

    Texture2D CreatePositionTexture()
    {
        simulationWorldScale = new Vector3(1f, 1f, 1f);
        simulationWorldOffset = new Vector3(0f, 0f, 0f);
        unpackMatrix = Matrix4x4.Translate(simulationWorldOffset) * Matrix4x4.Scale(simulationWorldScale);
        packMatrix = unpackMatrix.inverse;

        float widthHalf = ScaleWidth * 0.5f;
        float heightHalf = ScaleHeight * 0.5f;

        int w = SegX + 1;
        int h = SegY + 1;

        float segW = ScaleWidth / SegX;
        float segH = ScaleHeight / SegY;

        var vertices = new Vector4[h * w];

        for (var iy = 0; iy < h; iy++)
        {
            var y = iy * segH - heightHalf;
            for (var ix = 0; ix < w; ix++)
            {
                var x = ix * segW - widthHalf;
                var flag = iy == 0 ? 0f : 1f;
                var v = new Vector3(x, -y, 0);

                v = packMatrix.MultiplyPoint(v);

                vertices[iy * w + ix] = new Vector4(v.x, v.y, v.z, flag);
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false, false);
        tex.filterMode = FilterMode.Bilinear;

        var colors = new Color[vertices.Length];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var vertex = vertices[(h - y - 1) * w + x];
                colors[y * w + x] = new Color(vertex.x, vertex.y, vertex.z, vertex.w);
            }
        }
        tex.SetPixels(colors);
        tex.Apply(false, false);

        return tex;
    }

    void ResetSimulation()
    {
        if(rtResultTexture != null)
        {
            Destroy(rtResultTexture);
        }
        rtResultTexture = new RenderTexture(SegX + 1, SegY + 1, 0, RenderTextureFormat.ARGBFloat);

        if (rtPositionTexture != null)
        {
            Destroy(rtPositionTexture);
        }
        rtPositionTexture = new RenderTexture(SegX + 1, SegY + 1, 0, RenderTextureFormat.ARGBFloat);

        if(rtPrevPositionTexture != null)
        {
            Destroy(rtPrevPositionTexture);
        }
        rtPrevPositionTexture = new RenderTexture(SegX + 1, SegY + 1, 0, RenderTextureFormat.ARGBFloat);

        if(clothMesh != null)
        {
            Destroy(clothMesh);
        }
        clothMesh = CreateClothMesh();

        if(initialPositionTexture != null)
        {
            Destroy(initialPositionTexture);
        }
        initialPositionTexture = CreatePositionTexture();

        cloth.UpdateSimulationCamera(this);
    }

    void Awake()
    {
        ResetSimulation();
    }

    void SwapRenderTexture(ref RenderTexture rt0, ref RenderTexture rt1)
    {
        RenderTexture tmp = rt1;
        rt1 = rt0;
        rt0 = tmp;
    }

    void OnPostRender()
    { 
        const int applyPass = 0;
        const int springPass = 1;
        const int finishPass = 2;

        float l0 = ScaleWidth / SegX;
        float l1 = ScaleHeight / SegY;
        float l2 = Mathf.Sqrt(l0 * l0 + l1 * l1);
        float l3 = 2.0f * ScaleWidth / SegX;
        float l4 = 2.0f * ScaleHeight / SegY;

        if(initialPositionTexture != null)
        {
            Graphics.Blit(initialPositionTexture, rtPrevPositionTexture);
            Graphics.Blit(initialPositionTexture, rtPositionTexture);
            Destroy(initialPositionTexture);
        }

        RenderTexture rt0 = RenderTexture.GetTemporary(SegX + 1, SegY + 1, 0, RenderTextureFormat.ARGBFloat);
        RenderTexture rt1 = RenderTexture.GetTemporary(SegX + 1, SegY + 1, 0, RenderTextureFormat.ARGBFloat);

        // 力を加えるフェーズ
        simulationMaterial.SetFloat("_Gravity", gravity);
        simulationMaterial.SetFloat("_Resistance", resistance);
        simulationMaterial.SetTexture("_PrevTex", rtPrevPositionTexture);
        simulationMaterial.SetFloat("_DeltaT", deltaT);

        Graphics.Blit(rtPositionTexture, rt1, simulationMaterial, applyPass);
        Graphics.Blit(rtPositionTexture, rtPrevPositionTexture);
        SwapRenderTexture(ref rt0, ref rt1);

        simulationMaterial.SetFloat("_DeltaT", 1.0f);
        simulationMaterial.SetFloat("_SpringConstraint", springConstraint);
        for (var i = 0; i < iterations; i++)
        {
            simulationMaterial.SetFloat("_SpringLength", l0);
            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 1.0f, 0.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4(-1.0f, 0.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetFloat("_SpringLength", l1);
            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 0.0f, 1.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 0.0f,-1.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetFloat("_SpringLength", l2);
            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 1.0f, 1.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4(-1.0f,-1.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 1.0f,-1.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4(-1.0f, 1.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetFloat("_SpringLength", l3);
            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 2.0f, 0.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4(-2.0f, 0.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetFloat("_SpringLength", l4);
            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 0.0f, 2.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);

            simulationMaterial.SetVector("_NeighborOffset", new Vector4( 0.0f,-2.0f));
            Graphics.Blit(rt0, rt1, simulationMaterial, springPass);
            SwapRenderTexture(ref rt0, ref rt1);
        }

        var scale = sphere.transform.localScale;

        var radius = Mathf.Max(Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)), Mathf.Abs(scale.z)) * sphere.radius;
        simulationMaterial.SetFloat("_SphereRadius", radius);

        var position = sphere.transform.position;
        simulationMaterial.SetVector("_SpherePosition", new Vector4(position.x, position.y, position.z, 1.0f));

        Graphics.Blit(rt0, rtPositionTexture, simulationMaterial, finishPass);
        Graphics.Blit(rtPositionTexture, rtResultTexture);

        RenderTexture.ReleaseTemporary(rt0);
        RenderTexture.ReleaseTemporary(rt1);
    }
}
