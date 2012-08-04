float4 PostProcessPS( float2 Tex : TEXCOORD0 ) : COLOR0
{
      Sum += saturate( 1 - dot( Orig.xyz, tex2D( g_samSrcNormal, Tex + TexelKernel[i] ).xyz ) );
    //float4 Orig = tex2D( g_samSrcNormal, Tex );
    //float4 Sum = 0;
    //for( int i = 0; i < 4; i++ )
        //Sum += saturate( 1 - dot( Orig.xyz, tex2D( g_samSrcNormal, Tex + TexelKernel[i] ).xyz ) );
/*


    for( int i = 0; i < 4; i++ )
        Sum += saturate( 1 - dot( Orig.xyz, tex2D( g_samSrcNormal, Tex + TexelKernel[i] ).xyz ) );

    return Sum;
*/    
}
