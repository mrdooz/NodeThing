#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d9.h>
#include <stdint.h>
#include <d3dx9effect.h>
#include <MMSystem.h>
#include "../TextureLib/TextureLib.hpp"
#include "shader_code.h"
//#include "test1_c.h"
#include <xmmintrin.h>

#ifdef _DEBUG
//#define NO_SIZE_OPT
#endif

#ifdef NO_SIZE_OPT
#define CHECK_HR(x) ASSERT(SUCCEEDED(x))
#else   
#define CHECK_HR(x) x
#endif

static void *funcPtrs[] = {
  &source_solid,
  &source_noise,
  &modifier_add,
  &modifier_sub,
  &modifier_max,
  &modifier_min,
  &modifier_mul,
};

extern int *gPerm;
extern Vector2 *gGrad;

extern Texture **gTextures;

extern "C" {
  int  _fltused = 0;

  float __declspec(naked) _CIcos()
  {
    _asm {
      fcos
      ret
    };
  };

  float __declspec(naked) _CIsin()
  {
    _asm {
      fsin
      ret
    };
  };

  float __declspec(naked) _CIsqrt()
  {
    _asm {
      fsqrt
      ret
    };
  };
};

void memcpy(void *dst, const void *src, int len) {
  _asm {
    mov edi, dst;
    mov esi, src;
    mov ecx, len;
    rep movsb;
  }
}

#define RAND_MAX_32 ((1U << 31) - 1)

int tRand()
{
  static int seed = 0x12345;
  return (seed = (seed * 214013 + 2531011) & RAND_MAX_32) >> 16;
}


typedef uint8_t uint8;

IDirect3DDevice9 *gDevice;
IDirect3D9 *gD3D;
ID3DXMesh *gMesh;
HWND gHwnd;

const int xRes = 800;
const int yRes = 600;

static D3DPRESENT_PARAMETERS gPresentParmas = {
  xRes, yRes, D3DFMT_A8R8G8B8, 0, D3DMULTISAMPLE_NONE,
  0, D3DSWAPEFFECT_DISCARD, 0, true, true,
  D3DFMT_D24S8, 0, 0, D3DPRESENT_INTERVAL_IMMEDIATE 
};

unsigned char tjong[122] = {
  0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, // width, height
  0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, // num textures, final texture
  0x50,0x8d,0x05,0x24,0x10,0x6c,0x5a,0x50,0x68,0x00,0x00,0xa0,0x40,0x68,0x00,0x00,
  0xa0,0x40,0x68,0x00,0x00,0x00,0x00,0xff,0x90,0x04,0x00,0x00,0x00,0x81,0xc4,0x0c,
  0x00,0x00,0x00,0x58,0x50,0x68,0xe1,0x7a,0x8c,0x41,0x68,0xe1,0x7a,0x8c,0x41,0x68,
  0x01,0x00,0x00,0x00,0xff,0x90,0x04,0x00,0x00,0x00,0x81,0xc4,0x0c,0x00,0x00,0x00,
  0x58,0x50,0x68,0x9a,0x99,0x39,0x40,0x68,0x00,0x00,0x00,0x00,0x68,0x66,0x66,0x26,
  0x40,0x68,0x01,0x00,0x00,0x00,0x68,0x02,0x00,0x00,0x00,0xff,0x90,0x08,0x00,0x00,
  0x00,0x81,0xc4,0x14,0x00,0x00,0x00,0x58,0x58,0xc3
};

unsigned char wat[161] = {
  0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, // width, height
  0x03, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // num textures, final texture
  0x50,0x8d,0x05,0x24,0x10,0x6c,0x5a,0x50,0x68,0x00,0x00,0xa0,0x40,0x68,0x00,0x00,
  0xa0,0x40,0x68,0x00,0x00,0x00,0x00,0xff,0x90,0x04,0x00,0x00,0x00,0x81,0xc4,0x0c,
  0x00,0x00,0x00,0x58,0x50,0x68,0xe1,0x7a,0x8c,0x41,0x68,0xe1,0x7a,0x8c,0x41,0x68,
  0x01,0x00,0x00,0x00,0xff,0x90,0x04,0x00,0x00,0x00,0x81,0xc4,0x0c,0x00,0x00,0x00,
  0x58,0x50,0x68,0x9a,0x99,0x39,0x40,0x68,0x00,0x00,0x00,0x00,0x68,0x66,0x66,0x26,
  0x40,0x68,0x01,0x00,0x00,0x00,0x68,0x02,0x00,0x00,0x00,0xff,0x90,0x08,0x00,0x00,
  0x00,0x81,0xc4,0x14,0x00,0x00,0x00,0x58,0x50,0x68,0x00,0x00,0x80,0x3f,0x68,0x00,
  0x00,0x00,0x00,0x68,0x00,0x00,0x80,0x3f,0x68,0x02,0x00,0x00,0x00,0x68,0x01,0x00,
  0x00,0x00,0xff,0x90,0x18,0x00,0x00,0x00,0x81,0xc4,0x14,0x00,0x00,0x00,0x58,0x58,
  0xc3
};


