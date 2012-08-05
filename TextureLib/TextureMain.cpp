#include "stdafx.h"
#include <stdint.h>
#include <assert.h>
#include <math.h>
#include <stdlib.h>
#include <string>
#include <vector>
#include <deque>
#include <map>
#include "TextureLib.hpp"

using namespace std;

HANDLE gHeapHandle = INVALID_HANDLE_VALUE;

extern int *gPerm;
extern Vector2 *gGrad;

extern Texture **gTextures;

HANDLE gRenderThread = INVALID_HANDLE_VALUE;
HANDLE gCloseEvent = INVALID_HANDLE_VALUE;
HANDLE gNewDataEvent = INVALID_HANDLE_VALUE;
CRITICAL_SECTION gRenderCs;

typedef void(__stdcall *fnCompletedCallback)(HWND hwnd);
fnCompletedCallback gCompletedCallback;

static void *funcPtrs[] = {
  &source_solid,
  &source_noise,
  &modifier_add,
  &modifier_sub,
  &modifier_max,
  &modifier_min,
  &modifier_mul,
  &source_circles,
};

struct RenderData {
  HWND hwnd;
  int width;
  int height;
  int numTextures;
  int finalTexture;
  string name;
  vector<uint8> opCodes;
};

#define RAND_MAX_32 ((1U << 31) - 1)

int tRand()
{
  static int seed = 0x12345;
  return (seed = (seed * 214013 + 2531011) & RAND_MAX_32) >> 16;
}


// One queue per HWND
map<HWND, deque<RenderData *> >gRenderQueue;

DWORD WINAPI renderThread(void *param) {

  HANDLE events[2] = { gCloseEvent, gNewDataEvent };

  while (true) {
    EnterCriticalSection(&gRenderCs);
    bool queueEmpty = gRenderQueue.empty();
    LeaveCriticalSection(&gRenderCs);

    if (queueEmpty) {
      DWORD res = WaitForMultipleObjects(2, events, FALSE, INFINITE);
      if (res == WAIT_OBJECT_0) {
        // close
        break;
      }
    } else {
      // more items to render, so we'll just check the close event to know if
      // we should bail anyway
      if (WaitForSingleObject(gCloseEvent, 0) == WAIT_OBJECT_0)
        break;
    }

    EnterCriticalSection(&gRenderCs);
    // get data from the first non empty queue
    RenderData *data = nullptr;
    for (auto it = begin(gRenderQueue); it != end(gRenderQueue); /**/) {
      auto &q = it->second;
      if (!q.empty()) {
        data = q.front();
      }

      it = gRenderQueue.erase(it);
      if (data)
        break;
    }
    LeaveCriticalSection(&gRenderCs);

    if (!data)
      continue;

    gTextures = new Texture *[data->numTextures];
    for (int i = 0; i < data->numTextures; ++i) {
      Texture *texture = new Texture();
      texture->data = new float[4*data->width*data->height];
      texture->width = data->width;
      texture->height = data->height;
      gTextures[i] = texture;
    }

    // Copy the opcodes to executable memory, and call that badboy!
    uint8 *mem = (uint8 *)HeapAlloc(gHeapHandle, HEAP_ZERO_MEMORY, data->opCodes.size());
    memcpy(mem, data->opCodes.data(), data->opCodes.size());

    _asm {
      call [mem];
    }
    HeapFree(gHeapHandle, 0, mem);

    HDC dc = GetDC(data->hwnd);

    BITMAPINFO bmi;
    ZeroMemory(&bmi, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);

    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32;
    bmi.bmiHeader.biCompression = BI_RGB;
    bmi.bmiHeader.biWidth = data->width;
    bmi.bmiHeader.biHeight = -data->height;

    uint8 *dst;
    HBITMAP bm = CreateDIBSection(dc, &bmi, DIB_RGB_COLORS, (void **)&dst, NULL, 0);

    // Copy the dest texture to the hwnd
    float *src = gTextures[data->finalTexture]->data;
    for (int i = 0; i < data->height; ++i) {
      for (int j = 0; j < data->width; ++j) {
        dst[3] = (uint8)(255 * max(0, min(src[0], 1)));
        dst[2] = (uint8)(255 * max(0, min(src[1], 1)));
        dst[1] = (uint8)(255 * max(0, min(src[2], 1)));
        dst[0] = (uint8)(255 * max(0, min(src[3], 1)));
        dst += 4;
        src += 4;
      }
    }

    HWND hwnd = data->hwnd;
    HDC src_dc = CreateCompatibleDC(dc);
    SelectObject(src_dc, bm);
    BitBlt(dc, 0, 0, 512, 512, src_dc, 0, 0, SRCCOPY);

    DeleteDC(src_dc);
    DeleteObject(bm);

    ReleaseDC(hwnd, dc);

    for (int i = 0; i < data->numTextures; ++i) {
      delete [] gTextures[i]->data;
      delete gTextures[i];
    }

    delete [] gTextures;
    delete data;

    gCompletedCallback(hwnd);

  }
  return 0;
}

