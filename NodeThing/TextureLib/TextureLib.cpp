// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <stdint.h>

typedef uint32_t uint32;

extern "C"
{
  void tjong();

  float bong(float a, float b, float c)
  {
    return a + b + c;
  }

  __declspec(dllexport) void FillHwnd(HWND hwnd, int width, int height) {

    float xx = bong(10.0f, 20.0f, 30.0f);
    tjong();
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

  //_nodeNames.AddRange(new [] { "Solid", "Noise"});
  //_nodeNames.AddRange(new [] {"Add", "Sub", "Max", "Min"});

  struct Texture {
    char *data;
    int width;
    int height;
  };

  Texture **g_textures = nullptr;

  __declspec(naked) void source_solid(int dst_texture, uint32 color) {
    Texture *texture;
    texture = g_textures[dst_texture];
    uint32 *p;
    p = (uint32 *)texture->data;
    for (int i = 0; i < texture->height; ++i) {
      for (int j = 0; j  < texture->width; ++j) {
        *p++ = color;
      }
    }

  }

  __declspec(naked) void source_noise(int dst_texture) {
  }

  __declspec(naked) void modifier_add() {

  }

  __declspec(naked) void modifier_sub() {

  }

  __declspec(naked) void modifier_max() {

  }

  __declspec(naked) void modifier_min() {

  }

  void tjong() {
    void *xx[] = {
      &source_solid,
      &source_noise,
      &modifier_add,
      &modifier_sub,
      &modifier_max,
      &modifier_min
    };

    _asm {
      mov	eax, xx;
      call	eax

    }
  }

};

