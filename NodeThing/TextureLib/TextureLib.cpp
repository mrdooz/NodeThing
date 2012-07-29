// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <stdint.h>

typedef uint32_t uint32;
typedef uint8_t uint8;

HANDLE gHeapHandle;

extern "C"
{
  struct Texture {
    char *data;
    int width;
    int height;
  };

  Texture **g_textures = nullptr;

  void source_solid(int dst_texture, uint32 color) {
/*
    Texture *texture = g_textures[dst_texture];
    uint32 *p = (uint32 *)texture->data;
    for (int i = 0; i < texture->height; ++i) {
      for (int j = 0; j  < texture->width; ++j) {
        *p++ = color;
      }
    }
*/
  }

  void source_noise(int dst_texture) {
  }

  void modifier_add(int dst_texture) {

  }

  void modifier_sub(int dst_texture) {

  }

  void modifier_max(int dst_texture) {

  }

  void modifier_min(int dst_texture) {

  }

  static void *funcPtrs[] = {
    &source_solid,
    &source_noise,
    &source_solid,
    &modifier_add,
    &modifier_sub,
    &modifier_max,
    &modifier_min
  };

/*
  void tjong() {


    _asm {
      push  eax;
      lea   eax, xx
      push  10;
      push  20;
      call  [eax + 8];
      add   esp, 8;
      pop   eax;
    }

  }
*/
  float bong(float a, float b, float c)
  {
    return a + b + c;
  }

  __declspec(dllexport) void RenderTexture(HWND hwnd, int width, int height, int num_textures, const char *name, int opCodeLen, const char *opCodes) {

    gHeapHandle = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 32*1024, 128*1024);
    uint8 *mem = (uint8 *)HeapAlloc(gHeapHandle, HEAP_ZERO_MEMORY, opCodeLen);
    // push eax
    mem[0] = 0x50;
    // lea eax, funcPtrs
    mem[1] = 0x8d;
    mem[2] = 0x05;
    *(uint32 *)&mem[3] = (uint32)&funcPtrs[0];

    memcpy(mem + 7, opCodes, opCodeLen);

    // pop eax
    mem[7 + opCodeLen + 0] = 0x58;

    // ret
    mem[7 + opCodeLen + 1] = 0xc3;

    _asm {
      call [mem];
    }

    //tjong();
    HDC dc = GetDC(hwnd);

    BITMAPINFO bmi;
    ZeroMemory(&bmi, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);

    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;
    bmi.bmiHeader.biWidth = width;
    bmi.bmiHeader.biHeight = -height;

    void *bits;
    HBITMAP bm = CreateDIBSection(dc, &bmi, DIB_RGB_COLORS, &bits, NULL, 0);
    HDC src_dc = CreateCompatibleDC(dc);
    SelectObject(src_dc, bm);
    BitBlt(dc, 0, 0, 512, 512, src_dc, 0, 0, SRCCOPY);

    DeleteDC(src_dc);
    DeleteObject(bm);

    ReleaseDC(hwnd, dc);

  }



};