extern "C" {

  __declspec(dllexport) bool initTextureLib(fnCompletedCallback completedCallback) {

    gCompletedCallback = completedCallback;

    gHeapHandle = HeapCreate(HEAP_CREATE_ENABLE_EXECUTE, 32*1024, 128*1024);
    if (gHeapHandle == INVALID_HANDLE_VALUE)
      return false;

    gPerm = new int[512];
    for (int i = 0; i < 512; ++i)
      gPerm[i] = tRand() % 256;

    gGrad = new Vector2[cNumGradients];
    for (int i = 0; i < cNumGradients; ++i)
      gGrad[i] = normalize(Vector2(randf(-1.0f,1.0f), randf(-1.0f,1.0f)));

    gCloseEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
    gNewDataEvent = CreateEvent(NULL, FALSE, FALSE, NULL);

    InitializeCriticalSection(&gRenderCs);

    DWORD threadId;
    gRenderThread = CreateThread(NULL, 0, renderThread, NULL, 0, &threadId);

    return true;
  }

  __declspec(dllexport) void closeTextureLib() {
    HeapDestroy(gHeapHandle);
    gHeapHandle = INVALID_HANDLE_VALUE;

    delete [] gPerm;
    delete [] gGrad;

    if (gRenderThread != INVALID_HANDLE_VALUE) {
      SetEvent(gCloseEvent);
      WaitForSingleObject(gRenderThread, INFINITE);
      CloseHandle(gRenderThread);
    }

    CloseHandle(gCloseEvent);
    CloseHandle(gNewDataEvent);

    DeleteCriticalSection(&gRenderCs);
  }

  void patchOpCodes(int opCodeLen, const uint8 *opCodes, vector<uint8> *out)   {

    out->resize(opCodeLen + 9);
    uint8 *mem = out->data();

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
  }

  void printInt32(FILE *f, int v, const char *prefix, const char *suffix)
  {
    fprintf(f, "%s0x%.2x, 0x%.2x, 0x%.2x, 0x%.2x%s", prefix ? prefix : "", v & 0xff, (v >> 8) & 0xff, (v >> 16) & 0xff, (v >> 24) & 0xff, suffix ? suffix : "");
  }

  __declspec(dllexport) void generateCode(int width, int height, int numTextures, int finalTexture, const char *name, int opCodeLen, const uint8 *opCodes, const char *filename) {

    vector<uint8> mem;
    patchOpCodes(opCodeLen, opCodes, &mem);

    FILE *f = fopen(filename, "at");
    fseek(f, 0, SEEK_END);
    fprintf(f, "unsigned char %s[%d] = {\n", name ? name : "dummy", 4*4+mem.size());
    printInt32(f, width, "\t", ", ");
    printInt32(f, height, "", ", // width, height\n");

    printInt32(f, numTextures, "\t", ", ");
    printInt32(f, finalTexture, "", ", // num textures, final texture\n\t");

    for (size_t i = 0; i < mem.size(); ++i) {
      fprintf(f, "0x%.2x%s%s", mem[i], 
        i != mem.size()-1 ? "," : "", 
        (i & 0xf) == 0xf ? "\n\t" : "");
    }

    fprintf(f, "\n};\n\n");

    fclose(f);
  }

  __declspec(dllexport) void renderTexture(HWND hwnd, int width, int height, int numTextures, int finalTexture, const char *name, int opCodeLen, const uint8 *opCodes) {

    auto rd = new RenderData;
    rd->hwnd = hwnd;
    rd->width = width;
    rd->height = height;
    rd->numTextures = numTextures;
    rd->finalTexture = finalTexture;
    rd->name = name ? name : "";
    patchOpCodes(opCodeLen, opCodes, &rd->opCodes);

    EnterCriticalSection(&gRenderCs);
    gRenderQueue[hwnd].push_back(rd);
    LeaveCriticalSection(&gRenderCs);

    SetEvent(gNewDataEvent);
  }
};
