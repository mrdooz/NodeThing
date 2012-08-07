#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d9.h>
#include <stdint.h>
#include <d3dx9effect.h>
#include <MMSystem.h>
#include "../TextureLib/TextureLib.hpp"
//#include "shader_code.h"
#include "test1_fx.h"
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
  &source_circles,
  &source_random,
  &source_sinwaves,
  &source_plasma,
  &modifier_map_distort,
};

extern int gPerm[];
extern Vector2 gGrad[];

extern Texture **gTextures;

extern "C" {
  int  _fltused = 0;

  // In release, we use intrinsics, so these guys are only needed in debug
#ifdef _DEBUG
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
#endif

};

void memcpy(void *dst, const void *src, int len) {
  _asm {
    mov edi, dst;
    mov esi, src;
    mov ecx, len;
    rep movsb;
  }
}


ID3DXConstantTable *gConstantTable;
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

unsigned char funky[195] = {
  0x00, 0x02, 0x00, 0x02, // width, height
  0x04, 0x01, // num textures, final texture
  0x50,0x8d,0x05,0x38,0x80,0x48,0x0f,0x50,0x68,0x26,0x33,0x04,0x00,0x6a,0x03,0x6a,
  0x05,0x6a,0x00,0x68,0xd7,0xa3,0x70,0x3f,0x6a,0x00,0xff,0x90,0x28,0x00,0x00,0x00,
  0x81,0xc4,0x18,0x00,0x00,0x00,0x58,0x50,0x68,0x96,0x43,0x4c,0x42,0x68,0x39,0xb4,
  0x90,0x41,0x68,0x3c,0xef,0x30,0x40,0x68,0x00,0x00,0x00,0x00,0x68,0xb8,0x1e,0x85,
  0x3e,0x68,0xe1,0x7a,0x14,0x3f,0x6a,0x02,0x6a,0x00,0x68,0x00,0x00,0x00,0x3f,0x6a,
  0x01,0xff,0x90,0x24,0x00,0x00,0x00,0x81,0xc4,0x28,0x00,0x00,0x00,0x58,0x50,0x68,
  0x66,0x66,0x16,0x41,0x68,0x66,0x66,0x16,0x41,0x68,0x66,0x66,0xa6,0x40,0x68,0x00,
  0x00,0xc0,0x40,0x6a,0x02,0xff,0x90,0x04,0x00,0x00,0x00,0x81,0xc4,0x14,0x00,0x00,
  0x00,0x58,0x50,0x6a,0x00,0x68,0xf3,0xb0,0x50,0x3d,0x6a,0x01,0x6a,0x01,0x6a,0x03,
  0xff,0x90,0x2c,0x00,0x00,0x00,0x81,0xc4,0x14,0x00,0x00,0x00,0x58,0x50,0x68,0xcd,
  0xcc,0xcc,0x3f,0x6a,0x00,0x68,0x00,0x00,0x60,0x40,0x6a,0x03,0x6a,0x01,0xff,0x90,
  0x18,0x00,0x00,0x00,0x81,0xc4,0x14,0x00,0x00,0x00,0x58,0x58,0xc3
};

struct TextureHeader {
  uint16 width, height;
  uint8 numTextures, finalTexture;
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
  for (int i = 0; i < 512; ++i)
    gPerm[i] = tRand() % 256;

  for (int i = 0; i < cNumGradients; ++i)
    gGrad[i] = normalize(Vector2(randf(-1.0f,1.0f), randf(-1.0f,1.0f)));


  gD3D = Direct3DCreate9( D3D_SDK_VERSION );
  gHwnd = CreateWindow("static",0,WS_POPUP|WS_VISIBLE,0,0,xRes, yRes,0,0,0,0);
  gD3D->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, gHwnd, D3DCREATE_HARDWARE_VERTEXPROCESSING, &gPresentParmas, &gDevice);

  ID3DXBuffer *errors;
  ID3DXBuffer *shader;
  D3DXCompileShader(test1_fx, sizeof(test1_fx), NULL, NULL, "vsMain", "vs_3_0", 0, &shader, &errors, &gConstantTable);
  IDirect3DVertexShader9 *vs;
  gDevice->CreateVertexShader((DWORD *)shader->GetBufferPointer(), &vs);

  D3DXCompileShader(test1_fx, sizeof(test1_fx), NULL, NULL, "psMain", "ps_3_0", 0, &shader, &errors, NULL);
  IDirect3DPixelShader9 *ps;
  gDevice->CreatePixelShader((DWORD *)shader->GetBufferPointer(), &ps);

  createMesh();

  setTruncate();
  IDirect3DTexture9 *tex;
  createTexture(funky, sizeof(funky), &tex);

  D3DXMATRIX world, view, proj, viewProj;
  D3DXMatrixIdentity(&world);
  D3DXMatrixLookAtLH(&view, &D3DXVECTOR3(0,0,-4), &D3DXVECTOR3(0,0,0), &D3DXVECTOR3(0,1,0));
  D3DXMatrixPerspectiveFovLH(&proj, 45 * D3DX_PI / 180, 4/3.0f, 1, 1000);
  D3DXMatrixMultiply(&viewProj, &view,&proj);
  
  DWORD start = timeGetTime();

  do {
    gDevice->Clear(0, NULL, D3DCLEAR_TARGET | D3DCLEAR_ZBUFFER | D3DCLEAR_STENCIL, 0x000000, 1.0f, 0);
    gDevice->BeginScene();

    gDevice->SetVertexShader(vs);
    gDevice->SetPixelShader(ps);

    DWORD cur = timeGetTime() - start;
    D3DXMatrixRotationYawPitchRoll(&world, cur / 1000.0f, cur / 2000.0f, cur / 3000.0f);

    gConstantTable->SetMatrix(gDevice, VAR_WORLD, &world);
    gConstantTable->SetMatrix(gDevice, VAR_VIEWPROJ, &viewProj);
    gDevice->SetTexture(0, tex);

    gMesh->DrawSubset(0);

    gDevice->EndScene();
    gDevice->Present(0,0,0,0);

  } while (!GetAsyncKeyState(VK_ESCAPE));

  gDevice->Release();
  gD3D->Release();
  DestroyWindow(gHwnd);

  ExitProcess(0);
}