struct TextureHeader {
  int width, height;
  int numTextures, finalTexture;
#pragma warning(suppress: 4200)
  uint8 opcodes[0];
};

void *tAlloc(size_t size) {
  return GlobalAlloc(GMEM_FIXED, size);
}

void tFree(void *mem) {
  GlobalFree(mem);
}

template<typename T>
T *tNew(size_t count = 1) {
  return (T *)tAlloc(sizeof(T) * count);
}

void WINAPI fillTexture(D3DXVECTOR4* pOut, CONST D3DXVECTOR2* pTexCoord, CONST D3DXVECTOR2* pTexelSize, LPVOID pData) {
  Texture *t = gTextures[(int)pData];
  int w = t->width;
  int h = t->height;
  int x = (int)((pTexCoord->x - pTexelSize->x/2) * w);
  int y = (int)((pTexCoord->y - pTexelSize->y/2) * h);

  float *src = &t->data[4*(y*w+x)];
  _mm_storeu_ps((float *)pOut, _mm_min_ps(_mm_set_ps1(1), _mm_max_ps(_mm_set_ps1(0), _mm_load_ps(src))));
/*
  float *dst = (float *)pOut;
  for (int i = 0; i < 4; ++i) {
    *dst++ = max(0, min(1, *src));
    src++;
  }
*/
}

