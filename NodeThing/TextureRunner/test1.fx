matrix World;
matrix ViewProj;

struct VsInput {
  vector pos : POSITION;
  float2 tex : TEXCOORD;
};

struct VsOutput {
  vector pos : POSITION;
  float2 tex : TEXCOORD;
};

VsOutput vsMain(VsInput input) {
  VsOutput output;
  matrix mtx = mul(World, ViewProj);
  output.pos = mul(input.pos, mtx);
  output.tex = input.tex;
  return output;
}

Texture tex;

sampler s0 = sampler_state {
  Texture = (tex);
};

float4 psMain(VsOutput input) : COLOR0 {
  return tex2D(s0, input.tex);
}

technique t0 {
  pass p0 {
    vertexshader = compile vs_3_0 vsMain();
    pixelshader = compile ps_3_0 psMain();
  }
}
