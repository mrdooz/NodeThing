// TextureLib.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include <stdint.h>
#include <assert.h>

typedef uint32_t uint32;
typedef uint8_t uint8;


struct Texture {
  float *data;  // RGBA
  int width;
  int height;
};

Texture **gTextures = nullptr;
HANDLE gHeapHandle = INVALID_HANDLE_VALUE;

extern "C"
{
  void source_solid(int dstTexture, uint32 color) {
    Texture *texture = gTextures[dstTexture];
    float *p = (float *)texture->data;
    float r = ((color >> 24) & 0xff)/ 255.0f;
    float g = ((color >> 16) & 0xff) / 255.0f;
    float b = ((color >>  8) & 0xff) / 255.0f;
    float a = ((color >>  0) & 0xff) / 255.0f;

    for (int i = 0; i < texture->height; ++i) {
      for (int j = 0; j  < texture->width; ++j) {
        p[0] = r;
        p[1] = g;
        p[2] = b;
        p[3] = a;
        p += 4;
      }
    }
  }

  void source_noise(int dstTexture, int seed) {
  }

  void modifier_add(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {

    Texture *dstTexture = gTextures[dstTextureIdx];
    float *dst = dstTexture->data;

    Texture *srcTexture1 = gTextures[srcTexture1Idx];
    float *src1 = srcTexture1->data;

    Texture *srcTexture2 = gTextures[srcTexture2Idx];
    float *src2 = srcTexture2->data;

    assert(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
    assert(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

    for (int i = 0; i < dstTexture->height; ++i) {
      for (int j = 0; j < dstTexture->width; ++j) {
        dst[0] = blend1 * src1[0] + blend2 * src2[0];
        dst[1] = blend1 * src1[1] + blend2 * src2[1];
        dst[2] = blend1 * src1[2] + blend2 * src2[2];
        dst[3] = blend1 * src1[3] + blend2 * src2[3];
        dst += 4;
        src1 += 4;
        src2 += 4;
      }
    }
  }

  void modifier_sub(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
    Texture *dstTexture = gTextures[dstTextureIdx];
    float *dst = dstTexture->data;

    Texture *srcTexture1 = gTextures[srcTexture1Idx];
    float *src1 = srcTexture1->data;

    Texture *srcTexture2 = gTextures[srcTexture2Idx];
    float *src2 = srcTexture2->data;

    assert(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
    assert(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

    for (int i = 0; i < dstTexture->height; ++i) {
      for (int j = 0; j < dstTexture->width; ++j) {
        dst[0] = blend1 * src1[0] - blend2 * src2[0];
        dst[1] = blend1 * src1[1] - blend2 * src2[1];
        dst[2] = blend1 * src1[2] - blend2 * src2[2];
        dst[3] = blend1 * src1[3] - blend2 * src2[3];
        dst += 4;
        src1 += 4;
        src2 += 4;
      }
    }

  }

  void modifier_max(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
    Texture *dstTexture = gTextures[dstTextureIdx];
    float *dst = dstTexture->data;

    Texture *srcTexture1 = gTextures[srcTexture1Idx];
    float *src1 = srcTexture1->data;

    Texture *srcTexture2 = gTextures[srcTexture2Idx];
    float *src2 = srcTexture2->data;

    assert(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
    assert(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

    for (int i = 0; i < dstTexture->height; ++i) {
      for (int j = 0; j < dstTexture->width; ++j) {
        dst[0] = max(blend1 * src1[0], blend2 * src2[0]);
        dst[1] = max(blend1 * src1[1], blend2 * src2[1]);
        dst[2] = max(blend1 * src1[2], blend2 * src2[2]);
        dst[3] = max(blend1 * src1[3], blend2 * src2[3]);
        dst += 4;
        src1 += 4;
        src2 += 4;
      }
    }
  }

  void modifier_min(int dstTextureIdx, int srcTexture1Idx, float blend1, int srcTexture2Idx, float blend2) {
    Texture *dstTexture = gTextures[dstTextureIdx];
    float *dst = dstTexture->data;

    Texture *srcTexture1 = gTextures[srcTexture1Idx];
    float *src1 = srcTexture1->data;

    Texture *srcTexture2 = gTextures[srcTexture2Idx];
    float *src2 = srcTexture2->data;

    assert(dstTexture->width == srcTexture1->width && dstTexture->width == srcTexture2->width);
    assert(dstTexture->height == srcTexture1->height && dstTexture->height == srcTexture2->height);

    for (int i = 0; i < dstTexture->height; ++i) {
      for (int j = 0; j < dstTexture->width; ++j) {
        dst[0] = min(blend1 * src1[0], blend2 * src2[0]);
        dst[1] = min(blend1 * src1[1], blend2 * src2[1]);
        dst[2] = min(blend1 * src1[2], blend2 * src2[2]);
        dst[3] = min(blend1 * src1[3], blend2 * src2[3]);
        dst += 4;
        src1 += 4;
        src2 += 4;
      }
    }

  }

  static void *funcPtrs[] = {
    &source_solid,
    &source_noise,
    &modifier_add,
    &modifier_sub,
    &modifier_max,
    &modifier_min
  };

  __declspec(dllexport) bool initTextureLib() {
    gHeapHandle = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 32*1024, 128*1024);
    return gHeapHandle != INVALID_HANDLE_VALUE;
  }

  __declspec(dllexport) void closeTextureLib() {
    HeapDestroy(gHeapHandle);
    gHeapHandle = INVALID_HANDLE_VALUE;
  }

  __declspec(dllexport) void renderTexture(HWND hwnd, int width, int height, int numTextures, int finalTexture, const char *name, int opCodeLen, const char *opCodes) {

    gTextures = new Texture *[numTextures];
    for (int i = 0; i < numTextures; ++i) {
      Texture *texture = new Texture();
      texture->data = new float[4*width*height];
      texture->width = width;
      texture->height = height;
      gTextures[i] = texture;
    }

    // Massage the generated opcode a little
    uint8 *mem = (uint8 *)HeapAlloc(gHeapHandle, HEAP_ZERO_MEMORY, opCodeLen+9);
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
    HeapFree(gHeapHandle, 0, mem);

    HDC dc = GetDC(hwnd);

    BITMAPINFO bmi;
    ZeroMemory(&bmi, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);

    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;
    bmi.bmiHeader.biWidth = width;
    bmi.bmiHeader.biHeight = -height;

    uint8 *dst;
    HBITMAP bm = CreateDIBSection(dc, &bmi, DIB_RGB_COLORS, (void **)&dst, NULL, 0);

    // Copy the dest texture to the hwnd
    float *src = gTextures[finalTexture]->data;
    for (int i = 0; i < height; ++i) {
      for (int j = 0; j < width; ++j) {
        dst[3] = (uint8)(255 * max(0, min(src[0], 1)));
        dst[2] = (uint8)(255 * max(0, min(src[1], 1)));
        dst[1] = (uint8)(255 * max(0, min(src[2], 1)));
        dst[0] = (uint8)(255 * max(0, min(src[3], 1)));
        dst += 4;
        src += 4;
      }
    }

    HDC src_dc = CreateCompatibleDC(dc);
    SelectObject(src_dc, bm);
    BitBlt(dc, 0, 0, 512, 512, src_dc, 0, 0, SRCCOPY);

    DeleteDC(src_dc);
    DeleteObject(bm);

    ReleaseDC(hwnd, dc);


    for (int i = 0; i < numTextures; ++i) {
      delete [] gTextures[i]->data;
      delete gTextures[i];
    }

    delete [] gTextures;
  }
};

