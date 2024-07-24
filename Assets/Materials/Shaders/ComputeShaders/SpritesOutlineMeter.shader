Shader "Sprites/OutlineMeter"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        [Header(OutlineMeterProperties)]
        _MeterValue ("Meter Value", Range(0, 1)) = 1
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
                #pragma vertex SpriteVert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma multi_compile _ PIXELSNAP_ON
                #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
                #include "UnitySprites.cginc"

                float4 _MainTex_TexelSize;
                float _MeterValue;
                fixed4 _OutlineColor;

                fixed4 frag(v2f IN) : SV_Target
                {
                    fixed4 t = SampleSpriteTexture(IN.texcoord);
                    float y = IN.texcoord.y - fmod(IN.texcoord.y, _MainTex_TexelSize.y);

                    if (t.a == 0 && y < _MeterValue) {
                        fixed4 l = SampleSpriteTexture(IN.texcoord - fixed2(_MainTex_TexelSize.x, 0));
                        fixed4 r = SampleSpriteTexture(IN.texcoord + fixed2(_MainTex_TexelSize.x, 0));
                        fixed4 u = SampleSpriteTexture(IN.texcoord + fixed2(0, _MainTex_TexelSize.y));
                        fixed4 d = SampleSpriteTexture(IN.texcoord - fixed2(0, _MainTex_TexelSize.y));

                        if ((l.a + r.a + u.a + d.a) > 0) {
                            return _OutlineColor;
                        }
                    }

                    fixed4 c = t * IN.color;
                    c.rgb *= c.a;
                    return c;
                }
            ENDCG
        }
    }
}