void createTexture(const void *raw, int len, IDirect3DTexture9 **texture) {

  TextureHeader *header = (TextureHeader *)raw;
  // patch opcodes to point to our function ptr array
  *(uint32 *)&header->opcodes[3] = (uint32)&funcPtrs[0];

  gTextures = tNew<Texture *>(header->numTextures);
  for (int i = 0; i < header->numTextures; ++i) {
    Texture *texture = tNew<Texture>();

    // Use VirtualAlloc to guarantee 16 byte alignment (actually page alignment)
    int size = 4*header->width*header->height*sizeof(float);
    texture->data = (float *)VirtualAlloc(NULL, size, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
    texture->width = header->width;
    texture->height = header->height;
    gTextures[i] = texture;
  }


  // Copy the opcodes to executable memory, and call that badboy!
  int opCodeLen = len - sizeof(TextureHeader);
  uint8 *mem = (uint8 *)VirtualAlloc(NULL, opCodeLen, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
  memcpy(mem, header->opcodes, opCodeLen);

  _asm {
    call [mem];
  }

  // create the d3d texture, and fill it with the destination texture
  gDevice->CreateTexture(header->width, header->height, 1, 0, D3DFMT_A8R8G8B8, D3DPOOL_MANAGED, texture, nullptr);

  D3DXFillTexture(*texture, fillTexture, (void *)header->finalTexture);

  VirtualFree(mem, 0, MEM_RELEASE);

  for (int i = 0; i < header->numTextures; ++i) {
    VirtualFree(gTextures[i]->data, 0, MEM_RELEASE);
    tFree(gTextures[i]);
  }

  tFree(gTextures);
}

unsigned int getFPUState(void)
{
  static unsigned int control;
  __asm fnstcw control;
  return control;
}

// set rounding mode to truncate
void setTruncate() {

  static short control_word;
  static short control_word2;
  __asm  {
      fstcw   control_word; // store fpu control word
      mov     dx, word ptr [control_word];
      or      dx,0x0c00;            // rounding: truncate
      mov     control_word2, dx;
      fldcw   control_word2;        // load modfied control word
  }
}

void createMesh() {

  CHECK_HR(D3DXCreateMeshFVF(12, 24, D3DXMESH_MANAGED | D3DXMESH_32BIT, D3DFVF_XYZ | D3DFVF_TEX1, gDevice, &gMesh));

#pragma pack(push, 1)
#pragma pack(pop)

  float *vb;
  int *ib;
  gMesh->LockVertexBuffer(0, (void **)&vb);
  gMesh->LockIndexBuffer(0, (void **)&ib);

  static const char vtx[] = {
    -1, +1, -1,
    +1, +1, -1,
    -1, -1, -1,
    +1, -1, -1,
    -1, +1, +1,
    +1, +1, +1,
    -1, -1, +1,
    +1, -1, +1
  };

  static const char quad_vtx[] = {
    0*3, 1*3, 2*3, 3*3,
    1*3, 5*3, 3*3, 7*3,
    5*3, 4*3, 7*3, 6*3,
    4*3, 0*3, 6*3, 2*3,
    4*3, 5*3, 0*3, 1*3,
    2*3, 3*3, 6*3, 7*3
  };

  static const char texture_coords[] = {
    0, 0,
    1, 0,
    0, 1,
    1, 1
  };

  static const char indices[] = {
    0, 1, 2,
    2, 1, 3
  };

  const char *q = quad_vtx;
  for (int i = 0; i < 6; ++i) {

    for (int j = 0; j < 6; ++j) {
      ib[i*6+j] = i*4 + indices[j];
    }

    for (int j = 0; j < 4; ++j) {
      *vb++ = vtx[*q+0];
      *vb++ = vtx[*q+1];
      *vb++ = vtx[*q+2];
      q++;
      *vb++ = texture_coords[j*2+0];
      *vb++ = texture_coords[j*2+1];
    }
  }

  gMesh->UnlockIndexBuffer();
  gMesh->UnlockVertexBuffer();
}

void __stdcall WinMainCRTStartup()
{
  gPerm = tNew<int>(512);
  for (int i = 0; i < 512; ++i)
    gPerm[i] = tRand() % 256;

  gGrad = tNew<Vector2>(cNumGradients);
  for (int i = 0; i < cNumGradients; ++i)
    gGrad[i] = normalize(Vector2(randf(-1.0f,1.0f), randf(-1.0f,1.0f)));


  gD3D = Direct3DCreate9( D3D_SDK_VERSION );
  if (!gD3D)
    ExitProcess(1);

  gHwnd = CreateWindow( "static",0,WS_POPUP|WS_VISIBLE,0,0,xRes, yRes,0,0,0,0);

  if (FAILED(gD3D->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, gHwnd, D3DCREATE_HARDWARE_VERTEXPROCESSING, &gPresentParmas, &gDevice)))
    ExitProcess(1);

  ID3DXEffect *effect;
  ID3DXBuffer *errors;

#ifdef NO_SIZE_OPT
  if (FAILED(D3DXCreateEffectFromFile(gDevice, "test1_c.fx", nullptr, nullptr, 0, nullptr, &effect, &errors))) {
    const char *err = (const char *)errors->GetBufferPointer();
    OutputDebugString(err);
    ExitProcess(1);
  }
#else
  //D3DXCreateEffect(gDevice, test1, sizeof(test1), nullptr, nullptr, 0, nullptr, &effect, &errors);
  D3DXCreateEffect(gDevice, test1_fx, sizeof(test1_fx), nullptr, nullptr, 0, nullptr, &effect, &errors);
#if _DEBUG
  if (errors) {
    const char *err = (const char *)errors->GetBufferPointer();
    OutputDebugString(err);
  }
#endif
  //const char *err = (const char *)errors->GetBufferPointer();

#endif

  createMesh();

  setTruncate();
  int state = getFPUState();
  IDirect3DTexture9 *tex;
  createTexture(wat, sizeof(wat), &tex);

#ifdef NO_SIZE_OPT
  D3DXHANDLE hWorld = effect->GetParameterByName(0, "World");
  D3DXHANDLE hViewProj = effect->GetParameterByName(0, "ViewProj");
  D3DXHANDLE hTexture = effect->GetParameterByName(0, "tex");
#else
  //D3DXHANDLE hWorld = effect->GetParameterByName(0, VAR_World);
  //D3DXHANDLE hViewProj = effect->GetParameterByName(0, VAR_ViewProj);
  //D3DXHANDLE hTexture = effect->GetParameterByName(0, VAR_tex);
  D3DXHANDLE hWorld = effect->GetParameterByName(0, VAR_WORLD);
  D3DXHANDLE hViewProj = effect->GetParameterByName(0, VAR_VIEWPROJ);
  D3DXHANDLE hTexture = effect->GetParameterByName(0, VAR_TEX);
#endif
  D3DXHANDLE hTechnique = effect->GetTechniqueByName("t0");

  D3DXMATRIX world, view, proj, viewProj;
  D3DXMatrixIdentity(&world);
  D3DXMatrixLookAtLH(&view, &D3DXVECTOR3(0,0,-4), &D3DXVECTOR3(0,0,0), &D3DXVECTOR3(0,1,0));
  D3DXMatrixPerspectiveFovLH(&proj, 45 * D3DX_PI / 180, 4/3.0f, 1, 1000);
  D3DXMatrixMultiply(&viewProj, &view,&proj);
  
  VirtualProtect(0, 0, 0, 0);

  DWORD start = timeGetTime();

  do {
    gDevice->Clear(0, NULL, D3DCLEAR_TARGET | D3DCLEAR_ZBUFFER | D3DCLEAR_STENCIL, 0x000000, 1.0f, 0);
    gDevice->BeginScene();

    effect->SetTechnique(hTechnique);
    UINT numPasses = 0;
    effect->Begin(&numPasses, 0);
    effect->BeginPass(0);

    DWORD cur = timeGetTime() - start;
    D3DXMatrixRotationYawPitchRoll(&world, cur / 1000.0f, cur / 2000.0f, cur / 3000.0f);

    effect->SetMatrix(hWorld, &world);
    effect->SetMatrix(hViewProj, &viewProj);
    effect->SetTexture(hTexture, tex);
    gMesh->DrawSubset(0);

    effect->EndPass();
    effect->End();

    gDevice->EndScene();
    gDevice->Present(0,0,0,0);

  } while (!GetAsyncKeyState(VK_ESCAPE));

  if (gDevice)
    gDevice->Release();
  gD3D->Release();
  DestroyWindow(gHwnd);

  ExitProcess(0);
}
