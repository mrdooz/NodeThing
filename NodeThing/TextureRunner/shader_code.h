/* File generated with Shader Minifier 1.1.1
 * http://www.ctrl-alt-test.fr
 */
#ifndef SHADER_CODE_H_
# define SHADER_CODE_H_
# define VAR_VIEWPROJ "x"
# define VAR_WORLD "V"
# define VAR_TEX "p"
# define F_PSMAIN "l"

const char test1_fx[] = ""
 "matrix V,x;struct VsInput{vector pos:POSITION;float2 tex:TEXCOORD;};struct VsOutput{vector pos:POSITION;float2 tex:TEXCOORD;};"
 "VsOutput t(VsInput t)"
 "{"
   "VsOutput p;"
   "matrix l=mul(V,x);"
   "p.pos=mul(t.pos,l);"
   "p.tex=t.tex;"
   "return p;"
 "}"
 "Texture p;"
 "sampler s0=sampler_state{"
  "Texture=(p);"
  "};"
 "float4 l(VsOutput t):COLOR0"
 "{"
   "return tex2D(s0,t.tex);"
 "}"
"technique t0{"
  "pass p0{"
    "vertexshader=compile vs_3_0 t();"
    "pixelshader=compile ps_3_0 l();"
  "}"
"}";  


#endif // SHADER_CODE_H_
