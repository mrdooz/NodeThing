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
